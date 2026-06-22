using UnityEngine;

namespace SnakeRoguelite.Gameplay.Enemies
{
    public sealed class DasherEnemy : PrototypeEnemyBase
    {
        private enum DashState
        {
            Chase = 0,
            Windup = 1,
            Dash = 2,
            Recover = 3,
        }

        [Header("Dash")]
        [SerializeField, Min(0.5f)] private float chaseSpeed = 4.2f;
        [SerializeField, Min(1f)] private float dashSpeed = 12f;
        [SerializeField, Min(0.05f)] private float windupSeconds = 0.4f;
        [SerializeField, Min(0.05f)] private float dashDurationSeconds = 0.35f;
        [SerializeField, Min(0.05f)] private float recoverSeconds = 0.45f;
        [SerializeField, Min(0.05f)] private float dashCooldownSeconds = 2.4f;

        private DashState _state = DashState.Chase;
        private float _stateTimer;
        private float _nextDashTime;
        private Vector3 _dashDirection = Vector3.forward;

        protected override void OnInitialized()
        {
            _state = DashState.Chase;
            _stateTimer = 0f;
            _nextDashTime = Time.time + dashCooldownSeconds;
        }

        protected override void OnResumed(float pauseDuration)
        {
            _nextDashTime += pauseDuration;
        }

        protected override void TickMovement(float deltaTime)
        {
            if (TargetSnake == null)
            {
                return;
            }

            switch (_state)
            {
                case DashState.Chase:
                    TickChase(deltaTime);
                    break;

                case DashState.Windup:
                    TickWindup(deltaTime);
                    break;

                case DashState.Dash:
                    TickDash(deltaTime);
                    break;

                case DashState.Recover:
                    TickRecover(deltaTime);
                    break;
            }
        }

        private void TickChase(float deltaTime)
        {
            MoveTowards(TargetSnake.HeadPosition, chaseSpeed, deltaTime);
            if (Time.time < _nextDashTime)
            {
                return;
            }

            _state = DashState.Windup;
            _stateTimer = windupSeconds;
        }

        private void TickWindup(float deltaTime)
        {
            FaceTowards(TargetSnake.HeadPosition);
            _stateTimer -= deltaTime;
            if (_stateTimer > 0f)
            {
                return;
            }

            var direction = TargetSnake.HeadPosition - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude <= 0.0001f)
            {
                direction = transform.forward;
            }

            _dashDirection = direction.normalized;
            _state = DashState.Dash;
            _stateTimer = dashDurationSeconds;
        }

        private void TickDash(float deltaTime)
        {
            transform.position += _dashDirection * (dashSpeed * deltaTime);
            transform.rotation = Quaternion.LookRotation(_dashDirection, Vector3.up);

            _stateTimer -= deltaTime;
            if (_stateTimer > 0f)
            {
                return;
            }

            _state = DashState.Recover;
            _stateTimer = recoverSeconds;
            _nextDashTime = Time.time + dashCooldownSeconds;
        }

        private void TickRecover(float deltaTime)
        {
            _stateTimer -= deltaTime;
            if (_stateTimer <= 0f)
            {
                _state = DashState.Chase;
            }
        }
    }
}
