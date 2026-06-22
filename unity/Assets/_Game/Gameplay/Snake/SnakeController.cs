using System;
using System.Collections.Generic;
using UnityEngine;

namespace SnakeRoguelite.Gameplay.Snake
{
    public sealed class SnakeController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Camera gameplayCamera;
        [SerializeField] private Transform bodyRoot;
        [SerializeField] private Transform bodySegmentPrefab;

        [Header("Movement")]
        [SerializeField, Min(1f)] private float moveSpeed = 6f;
        [SerializeField, Min(90f)] private float turnSpeedDegrees = 540f;
        [SerializeField] private Vector2 arenaHalfExtents = new(7f, 12f);
        [SerializeField] private float movementPlaneY;

        [Header("Body")]
        [SerializeField, Min(1)] private int initialSegmentCount = 6;
        [SerializeField, Min(0.1f)] private float segmentSpacing = 0.65f;
        [SerializeField, Min(0.05f)] private float pathSampleSpacing = 0.12f;
        [SerializeField, Min(1)] private int tailDangerSegmentCount = 2;
        [SerializeField, Min(0.1f)] private float visualSegmentScale = 0.42f;

        [Header("Combat")]
        [SerializeField, Min(0.1f)] private float headContactRadius = 0.5f;
        [SerializeField, Min(0.1f)] private float bodyContactRadius = 0.38f;
        [SerializeField, Min(0.1f)] private float headImpactDamage = 5f;
        [SerializeField, Min(0.1f)] private float bodyDamagePerSecond = 4f;
        [SerializeField, Min(1f)] private float tailDamageMultiplier = 2f;

        [Header("Health")]
        [SerializeField, Min(1)] private int fallbackStartingHealth = 5;

        [Header("Visuals")]
        [SerializeField] private Color bodyColor = new(0.32f, 0.85f, 0.47f);
        [SerializeField] private Color tailDangerColor = new(0.95f, 0.3f, 0.3f);
        [SerializeField] private Color hitFlashColor = new(1f, 0.85f, 0.4f);
        [SerializeField, Min(0.02f)] private float hitFlashDurationSeconds = 0.12f;

        private readonly List<Transform> _segments = new();
        private readonly List<Renderer> _segmentRenderers = new();
        private readonly List<Vector3> _pathPoints = new();
        private Plane _movementPlane = new(Vector3.up, Vector3.zero);

        private Vector3 _moveDirection = Vector3.forward;
        private Vector3 _pointerWorldPosition;
        private bool _hasPointerTarget;
        private int _maxHealth;
        private int _currentHealth;
        private bool _initialized;
        private bool _isDead;
        private bool _isPaused;
        private float _tailSpikeBurstDamage;
        private float _tailSpikeKnockbackForce;
        private float _bodyShieldReductionPerStack;
        private int _bodyShieldSegmentsPerStack;
        private float _bodyShieldMaxReduction;
        private bool _hasFrenzy;
        private float _frenzyHealthThresholdNormalized;
        private float _frenzyMoveMultiplier = 1f;
        private float _frenzyDamageMultiplier = 1f;
        private int _bonusGrowthPerKill;
        private float _pickupMagnetRadius;
        private float _hitFlashTimer;

        public event Action<int, int> DamageTaken;
        public event Action<int, int, SnakeContactZone, SnakeDamageSource> DamageTakenDetailed;
        public event Action Died;
        public event Action<int> Grew;

        public Vector3 HeadPosition => transform.position;
        public Vector3 TailPosition => _segments.Count > 0 ? _segments[_segments.Count - 1].position : transform.position;
        public float HeadImpactDamage => headImpactDamage * GetOutgoingDamageMultiplier();
        public float BodyDamagePerSecond => bodyDamagePerSecond * GetOutgoingDamageMultiplier();
        public float TailDamageMultiplier => tailDamageMultiplier;
        public float HeadContactRadius => headContactRadius;
        public bool IsAlive => !_isDead;
        public int CurrentHealth => _currentHealth;
        public int MaxHealth => _maxHealth;
        public int SegmentCount => _segments.Count;
        public int BonusGrowthPerKill => _bonusGrowthPerKill;
        public float TailSpikeBurstDamage => _tailSpikeBurstDamage;
        public float TailSpikeKnockbackForce => _tailSpikeKnockbackForce;
        public float PickupMagnetRadius => _pickupMagnetRadius;

        private void Awake()
        {
            if (gameplayCamera == null)
            {
                gameplayCamera = Camera.main;
            }

            if (bodyRoot == null)
            {
                bodyRoot = transform;
            }

            _movementPlane = new Plane(Vector3.up, new Vector3(0f, movementPlaneY, 0f));
        }

        private void Start()
        {
            if (!_initialized)
            {
                Initialize(fallbackStartingHealth);
            }
        }

        private void Update()
        {
            if (!_initialized || _isDead || _isPaused)
            {
                return;
            }

            TickHitFlash(Time.deltaTime);
            ReadPointerInput();
            TickMovement(Time.deltaTime);
            TickPath();
            TickBodySegments();
        }

        public void Initialize(int startingHealth)
        {
            _maxHealth = Mathf.Max(1, startingHealth);
            _currentHealth = _maxHealth;
            _isDead = false;
            _initialized = true;
            _moveDirection = Vector3.forward;
            ResetPowerState();

            TrimSegmentCount(initialSegmentCount);
            EnsureSegmentCount(initialSegmentCount);
            ResetPath();
            TickBodySegments();
        }

        public void Grow(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            var previousCount = _segments.Count;
            EnsureSegmentCount(previousCount + amount);
            Grew?.Invoke(_segments.Count - previousCount);
        }

        public void ApplyDamage(
            int amount,
            SnakeContactZone zone,
            SnakeDamageSource damageSource = SnakeDamageSource.Unknown)
        {
            if (amount <= 0 || _isDead)
            {
                return;
            }

            var finalDamage = Mathf.Max(1, Mathf.RoundToInt(amount * GetIncomingDamageMultiplier()));
            if (zone == SnakeContactZone.Tail)
            {
                finalDamage = Mathf.Max(1, Mathf.CeilToInt(finalDamage * tailDamageMultiplier));
            }

            _currentHealth -= finalDamage;
            if (_currentHealth < 0)
            {
                _currentHealth = 0;
            }

            _hitFlashTimer = hitFlashDurationSeconds;
            RefreshSegmentColors();
            DamageTaken?.Invoke(finalDamage, _currentHealth);
            DamageTakenDetailed?.Invoke(finalDamage, _currentHealth, zone, damageSource);

            if (_currentHealth == 0)
            {
                _isDead = true;
                Died?.Invoke();
            }
        }

        public bool TryGetClosestContactSample(Vector3 worldPosition, out SnakeContactSample sample)
        {
            var bestPoint = transform.position;
            var bestZone = SnakeContactZone.Head;
            var bestRadius = headContactRadius;
            var bestSqrDistance = FlatSqrDistance(transform.position, worldPosition);

            for (var i = 0; i < _segments.Count; i++)
            {
                var segmentPoint = _segments[i].position;
                var sqrDistance = FlatSqrDistance(segmentPoint, worldPosition);
                if (sqrDistance >= bestSqrDistance)
                {
                    continue;
                }

                bestPoint = segmentPoint;
                bestSqrDistance = sqrDistance;
                bestRadius = bodyContactRadius;
                bestZone = i >= _segments.Count - tailDangerSegmentCount
                    ? SnakeContactZone.Tail
                    : SnakeContactZone.Body;
            }

            sample = new SnakeContactSample(bestPoint, bestZone, bestRadius, bestSqrDistance);
            return true;
        }

        public void AddTailSpike(float burstDamage, float knockbackForce)
        {
            _tailSpikeBurstDamage += Mathf.Max(0f, burstDamage);
            _tailSpikeKnockbackForce += Mathf.Max(0f, knockbackForce);
        }

        public void AddBodyShield(float reductionPerStack, int segmentsPerStack, float maxReduction)
        {
            _bodyShieldReductionPerStack += Mathf.Max(0f, reductionPerStack);
            _bodyShieldSegmentsPerStack = Mathf.Max(_bodyShieldSegmentsPerStack, Mathf.Max(1, segmentsPerStack));
            _bodyShieldMaxReduction = Mathf.Max(_bodyShieldMaxReduction, Mathf.Clamp01(maxReduction));
        }

        public void AddFrenzy(float healthThresholdNormalized, float moveMultiplier, float damageMultiplier)
        {
            _hasFrenzy = true;
            _frenzyHealthThresholdNormalized = Mathf.Max(_frenzyHealthThresholdNormalized, Mathf.Clamp01(healthThresholdNormalized));
            _frenzyMoveMultiplier = Mathf.Max(_frenzyMoveMultiplier, Mathf.Max(1f, moveMultiplier));
            _frenzyDamageMultiplier = Mathf.Max(_frenzyDamageMultiplier, Mathf.Max(1f, damageMultiplier));
        }

        public void AddBonusGrowthPerKill(int amount)
        {
            _bonusGrowthPerKill += Mathf.Max(0, amount);
        }

        public void AddPickupMagnet(float radius)
        {
            _pickupMagnetRadius = Mathf.Max(_pickupMagnetRadius, Mathf.Max(0f, radius));
        }

        public void SetPaused(bool isPaused)
        {
            _isPaused = isPaused;
        }

        private void ReadPointerInput()
        {
            _hasPointerTarget = false;

            if (gameplayCamera == null)
            {
                return;
            }

            Vector2 pointerPosition;
            if (Input.touchCount > 0)
            {
                var touch = Input.GetTouch(0);
                pointerPosition = touch.position;
            }
            else if (Input.GetMouseButton(0))
            {
                pointerPosition = Input.mousePosition;
            }
            else
            {
                return;
            }

            var ray = gameplayCamera.ScreenPointToRay(pointerPosition);
            if (!_movementPlane.Raycast(ray, out var distance))
            {
                return;
            }

            _pointerWorldPosition = ray.GetPoint(distance);
            _pointerWorldPosition.y = movementPlaneY;
            _hasPointerTarget = true;
        }

        private void TickMovement(float deltaTime)
        {
            if (_hasPointerTarget)
            {
                var desiredDirection = _pointerWorldPosition - transform.position;
                desiredDirection.y = 0f;
                if (desiredDirection.sqrMagnitude > 0.001f)
                {
                    desiredDirection.Normalize();
                    _moveDirection = Vector3.RotateTowards(
                        _moveDirection,
                        desiredDirection,
                        Mathf.Deg2Rad * turnSpeedDegrees * deltaTime,
                        0f);
                }
            }

            var nextPosition = transform.position + (_moveDirection * GetCurrentMoveSpeed() * deltaTime);
            var clampedPosition = ClampToArena(nextPosition);
            if ((clampedPosition - nextPosition).sqrMagnitude > 0.0001f)
            {
                var toCenter = new Vector3(-clampedPosition.x, 0f, -clampedPosition.z);
                if (toCenter.sqrMagnitude > 0.001f)
                {
                    _moveDirection = Vector3.RotateTowards(
                        _moveDirection,
                        toCenter.normalized,
                        Mathf.Deg2Rad * turnSpeedDegrees * deltaTime,
                        0f);
                }
            }

            transform.position = clampedPosition;
            transform.rotation = Quaternion.LookRotation(_moveDirection, Vector3.up);
        }

        private void TickPath()
        {
            if (_pathPoints.Count == 0)
            {
                _pathPoints.Add(transform.position);
                return;
            }

            var headPoint = _pathPoints[0];
            if ((headPoint - transform.position).sqrMagnitude >= pathSampleSpacing * pathSampleSpacing)
            {
                _pathPoints.Insert(0, transform.position);
            }
            else
            {
                _pathPoints[0] = transform.position;
            }

            var maxDistance = (_segments.Count + 2) * segmentSpacing;
            var maxSamples = Mathf.CeilToInt(maxDistance / pathSampleSpacing) + 2;
            while (_pathPoints.Count > maxSamples)
            {
                _pathPoints.RemoveAt(_pathPoints.Count - 1);
            }
        }

        private void TickBodySegments()
        {
            for (var i = 0; i < _segments.Count; i++)
            {
                var distance = (i + 1) * segmentSpacing;
                var position = SamplePathPosition(distance);
                var segment = _segments[i];
                segment.position = position;

                var lookAtPoint = i == 0 ? transform.position : _segments[i - 1].position;
                var direction = lookAtPoint - segment.position;
                direction.y = 0f;
                if (direction.sqrMagnitude > 0.001f)
                {
                    segment.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
                }
            }

            RefreshSegmentColors();
        }

        private Vector3 SamplePathPosition(float distance)
        {
            if (_pathPoints.Count == 0)
            {
                return transform.position;
            }

            var remainingDistance = distance;
            for (var i = 0; i < _pathPoints.Count - 1; i++)
            {
                var from = _pathPoints[i];
                var to = _pathPoints[i + 1];
                var segmentDistance = Vector3.Distance(from, to);

                if (segmentDistance >= remainingDistance)
                {
                    var t = segmentDistance <= 0.0001f ? 0f : remainingDistance / segmentDistance;
                    return Vector3.Lerp(from, to, t);
                }

                remainingDistance -= segmentDistance;
            }

            return _pathPoints[_pathPoints.Count - 1];
        }

        private void EnsureSegmentCount(int desiredCount)
        {
            while (_segments.Count < desiredCount)
            {
                CreateSegment();
            }
        }

        private void TrimSegmentCount(int desiredCount)
        {
            while (_segments.Count > desiredCount)
            {
                var lastIndex = _segments.Count - 1;
                var segment = _segments[lastIndex];
                _segments.RemoveAt(lastIndex);
                _segmentRenderers.RemoveAt(lastIndex);

                if (segment != null)
                {
                    Destroy(segment.gameObject);
                }
            }
        }

        private void CreateSegment()
        {
            Transform segmentTransform;
            if (bodySegmentPrefab != null)
            {
                segmentTransform = Instantiate(bodySegmentPrefab, bodyRoot);
            }
            else
            {
                var fallbackSegment = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                fallbackSegment.name = $"BodySegment_{_segments.Count}";
                fallbackSegment.transform.SetParent(bodyRoot, false);
                fallbackSegment.transform.localScale = Vector3.one * visualSegmentScale;

                var collider = fallbackSegment.GetComponent<Collider>();
                if (collider != null)
                {
                    Destroy(collider);
                }

                segmentTransform = fallbackSegment.transform;
            }

            segmentTransform.position = transform.position;
            _segments.Add(segmentTransform);
            _segmentRenderers.Add(segmentTransform.GetComponentInChildren<Renderer>());
        }

        private void ResetPath()
        {
            _pathPoints.Clear();
            _pathPoints.Add(transform.position);

            for (var i = 0; i < _segments.Count; i++)
            {
                var position = transform.position - (Vector3.forward * ((i + 1) * segmentSpacing));
                _segments[i].position = position;
            }
        }

        private void RefreshSegmentColors()
        {
            var flashNormalized = hitFlashDurationSeconds <= 0f
                ? 0f
                : Mathf.Clamp01(_hitFlashTimer / hitFlashDurationSeconds);

            for (var i = 0; i < _segmentRenderers.Count; i++)
            {
                var renderer = _segmentRenderers[i];
                if (renderer == null)
                {
                    continue;
                }

                var isTailDanger = i >= _segmentRenderers.Count - tailDangerSegmentCount;
                var targetColor = isTailDanger ? tailDangerColor : bodyColor;
                renderer.material.color = Color.Lerp(targetColor, hitFlashColor, flashNormalized);
            }
        }

        private Vector3 ClampToArena(Vector3 position)
        {
            position.x = Mathf.Clamp(position.x, -arenaHalfExtents.x, arenaHalfExtents.x);
            position.z = Mathf.Clamp(position.z, -arenaHalfExtents.y, arenaHalfExtents.y);
            position.y = movementPlaneY;
            return position;
        }

        private float GetCurrentMoveSpeed()
        {
            return moveSpeed * GetOutgoingMoveMultiplier();
        }

        private float GetOutgoingDamageMultiplier()
        {
            if (_hasFrenzy && IsBelowFrenzyThreshold())
            {
                return _frenzyDamageMultiplier;
            }

            return 1f;
        }

        private float GetOutgoingMoveMultiplier()
        {
            if (_hasFrenzy && IsBelowFrenzyThreshold())
            {
                return _frenzyMoveMultiplier;
            }

            return 1f;
        }

        private float GetIncomingDamageMultiplier()
        {
            if (_bodyShieldSegmentsPerStack <= 0 || _bodyShieldReductionPerStack <= 0f)
            {
                return 1f;
            }

            var stackCount = SegmentCount / _bodyShieldSegmentsPerStack;
            var reduction = Mathf.Min(
                _bodyShieldMaxReduction,
                stackCount * _bodyShieldReductionPerStack);

            return Mathf.Clamp01(1f - reduction);
        }

        private bool IsBelowFrenzyThreshold()
        {
            if (_maxHealth <= 0)
            {
                return false;
            }

            var normalizedHealth = (float)_currentHealth / _maxHealth;
            return normalizedHealth <= _frenzyHealthThresholdNormalized;
        }

        private void ResetPowerState()
        {
            _tailSpikeBurstDamage = 0f;
            _tailSpikeKnockbackForce = 0f;
            _bodyShieldReductionPerStack = 0f;
            _bodyShieldSegmentsPerStack = 0;
            _bodyShieldMaxReduction = 0f;
            _hasFrenzy = false;
            _frenzyHealthThresholdNormalized = 0f;
            _frenzyMoveMultiplier = 1f;
            _frenzyDamageMultiplier = 1f;
            _bonusGrowthPerKill = 0;
            _pickupMagnetRadius = 0f;
            _hitFlashTimer = 0f;
        }

        private void TickHitFlash(float deltaTime)
        {
            if (_hitFlashTimer <= 0f)
            {
                return;
            }

            _hitFlashTimer -= deltaTime;
            if (_hitFlashTimer < 0f)
            {
                _hitFlashTimer = 0f;
            }
        }

        private static float FlatSqrDistance(Vector3 a, Vector3 b)
        {
            var delta = a - b;
            delta.y = 0f;
            return delta.sqrMagnitude;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.1f, 0.9f, 1f, 0.3f);
            Gizmos.DrawWireCube(
                new Vector3(0f, movementPlaneY, 0f),
                new Vector3(arenaHalfExtents.x * 2f, 0.1f, arenaHalfExtents.y * 2f));

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, headContactRadius);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(TailPosition, bodyContactRadius);
        }
    }
}
