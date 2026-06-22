using System;
using System.Collections.Generic;
using SnakeRoguelite.Core;
using SnakeRoguelite.Gameplay.Enemies;
using SnakeRoguelite.Meta;
using SnakeRoguelite.Gameplay.Pickups;
using SnakeRoguelite.Gameplay.Powers;
using SnakeRoguelite.Gameplay.Snake;
using SnakeRoguelite.Gameplay.Waves;
using UnityEngine;

namespace SnakeRoguelite.Gameplay.Run
{
    public sealed class PrototypeRunController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameBootstrap gameBootstrap;
        [SerializeField] private SnakeController snakeController;
        [SerializeField] private PrototypePowerController powerController;
        [SerializeField] private PrototypeMetaProgressionController metaProgressionController;
        [SerializeField] private ChaserEnemy chaserEnemyPrefab;
        [SerializeField] private DasherEnemy dasherEnemyPrefab;
        [SerializeField] private TankEnemy tankEnemyPrefab;
        [SerializeField] private PrototypeBossEnemy bossEnemyPrefab;
        [SerializeField] private PrototypeXpPickup xpPickupPrefab;
        [SerializeField] private Transform enemyRoot;
        [SerializeField] private Transform bossSpawnPoint;

        [Header("Spawn")]
        [SerializeField] private float spawnRadiusMin = 5f;
        [SerializeField] private float spawnRadiusMax = 10f;
        [SerializeField] private bool autoAdvanceWaves = true;
        [SerializeField] private bool autoResolveDrafts;
        [SerializeField, Min(1)] private int immediateSpawnCount = 2;

        private readonly List<PrototypeEnemyBase> _activeEnemies = new();
        private readonly List<PrototypeXpPickup> _activeXpPickups = new();
        private readonly List<PowerDefinition> _currentDraftChoices = new();
        private PrototypeBossEnemy _activeBoss;
        private bool _isGameplayPaused;
        private int _queuedChaserSpawns;
        private int _queuedDasherSpawns;
        private int _queuedTankSpawns;
        private float _waveElapsedSeconds;
        private float _waveDurationSeconds;
        private float _spawnIntervalSeconds;
        private float _spawnAccumulatorSeconds;

        public event Action<int> WaveStarted;
        public event Action<IReadOnlyList<PowerDefinition>> DraftOpened;
        public event Action<PowerDefinition> PowerSelected;
        public event Action<PrototypeEnemyBase> EnemyDefeated;
        public event Action RunStarted;
        public event Action BossStarted;
        public event Action<bool> RunEnded;
        public event Action<Vector3, int> XpPickupCollected;
        public event Action<Vector3, float, bool> EnemyDamageFeedback;
        public event Action<Vector3, float, bool> BossDamageFeedback;

        public IReadOnlyList<PowerDefinition> CurrentDraftChoices => _currentDraftChoices;
        public bool IsAwaitingDraftSelection => _currentDraftChoices.Count > 0;
        public RunSession RunSession => gameBootstrap != null ? gameBootstrap.RunSession : null;
        public PrototypeBossEnemy ActiveBoss => _activeBoss;
        public int ActiveEnemyCount => _activeEnemies.Count + GetQueuedSpawnCount();
        public int PendingEnemyCount => GetQueuedSpawnCount();
        public int ActivePickupCount => _activeXpPickups.Count;
        public float CurrentWaveElapsedSeconds => _waveElapsedSeconds;
        public float CurrentWaveDurationSeconds => _waveDurationSeconds;
        public float CurrentWaveProgressNormalized =>
            _waveDurationSeconds <= 0f
                ? 0f
                : Mathf.Clamp01(_waveElapsedSeconds / _waveDurationSeconds);

        private void Start()
        {
            ResolveReferences();
            if (gameBootstrap == null || snakeController == null)
            {
                return;
            }

            snakeController.DamageTaken += OnSnakeDamaged;
            snakeController.Died += OnSnakeDied;
            if (powerController != null)
            {
                powerController.Initialize(gameBootstrap, snakeController, this);
            }

            RestartRun();
        }

        private void Update()
        {
            if (_isGameplayPaused || gameBootstrap == null)
            {
                return;
            }

            var state = gameBootstrap.RunSession.State;
            if (state.Phase != RunPhase.InWave)
            {
                return;
            }

            TickWaveSpawning(Time.deltaTime);
        }

        private void OnDestroy()
        {
            if (snakeController != null)
            {
                snakeController.DamageTaken -= OnSnakeDamaged;
                snakeController.Died -= OnSnakeDied;
            }

            if (_activeBoss != null)
            {
                _activeBoss.Damaged -= OnBossDamaged;
                _activeBoss.Defeated -= OnBossDefeated;
            }
        }

        public void RestartRun()
        {
            ResolveReferences();
            if (gameBootstrap == null || snakeController == null)
            {
                return;
            }

            _currentDraftChoices.Clear();
            ClearEnemies();
            ClearBoss();
            ClearPickups();
            ResetWaveSpawnState();
            SetGameplayPaused(false);

            gameBootstrap.StartRun();
            snakeController.Initialize(gameBootstrap.RunSession.State.CurrentHealth);
            ApplySelectedRelic();
            if (powerController != null)
            {
                powerController.Initialize(gameBootstrap, snakeController, this);
            }

            RunStarted?.Invoke();
            SpawnCurrentWave();
        }

        private void OnSnakeDamaged(int damageAmount, int currentHealth)
        {
            gameBootstrap.RunSession.State.ApplyDamage(damageAmount);
        }

        private void OnSnakeDied()
        {
            gameBootstrap.RunSession.State.FailRun();
            ClearEnemies();
            ClearBoss();
            ClearPickups();
            ResetWaveSpawnState();
            RunEnded?.Invoke(false);
        }

        private void SpawnCurrentWave()
        {
            ClearEnemies();

            var wave = gameBootstrap.RunSession.GetCurrentWave();
            if (wave == null)
            {
                ResetWaveSpawnState();
                return;
            }

            ConfigureWaveSpawnState(wave);
            SpawnImmediateWaveBurst();

            WaveStarted?.Invoke(gameBootstrap.RunSession.State.CurrentWaveIndex + 1);
        }

        private void SpawnEnemy(PrototypeEnemyBase enemyPrefab)
        {
            if (enemyPrefab == null)
            {
                return;
            }

            var direction = Random.insideUnitCircle.normalized;
            if (direction.sqrMagnitude <= 0.0001f)
            {
                direction = Vector2.right;
            }

            var distance = Random.Range(spawnRadiusMin, spawnRadiusMax);
            var spawnPosition = snakeController.HeadPosition + new Vector3(
                direction.x * distance,
                0f,
                direction.y * distance);

            var enemy = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity, enemyRoot);
            enemy.Initialize(snakeController);
            enemy.Damaged += OnEnemyDamaged;
            enemy.Defeated += OnEnemyDefeated;
            _activeEnemies.Add(enemy);
        }

        private void OnEnemyDefeated(PrototypeEnemyBase enemy)
        {
            enemy.Damaged -= OnEnemyDamaged;
            enemy.Defeated -= OnEnemyDefeated;
            _activeEnemies.Remove(enemy);
            EnemyDefeated?.Invoke(enemy);
            snakeController.Grow(enemy.GrowthRewardSegments + snakeController.BonusGrowthPerKill);
            SpawnXpPickup(enemy.transform.position, enemy.ExperienceReward);

            if (_activeEnemies.Count > 0 || GetQueuedSpawnCount() > 0 || !autoAdvanceWaves)
            {
                return;
            }

            AdvanceWave();
        }

        private void OnEnemyDamaged(PrototypeEnemyBase enemy, float damage, bool wasKilled)
        {
            if (enemy == null)
            {
                return;
            }

            EnemyDamageFeedback?.Invoke(enemy.transform.position, damage, wasKilled);
        }

        private void AdvanceWave()
        {
            var runSession = gameBootstrap.RunSession;
            var canContinue = runSession.TryAdvance();
            if (!canContinue)
            {
                ResetWaveSpawnState();
                StartBossEncounter();
                return;
            }

            runSession.BeginNextWave();
            SpawnCurrentWave();
        }

        public void ApplyAreaDamage(Vector3 center, float radius, float damage)
        {
            var sqrRadius = radius * radius;
            for (var i = _activeEnemies.Count - 1; i >= 0; i--)
            {
                var enemy = _activeEnemies[i];
                if (enemy == null)
                {
                    _activeEnemies.RemoveAt(i);
                    continue;
                }

                var delta = enemy.transform.position - center;
                delta.y = 0f;
                if (delta.sqrMagnitude > sqrRadius)
                {
                    continue;
                }

                enemy.ApplyExternalDamage(damage);
            }

            if (_activeBoss == null)
            {
                return;
            }

            var bossDelta = _activeBoss.transform.position - center;
            bossDelta.y = 0f;
            if (bossDelta.sqrMagnitude <= sqrRadius)
            {
                _activeBoss.ApplyExternalDamage(damage);
            }
        }

        public void SelectDraftChoice(int index)
        {
            if (index < 0 || index >= _currentDraftChoices.Count)
            {
                return;
            }

            var selectedPower = _currentDraftChoices[index];
            _currentDraftChoices.Clear();

            var runSession = gameBootstrap.RunSession;
            runSession.SelectPower(selectedPower);
            if (powerController != null)
            {
                powerController.ApplyPower(selectedPower);
            }

            PowerSelected?.Invoke(selectedPower);

            var hasAnotherPendingDraft = runSession.FinalizeDraftSelection();
            if (hasAnotherPendingDraft)
            {
                PopulateDraftChoices();
                SetGameplayPaused(true);
                return;
            }

            _currentDraftChoices.Clear();
            SetGameplayPaused(false);

            if (runSession.State.Phase == RunPhase.InWave &&
                _activeEnemies.Count == 0 &&
                GetQueuedSpawnCount() == 0 &&
                autoAdvanceWaves)
            {
                AdvanceWave();
            }
        }

        private void ClearEnemies()
        {
            for (var i = _activeEnemies.Count - 1; i >= 0; i--)
            {
                var enemy = _activeEnemies[i];
                if (enemy == null)
                {
                    continue;
                }

                enemy.Damaged -= OnEnemyDamaged;
                enemy.Defeated -= OnEnemyDefeated;
                Destroy(enemy.gameObject);
            }

            _activeEnemies.Clear();
        }

        private void ClearPickups()
        {
            for (var i = _activeXpPickups.Count - 1; i >= 0; i--)
            {
                var pickup = _activeXpPickups[i];
                if (pickup == null)
                {
                    continue;
                }

                pickup.Collected -= OnXpPickupCollected;
                Destroy(pickup.gameObject);
            }

            _activeXpPickups.Clear();
        }

        private void StartBossEncounter()
        {
            var runSession = gameBootstrap.RunSession;
            runSession.BeginBossEncounter();
            SpawnBoss();
            BossStarted?.Invoke();
        }

        private void SpawnBoss()
        {
            ClearBoss();
            if (bossEnemyPrefab == null)
            {
                return;
            }

            var spawnPosition = bossSpawnPoint != null
                ? bossSpawnPoint.position
                : snakeController.HeadPosition + new Vector3(0f, 0f, 8f);

            _activeBoss = Instantiate(
                bossEnemyPrefab,
                spawnPosition,
                Quaternion.identity,
                enemyRoot);

            _activeBoss.Initialize(snakeController);
            _activeBoss.SetPaused(_isGameplayPaused);
            _activeBoss.Damaged += OnBossDamaged;
            _activeBoss.Defeated += OnBossDefeated;
        }

        private void OnBossDefeated(PrototypeBossEnemy boss)
        {
            if (_activeBoss == boss)
            {
                _activeBoss = null;
            }

            boss.Damaged -= OnBossDamaged;
            boss.Defeated -= OnBossDefeated;
            gameBootstrap.RunSession.CompleteBoss();
            RunEnded?.Invoke(true);
        }

        private void OnBossDamaged(PrototypeBossEnemy boss, float damage, bool wasKilled)
        {
            if (boss == null)
            {
                return;
            }

            BossDamageFeedback?.Invoke(boss.transform.position, damage, wasKilled);
        }

        private void ClearBoss()
        {
            if (_activeBoss == null)
            {
                return;
            }

            _activeBoss.Damaged -= OnBossDamaged;
            _activeBoss.Defeated -= OnBossDefeated;
            Destroy(_activeBoss.gameObject);
            _activeBoss = null;
        }

        private void PopulateDraftChoices()
        {
            _currentDraftChoices.Clear();

            var source = metaProgressionController != null
                ? metaProgressionController.GetUnlockedPowers()
                : null;
            var choices = gameBootstrap.RunSession.RollDraftChoices(source);
            for (var i = 0; i < choices.Count; i++)
            {
                _currentDraftChoices.Add(choices[i]);
            }

            if (_currentDraftChoices.Count == 0)
            {
                var hasAnotherPendingDraft = gameBootstrap.RunSession.FinalizeDraftSelection();
                if (hasAnotherPendingDraft)
                {
                    PopulateDraftChoices();
                }

                return;
            }

            DraftOpened?.Invoke(_currentDraftChoices);

            if (autoResolveDrafts && _currentDraftChoices.Count > 0)
            {
                SelectDraftChoice(0);
            }
        }

        private void SetGameplayPaused(bool isPaused)
        {
            _isGameplayPaused = isPaused;
            snakeController.SetPaused(isPaused);

            for (var i = 0; i < _activeEnemies.Count; i++)
            {
                var enemy = _activeEnemies[i];
                if (enemy == null)
                {
                    continue;
                }

                enemy.SetPaused(isPaused);
            }

            if (_activeBoss != null)
            {
                _activeBoss.SetPaused(isPaused);
            }

            for (var i = 0; i < _activeXpPickups.Count; i++)
            {
                var pickup = _activeXpPickups[i];
                if (pickup == null)
                {
                    continue;
                }

                pickup.SetPaused(isPaused);
            }
        }

        private void ResolveReferences()
        {
            if (gameBootstrap == null)
            {
                gameBootstrap = GetComponent<GameBootstrap>();
            }

            if (metaProgressionController == null)
            {
                metaProgressionController = GetComponent<PrototypeMetaProgressionController>();
            }
        }

        private void ApplySelectedRelic()
        {
            if (metaProgressionController == null || snakeController == null)
            {
                return;
            }

            var relic = metaProgressionController.SelectedRelic;
            if (relic == null)
            {
                return;
            }

            switch (relic.EffectType)
            {
                case RelicEffectType.ExtraStartingSegments:
                    snakeController.Grow(Mathf.RoundToInt(relic.PrimaryValue));
                    break;

                case RelicEffectType.StartingMagnet:
                    snakeController.AddPickupMagnet(relic.PrimaryValue);
                    break;

                case RelicEffectType.OpeningBodyShield:
                    snakeController.AddBodyShield(
                        relic.PrimaryValue,
                        Mathf.RoundToInt(relic.SecondaryValue),
                        relic.TertiaryValue);
                    break;
            }
        }

        private void TickWaveSpawning(float deltaTime)
        {
            _waveElapsedSeconds += deltaTime;

            if (GetQueuedSpawnCount() <= 0)
            {
                return;
            }

            _spawnAccumulatorSeconds += deltaTime;
            while (_spawnAccumulatorSeconds >= _spawnIntervalSeconds && GetQueuedSpawnCount() > 0)
            {
                _spawnAccumulatorSeconds -= _spawnIntervalSeconds;
                TrySpawnQueuedEnemy();
            }
        }

        private void ConfigureWaveSpawnState(WaveDefinition wave)
        {
            _queuedChaserSpawns = Mathf.Max(0, wave.ChaserCount);
            _queuedDasherSpawns = Mathf.Max(0, wave.DasherCount);
            _queuedTankSpawns = Mathf.Max(0, wave.TankCount);
            _waveElapsedSeconds = 0f;
            _waveDurationSeconds = Mathf.Max(1f, wave.DurationSeconds);
            _spawnAccumulatorSeconds = 0f;

            var totalSpawnCount = GetQueuedSpawnCount();
            var delayedSpawnCount = Mathf.Max(1, totalSpawnCount - Mathf.Max(0, immediateSpawnCount));
            _spawnIntervalSeconds = _waveDurationSeconds / delayedSpawnCount;
        }

        private void SpawnImmediateWaveBurst()
        {
            var spawnCount = Mathf.Min(immediateSpawnCount, GetQueuedSpawnCount());
            for (var i = 0; i < spawnCount; i++)
            {
                TrySpawnQueuedEnemy();
            }
        }

        private void TrySpawnQueuedEnemy()
        {
            var totalQueued = GetQueuedSpawnCount();
            if (totalQueued <= 0)
            {
                return;
            }

            var roll = UnityEngine.Random.Range(0, totalQueued);
            if (roll < _queuedChaserSpawns)
            {
                _queuedChaserSpawns -= 1;
                SpawnEnemy(chaserEnemyPrefab);
                return;
            }

            roll -= _queuedChaserSpawns;
            if (roll < _queuedDasherSpawns)
            {
                _queuedDasherSpawns -= 1;
                SpawnEnemy(dasherEnemyPrefab);
                return;
            }

            _queuedTankSpawns = Mathf.Max(0, _queuedTankSpawns - 1);
            SpawnEnemy(tankEnemyPrefab);
        }

        private int GetQueuedSpawnCount()
        {
            return _queuedChaserSpawns + _queuedDasherSpawns + _queuedTankSpawns;
        }

        private void ResetWaveSpawnState()
        {
            _queuedChaserSpawns = 0;
            _queuedDasherSpawns = 0;
            _queuedTankSpawns = 0;
            _waveElapsedSeconds = 0f;
            _waveDurationSeconds = 0f;
            _spawnIntervalSeconds = 0f;
            _spawnAccumulatorSeconds = 0f;
        }

        private void SpawnXpPickup(Vector3 worldPosition, int experienceAmount)
        {
            if (experienceAmount <= 0)
            {
                return;
            }

            PrototypeXpPickup pickup;
            if (xpPickupPrefab != null)
            {
                pickup = Instantiate(xpPickupPrefab, worldPosition, Quaternion.identity, enemyRoot);
            }
            else
            {
                pickup = CreateFallbackXpPickup(worldPosition, enemyRoot);
            }

            pickup.Initialize(snakeController, experienceAmount);
            pickup.SetPaused(_isGameplayPaused);
            pickup.Collected += OnXpPickupCollected;
            _activeXpPickups.Add(pickup);
        }

        private void OnXpPickupCollected(PrototypeXpPickup pickup)
        {
            if (pickup == null)
            {
                return;
            }

            pickup.Collected -= OnXpPickupCollected;
            _activeXpPickups.Remove(pickup);
            XpPickupCollected?.Invoke(pickup.transform.position, pickup.ExperienceAmount);

            var leveledUp = gameBootstrap.RunSession.ProcessXpGain(pickup.ExperienceAmount);
            if (!leveledUp)
            {
                return;
            }

            PopulateDraftChoices();
            SetGameplayPaused(true);
        }

        private static PrototypeXpPickup CreateFallbackXpPickup(Vector3 worldPosition, Transform parent)
        {
            var pickupObject = new GameObject("XpPickup");
            pickupObject.transform.position = worldPosition;
            if (parent != null)
            {
                pickupObject.transform.SetParent(parent, true);
            }

            return pickupObject.AddComponent<PrototypeXpPickup>();
        }
    }
}
