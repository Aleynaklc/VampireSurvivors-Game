using System;
using SnakeRoguelite.Gameplay.Snake;
using UnityEngine;

namespace SnakeRoguelite.Gameplay.Enemies
{
    public abstract class PrototypeEnemyBase : MonoBehaviour
    {
        [Header("Combat")]
        [SerializeField, Min(0.5f)] private float maxHealth = 12f;
        [SerializeField, Min(0.1f)] private float contactRadius = 0.45f;
        [SerializeField, Min(0.05f)] private float headImpactCooldownSeconds = 0.25f;
        [SerializeField, Min(0.05f)] private float bodyDamageTickSeconds = 0.35f;
        [SerializeField, Min(1)] private int contactDamage = 1;
        [SerializeField, Min(0)] private int growthRewardSegments = 1;
        [SerializeField, Min(0)] private int experienceReward = 1;
        [SerializeField, Min(0f)] private float knockbackDamping = 8f;
        [SerializeField, Min(0.1f)] private float headDamageTakenMultiplier = 1f;
        [SerializeField, Min(0.1f)] private float bodyDamageTakenMultiplier = 1f;

        [Header("Feedback")]
        [SerializeField] private Color flashColor = new(1f, 0.35f, 0.35f);
        [SerializeField, Min(0.02f)] private float flashDurationSeconds = 0.09f;

        private SnakeController _targetSnake;
        private float _currentHealth;
        private Vector3 _externalVelocity;
        private float _nextHeadImpactTime;
        private float _nextBodyTickTime;
        private bool _isDead;
        private bool _isPaused;
        private float _pausedAtTime;
        private float _flashTimer;
        private Renderer[] _renderers = Array.Empty<Renderer>();
        private Color[] _baseColors = Array.Empty<Color>();

        public event Action<PrototypeEnemyBase> Defeated;
        public event Action<PrototypeEnemyBase, float, bool> Damaged;

        public int ExperienceReward => experienceReward;
        public int GrowthRewardSegments => growthRewardSegments;
        public bool IsAlive => !_isDead;
        public SnakeController TargetSnake => _targetSnake;
        protected float ContactRadius => contactRadius;

        protected virtual void Awake()
        {
            _currentHealth = maxHealth;
            CacheRenderers();
        }

        protected virtual void Update()
        {
            if (_isDead)
            {
                return;
            }

            UpdateFlash(Time.deltaTime);

            if (_isPaused || _targetSnake == null || !_targetSnake.IsAlive)
            {
                return;
            }

            TickMovement(Time.deltaTime);
            TickContacts(Time.deltaTime);
            TickKnockback(Time.deltaTime);
        }

        public void Initialize(SnakeController snakeController)
        {
            _targetSnake = snakeController;
            OnInitialized();
        }

        public void ApplyExternalDamage(float amount)
        {
            ApplyDamage(amount);
        }

        public void ApplyKnockback(Vector3 direction, float force)
        {
            if (force <= 0f)
            {
                return;
            }

            var flatDirection = direction;
            flatDirection.y = 0f;
            if (flatDirection.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            _externalVelocity += flatDirection.normalized * force;
        }

        public void SetPaused(bool isPaused)
        {
            if (_isPaused == isPaused)
            {
                return;
            }

            if (isPaused)
            {
                _pausedAtTime = Time.time;
            }
            else
            {
                var pauseDuration = Time.time - _pausedAtTime;
                _nextHeadImpactTime += pauseDuration;
                _nextBodyTickTime += pauseDuration;
                OnResumed(pauseDuration);
            }

            _isPaused = isPaused;
        }

        protected abstract void TickMovement(float deltaTime);

        protected virtual void OnInitialized()
        {
        }

        protected virtual void OnResumed(float pauseDuration)
        {
        }

        protected void MoveTowards(Vector3 targetPoint, float moveSpeed, float deltaTime)
        {
            var direction = targetPoint - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            var normalized = direction.normalized;
            transform.position += normalized * (moveSpeed * deltaTime);
            transform.rotation = Quaternion.LookRotation(normalized, Vector3.up);
        }

        protected void FaceTowards(Vector3 targetPoint)
        {
            var direction = targetPoint - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        }

        private void TickContacts(float deltaTime)
        {
            var headDistance = FlatDistance(_targetSnake.HeadPosition, transform.position);
            if (headDistance <= contactRadius + _targetSnake.HeadContactRadius)
            {
                if (Time.time < _nextHeadImpactTime)
                {
                    return;
                }

                ApplyDamage(_targetSnake.HeadImpactDamage * headDamageTakenMultiplier);
                _targetSnake.ApplyDamage(
                    contactDamage,
                    SnakeContactZone.Head,
                    SnakeDamageSource.EnemyHeadImpact);
                _nextHeadImpactTime = Time.time + headImpactCooldownSeconds;
                return;
            }

            _targetSnake.TryGetClosestContactSample(transform.position, out var sample);
            var combinedRadius = contactRadius + sample.Radius;
            if (sample.SqrDistance > combinedRadius * combinedRadius)
            {
                return;
            }

            ApplyDamage(_targetSnake.BodyDamagePerSecond * bodyDamageTakenMultiplier * deltaTime);

            if (Time.time < _nextBodyTickTime)
            {
                return;
            }

            if (sample.Zone == SnakeContactZone.Tail && _targetSnake.TailSpikeBurstDamage > 0f)
            {
                ApplyDamage(_targetSnake.TailSpikeBurstDamage);
                var knockbackDirection = transform.position - sample.Point;
                ApplyKnockback(knockbackDirection, _targetSnake.TailSpikeKnockbackForce);
            }

            _targetSnake.ApplyDamage(
                contactDamage,
                sample.Zone,
                SnakeDamageSource.EnemyBodyContact);
            _nextBodyTickTime = Time.time + bodyDamageTickSeconds;
        }

        private void ApplyDamage(float amount)
        {
            if (amount <= 0f || _isDead)
            {
                return;
            }

            _currentHealth -= amount;
            _flashTimer = flashDurationSeconds;
            RefreshRendererColors(1f);

            var wasKilled = _currentHealth <= 0f;
            Damaged?.Invoke(this, amount, wasKilled);

            if (!wasKilled)
            {
                return;
            }

            _isDead = true;
            Defeated?.Invoke(this);
            Destroy(gameObject);
        }

        private void TickKnockback(float deltaTime)
        {
            if (_externalVelocity.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            transform.position += _externalVelocity * deltaTime;
            _externalVelocity = Vector3.Lerp(
                _externalVelocity,
                Vector3.zero,
                knockbackDamping * deltaTime);
        }

        private void CacheRenderers()
        {
            _renderers = GetComponentsInChildren<Renderer>(true);
            _baseColors = new Color[_renderers.Length];

            for (var i = 0; i < _renderers.Length; i++)
            {
                var renderer = _renderers[i];
                _baseColors[i] = renderer != null ? renderer.material.color : Color.white;
            }
        }

        private void UpdateFlash(float deltaTime)
        {
            if (_flashTimer <= 0f)
            {
                return;
            }

            _flashTimer -= deltaTime;
            var normalized = flashDurationSeconds <= 0f
                ? 0f
                : Mathf.Clamp01(_flashTimer / flashDurationSeconds);

            RefreshRendererColors(normalized);
        }

        private void RefreshRendererColors(float normalizedFlash)
        {
            for (var i = 0; i < _renderers.Length; i++)
            {
                var renderer = _renderers[i];
                if (renderer == null)
                {
                    continue;
                }

                renderer.material.color = Color.Lerp(
                    _baseColors[i],
                    flashColor,
                    normalizedFlash);
            }
        }

        protected static float FlatDistance(Vector3 a, Vector3 b)
        {
            var delta = a - b;
            delta.y = 0f;
            return delta.magnitude;
        }
    }
}
