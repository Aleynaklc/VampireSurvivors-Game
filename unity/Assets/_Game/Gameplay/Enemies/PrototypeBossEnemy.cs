using System;
using SnakeRoguelite.Gameplay.Snake;
using UnityEngine;

namespace SnakeRoguelite.Gameplay.Enemies
{
    public sealed class PrototypeBossEnemy : MonoBehaviour
    {
        private enum BossMode
        {
            Chase = 0,
            Charge = 1,
            Recover = 2,
        }

        [Header("Stats")]
        [SerializeField, Min(10f)] private float maxHealth = 90f;
        [SerializeField, Min(0.5f)] private float chaseSpeed = 2.8f;
        [SerializeField, Min(1f)] private float chargeSpeed = 10f;
        [SerializeField, Min(0.1f)] private float chargeDurationSeconds = 0.9f;
        [SerializeField, Min(0.1f)] private float recoverDurationSeconds = 1.1f;
        [SerializeField, Min(0.1f)] private float chargeCooldownSeconds = 2.5f;

        [Header("Combat")]
        [SerializeField, Min(0.1f)] private float contactRadius = 0.9f;
        [SerializeField, Min(0.05f)] private float contactTickSeconds = 0.4f;
        [SerializeField, Min(1)] private int contactDamage = 1;
        [SerializeField, Min(1f)] private float bodyDamageTakenMultiplier = 0.55f;
        [SerializeField, Min(1f)] private float headDamageTakenMultiplier = 0.8f;

        [Header("Pulse")]
        [SerializeField, Min(0.5f)] private float pulseRadius = 3.5f;
        [SerializeField, Min(0.1f)] private float pulseCooldownSeconds = 4f;
        [SerializeField, Min(1)] private int pulseDamage = 1;

        [Header("Feedback")]
        [SerializeField] private Color flashColor = new(1f, 0.45f, 0.35f);
        [SerializeField, Min(0.02f)] private float flashDurationSeconds = 0.1f;

        private SnakeController _targetSnake;
        private float _currentHealth;
        private float _nextContactTime;
        private float _nextChargeTime;
        private float _nextPulseTime;
        private float _modeTimer;
        private bool _isDead;
        private bool _isPaused;
        private float _pausedAtTime;
        private BossMode _mode = BossMode.Chase;
        private Vector3 _chargeDirection = Vector3.forward;
        private float _flashTimer;
        private Renderer[] _renderers = Array.Empty<Renderer>();
        private Color[] _baseColors = Array.Empty<Color>();

        public event Action<PrototypeBossEnemy> Defeated;
        public event Action<PrototypeBossEnemy, float, bool> Damaged;
        public float CurrentHealth => Mathf.Max(0f, _currentHealth);
        public float MaxHealth => maxHealth;
        public float HealthNormalized => maxHealth <= 0f ? 0f : Mathf.Clamp01(_currentHealth / maxHealth);
        public bool IsAlive => !_isDead;

        private void Awake()
        {
            _currentHealth = maxHealth;
            _nextChargeTime = Time.time + chargeCooldownSeconds;
            _nextPulseTime = Time.time + pulseCooldownSeconds;
            CacheRenderers();
        }

        private void Update()
        {
            UpdateFlash(Time.deltaTime);

            if (_isDead || _isPaused || _targetSnake == null || !_targetSnake.IsAlive)
            {
                return;
            }

            TickModeState(Time.deltaTime);
            TickContactDamage(Time.deltaTime);
            TickPulseAttack();
        }

        public void Initialize(SnakeController snakeController)
        {
            _targetSnake = snakeController;
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
                _nextContactTime += pauseDuration;
                _nextChargeTime += pauseDuration;
                _nextPulseTime += pauseDuration;
            }

            _isPaused = isPaused;
        }

        public void ApplyExternalDamage(float amount)
        {
            ApplyDamage(amount);
        }

        private void TickModeState(float deltaTime)
        {
            switch (_mode)
            {
                case BossMode.Chase:
                    TickChase(deltaTime);
                    break;

                case BossMode.Charge:
                    TickCharge(deltaTime);
                    break;

                case BossMode.Recover:
                    TickRecover(deltaTime);
                    break;
            }
        }

        private void TickChase(float deltaTime)
        {
            var targetPoint = _targetSnake.TailPosition;
            var direction = targetPoint - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.0001f)
            {
                var normalized = direction.normalized;
                transform.position += normalized * (chaseSpeed * deltaTime);
                transform.rotation = Quaternion.LookRotation(normalized, Vector3.up);
            }

            if (Time.time < _nextChargeTime)
            {
                return;
            }

            var chargeTarget = (_targetSnake.HeadPosition - transform.position);
            chargeTarget.y = 0f;
            if (chargeTarget.sqrMagnitude <= 0.0001f)
            {
                chargeTarget = transform.forward;
            }

            _chargeDirection = chargeTarget.normalized;
            _mode = BossMode.Charge;
            _modeTimer = chargeDurationSeconds;
        }

        private void TickCharge(float deltaTime)
        {
            transform.position += _chargeDirection * (chargeSpeed * deltaTime);
            transform.rotation = Quaternion.LookRotation(_chargeDirection, Vector3.up);

            _modeTimer -= deltaTime;
            if (_modeTimer > 0f)
            {
                return;
            }

            _mode = BossMode.Recover;
            _modeTimer = recoverDurationSeconds;
            _nextChargeTime = Time.time + chargeCooldownSeconds;
        }

        private void TickRecover(float deltaTime)
        {
            _modeTimer -= deltaTime;
            if (_modeTimer <= 0f)
            {
                _mode = BossMode.Chase;
            }
        }

        private void TickContactDamage(float deltaTime)
        {
            var headDistance = FlatDistance(_targetSnake.HeadPosition, transform.position);
            if (headDistance <= contactRadius + _targetSnake.HeadContactRadius)
            {
                ApplyDamage(_targetSnake.HeadImpactDamage * headDamageTakenMultiplier);

                if (Time.time >= _nextContactTime)
                {
                    _targetSnake.ApplyDamage(
                        contactDamage,
                        SnakeContactZone.Head,
                        SnakeDamageSource.BossHeadImpact);
                    _nextContactTime = Time.time + contactTickSeconds;
                }

                return;
            }

            _targetSnake.TryGetClosestContactSample(transform.position, out var sample);
            var combinedRadius = contactRadius + sample.Radius;
            if (sample.SqrDistance > combinedRadius * combinedRadius)
            {
                return;
            }

            ApplyDamage(_targetSnake.BodyDamagePerSecond * bodyDamageTakenMultiplier * deltaTime);

            if (Time.time < _nextContactTime)
            {
                return;
            }

            _targetSnake.ApplyDamage(
                contactDamage,
                sample.Zone,
                SnakeDamageSource.BossBodyContact);
            _nextContactTime = Time.time + contactTickSeconds;
        }

        private void TickPulseAttack()
        {
            if (Time.time < _nextPulseTime)
            {
                return;
            }

            var distanceToSnake = FlatDistance(transform.position, _targetSnake.HeadPosition);
            if (distanceToSnake <= pulseRadius)
            {
                _targetSnake.ApplyDamage(
                    pulseDamage,
                    SnakeContactZone.Body,
                    SnakeDamageSource.BossPulse);
            }

            _nextPulseTime = Time.time + pulseCooldownSeconds;
        }

        private void ApplyDamage(float amount)
        {
            if (amount <= 0f || _isDead)
            {
                return;
            }

            _currentHealth -= amount;
            _flashTimer = flashDurationSeconds;
            RefreshColors(1f);

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

            RefreshColors(normalized);
        }

        private void RefreshColors(float normalized)
        {
            for (var i = 0; i < _renderers.Length; i++)
            {
                var renderer = _renderers[i];
                if (renderer == null)
                {
                    continue;
                }

                renderer.material.color = Color.Lerp(_baseColors[i], flashColor, normalized);
            }
        }

        private static float FlatDistance(Vector3 a, Vector3 b)
        {
            var delta = a - b;
            delta.y = 0f;
            return delta.magnitude;
        }
    }
}
