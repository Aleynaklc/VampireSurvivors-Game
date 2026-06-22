using SnakeRoguelite.Core;
using SnakeRoguelite.Gameplay.Enemies;
using SnakeRoguelite.Gameplay.Run;
using SnakeRoguelite.Gameplay.Snake;
using UnityEngine;

namespace SnakeRoguelite.Gameplay.Powers
{
    public sealed class PrototypePowerController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameBootstrap gameBootstrap;
        [SerializeField] private SnakeController snakeController;
        [SerializeField] private PrototypeRunController runController;

        private int _sparkMoltStacks;
        private float _sparkMoltRadius;
        private float _sparkMoltDamage;
        private float _overloadRadius;
        private float _overloadDamage;
        private int _overloadKillThreshold;
        private int _overloadKillCount;

        public void Initialize(
            GameBootstrap bootstrap,
            SnakeController snake,
            PrototypeRunController prototypeRunController)
        {
            if (gameBootstrap != null)
            {
                gameBootstrap.RunSession.State.LevelChanged -= OnLevelChanged;
            }

            if (runController != null)
            {
                runController.EnemyDefeated -= OnEnemyDefeated;
            }

            gameBootstrap = bootstrap;
            snakeController = snake;
            runController = prototypeRunController;
            ResetRunState();

            if (gameBootstrap != null)
            {
                gameBootstrap.RunSession.State.LevelChanged += OnLevelChanged;
            }

            if (runController != null)
            {
                runController.EnemyDefeated += OnEnemyDefeated;
            }
        }

        private void OnDestroy()
        {
            if (gameBootstrap != null)
            {
                gameBootstrap.RunSession.State.LevelChanged -= OnLevelChanged;
            }

            if (runController != null)
            {
                runController.EnemyDefeated -= OnEnemyDefeated;
            }
        }

        public void ApplyPower(PowerDefinition powerDefinition)
        {
            if (powerDefinition == null || snakeController == null)
            {
                return;
            }

            switch (powerDefinition.EffectType)
            {
                case PowerEffectType.TailSpike:
                    snakeController.AddTailSpike(
                        powerDefinition.PrimaryValue,
                        powerDefinition.SecondaryValue);
                    break;

                case PowerEffectType.BodyShield:
                    snakeController.AddBodyShield(
                        powerDefinition.PrimaryValue,
                        Mathf.RoundToInt(powerDefinition.SecondaryValue),
                        powerDefinition.TertiaryValue);
                    break;

                case PowerEffectType.FrenzyCoil:
                    snakeController.AddFrenzy(
                        powerDefinition.PrimaryValue,
                        powerDefinition.SecondaryValue,
                        powerDefinition.TertiaryValue);
                    break;

                case PowerEffectType.HungerScale:
                    snakeController.AddBonusGrowthPerKill(
                        Mathf.RoundToInt(powerDefinition.PrimaryValue));
                    break;

                case PowerEffectType.SparkMolt:
                    _sparkMoltStacks += 1;
                    _sparkMoltRadius = Mathf.Max(_sparkMoltRadius, powerDefinition.PrimaryValue);
                    _sparkMoltDamage += Mathf.Max(0f, powerDefinition.SecondaryValue);
                    TriggerSparkMoltPulse();
                    break;

                case PowerEffectType.MagnetTail:
                    snakeController.AddPickupMagnet(powerDefinition.PrimaryValue);
                    break;

                case PowerEffectType.Overload:
                    _overloadKillThreshold = Mathf.Max(
                        1,
                        _overloadKillThreshold == 0
                            ? Mathf.RoundToInt(powerDefinition.PrimaryValue)
                            : Mathf.Min(_overloadKillThreshold, Mathf.RoundToInt(powerDefinition.PrimaryValue)));
                    _overloadRadius = Mathf.Max(_overloadRadius, powerDefinition.SecondaryValue);
                    _overloadDamage += Mathf.Max(0f, powerDefinition.TertiaryValue);
                    break;
            }
        }

        public void ResetRunState()
        {
            _sparkMoltStacks = 0;
            _sparkMoltRadius = 0f;
            _sparkMoltDamage = 0f;
            _overloadRadius = 0f;
            _overloadDamage = 0f;
            _overloadKillThreshold = 0;
            _overloadKillCount = 0;
        }

        private void OnLevelChanged(int level)
        {
            if (level <= 1)
            {
                return;
            }

            TriggerSparkMoltPulse();
        }

        private void TriggerSparkMoltPulse()
        {
            if (_sparkMoltStacks <= 0 || runController == null || snakeController == null)
            {
                return;
            }

            runController.ApplyAreaDamage(
                snakeController.HeadPosition,
                _sparkMoltRadius,
                _sparkMoltDamage);
        }

        private void OnEnemyDefeated(PrototypeEnemyBase enemy)
        {
            if (_overloadKillThreshold <= 0 || runController == null || snakeController == null)
            {
                return;
            }

            _overloadKillCount += 1;
            if (_overloadKillCount < _overloadKillThreshold)
            {
                return;
            }

            _overloadKillCount = 0;
            runController.ApplyAreaDamage(
                snakeController.HeadPosition,
                _overloadRadius,
                _overloadDamage);
        }
    }
}
