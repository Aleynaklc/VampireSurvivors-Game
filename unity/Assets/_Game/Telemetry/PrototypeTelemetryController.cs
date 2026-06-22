using System;
using System.Collections.Generic;
using System.IO;
using SnakeRoguelite.Core;
using SnakeRoguelite.Gameplay.Powers;
using SnakeRoguelite.Gameplay.Run;
using SnakeRoguelite.Gameplay.Snake;
using SnakeRoguelite.Meta;
using UnityEngine;

namespace SnakeRoguelite.Telemetry
{
    public sealed class PrototypeTelemetryController : MonoBehaviour
    {
        [Serializable]
        public sealed class RunRecord
        {
            public string RunId;
            public string CompletedAtUtc;
            public bool Cleared;
            public string FailReason;
            public float DurationSeconds;
            public int WaveReached;
            public int LevelReached;
            public int Kills;
            public int PeakSegments;
            public int DamageEvents;
            public int TotalDamageTaken;
            public int XpPickupCount;
            public int XpCollected;
            public int PowersChosen;
            public string SelectedRelicId;
            public List<string> PowerIds = new();
        }

        [Serializable]
        private sealed class RunRecordCollection
        {
            public List<RunRecord> Runs = new();
        }

        [Header("References")]
        [SerializeField] private GameBootstrap gameBootstrap;
        [SerializeField] private PrototypeRunController prototypeRunController;
        [SerializeField] private SnakeController snakeController;
        [SerializeField] private PrototypeMetaProgressionController prototypeMetaProgressionController;

        [Header("Storage")]
        [SerializeField] private string fileName = "prototype_run_telemetry.json";
        [SerializeField, Min(1)] private int maxStoredRuns = 50;

        private readonly List<string> _currentPowerIds = new();
        private string _telemetryFilePath = string.Empty;
        private bool _subscribed;
        private bool _runStateSubscribed;
        private string _currentRunId = string.Empty;
        private float _currentRunStartRealtime;
        private int _currentKills;
        private int _currentWaveReached;
        private int _currentPeakSegments;
        private int _currentDamageEvents;
        private int _currentTotalDamageTaken;
        private int _currentXpPickupCount;
        private int _currentXpCollected;
        private string _currentFailReason = "None";
        private RunRecord _lastCompletedRun;
        private int _storedRunCount;

        public RunRecord LastCompletedRun => _lastCompletedRun;
        public string TelemetryFilePath => _telemetryFilePath;
        public int StoredRunCount => _storedRunCount;

        private void Awake()
        {
            if (prototypeMetaProgressionController == null)
            {
                prototypeMetaProgressionController = FindObjectOfType<PrototypeMetaProgressionController>();
            }

            _telemetryFilePath = Path.Combine(Application.persistentDataPath, fileName);
            RefreshStoredRunCount();
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void Start()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Subscribe()
        {
            if (!_subscribed)
            {
                if (prototypeRunController != null)
                {
                    prototypeRunController.RunStarted += OnRunStarted;
                    prototypeRunController.WaveStarted += OnWaveStarted;
                    prototypeRunController.PowerSelected += OnPowerSelected;
                    prototypeRunController.EnemyDefeated += OnEnemyDefeated;
                    prototypeRunController.XpPickupCollected += OnXpPickupCollected;
                    prototypeRunController.RunEnded += OnRunEnded;
                }

                if (snakeController != null)
                {
                    snakeController.Grew += OnSnakeGrew;
                    snakeController.DamageTakenDetailed += OnSnakeDamageTakenDetailed;
                }

                _subscribed = true;
            }

            if (!_runStateSubscribed && gameBootstrap != null && gameBootstrap.RunSession != null)
            {
                gameBootstrap.RunSession.State.LevelChanged += OnLevelChanged;
                _runStateSubscribed = true;
            }
        }

        private void Unsubscribe()
        {
            if (_subscribed)
            {
                if (prototypeRunController != null)
                {
                    prototypeRunController.RunStarted -= OnRunStarted;
                    prototypeRunController.WaveStarted -= OnWaveStarted;
                    prototypeRunController.PowerSelected -= OnPowerSelected;
                    prototypeRunController.EnemyDefeated -= OnEnemyDefeated;
                    prototypeRunController.XpPickupCollected -= OnXpPickupCollected;
                    prototypeRunController.RunEnded -= OnRunEnded;
                }

                if (snakeController != null)
                {
                    snakeController.Grew -= OnSnakeGrew;
                    snakeController.DamageTakenDetailed -= OnSnakeDamageTakenDetailed;
                }
            }

            if (_runStateSubscribed && gameBootstrap != null && gameBootstrap.RunSession != null)
            {
                gameBootstrap.RunSession.State.LevelChanged -= OnLevelChanged;
            }

            _subscribed = false;
            _runStateSubscribed = false;
        }

        private void OnRunStarted()
        {
            _currentRunId = Guid.NewGuid().ToString("N");
            _currentRunStartRealtime = Time.realtimeSinceStartup;
            _currentKills = 0;
            _currentWaveReached = 1;
            _currentPeakSegments = snakeController != null ? snakeController.SegmentCount : 0;
            _currentDamageEvents = 0;
            _currentTotalDamageTaken = 0;
            _currentXpPickupCount = 0;
            _currentXpCollected = 0;
            _currentFailReason = "None";
            _currentPowerIds.Clear();
        }

        private void OnWaveStarted(int waveIndex)
        {
            _currentWaveReached = Mathf.Max(_currentWaveReached, waveIndex);
        }

        private void OnPowerSelected(PowerDefinition power)
        {
            if (power == null)
            {
                return;
            }

            _currentPowerIds.Add(power.PowerId);
        }

        private void OnEnemyDefeated(Gameplay.Enemies.PrototypeEnemyBase enemy)
        {
            _currentKills += 1;
        }

        private void OnXpPickupCollected(Vector3 worldPosition, int amount)
        {
            _currentXpPickupCount += 1;
            _currentXpCollected += Mathf.Max(0, amount);
        }

        private void OnSnakeGrew(int amount)
        {
            if (snakeController == null)
            {
                return;
            }

            _currentPeakSegments = Mathf.Max(_currentPeakSegments, snakeController.SegmentCount);
        }

        private void OnSnakeDamageTakenDetailed(
            int damageAmount,
            int currentHealth,
            SnakeContactZone zone,
            SnakeDamageSource damageSource)
        {
            _currentDamageEvents += 1;
            _currentTotalDamageTaken += Mathf.Max(0, damageAmount);

            if (currentHealth > 0)
            {
                return;
            }

            _currentFailReason = FormatFailReason(damageSource, zone);
        }

        private void OnLevelChanged(int level)
        {
            if (snakeController == null)
            {
                return;
            }

            _currentPeakSegments = Mathf.Max(_currentPeakSegments, snakeController.SegmentCount);
        }

        private void OnRunEnded(bool cleared)
        {
            var runSession = gameBootstrap != null ? gameBootstrap.RunSession : null;
            if (runSession == null)
            {
                return;
            }

            var record = new RunRecord
            {
                RunId = string.IsNullOrWhiteSpace(_currentRunId) ? Guid.NewGuid().ToString("N") : _currentRunId,
                CompletedAtUtc = DateTime.UtcNow.ToString("o"),
                Cleared = cleared,
                FailReason = cleared ? "Cleared" : _currentFailReason,
                DurationSeconds = Mathf.Max(0f, Time.realtimeSinceStartup - _currentRunStartRealtime),
                WaveReached = _currentWaveReached,
                LevelReached = runSession.State.CurrentLevel,
                Kills = _currentKills,
                PeakSegments = Mathf.Max(
                    _currentPeakSegments,
                    snakeController != null ? snakeController.SegmentCount : _currentPeakSegments),
                DamageEvents = _currentDamageEvents,
                TotalDamageTaken = _currentTotalDamageTaken,
                XpPickupCount = _currentXpPickupCount,
                XpCollected = _currentXpCollected,
                PowersChosen = _currentPowerIds.Count,
                SelectedRelicId = GetSelectedRelicId(),
                PowerIds = new List<string>(_currentPowerIds)
            };

            _lastCompletedRun = record;
            PersistRun(record);
        }

        private void PersistRun(RunRecord record)
        {
            try
            {
                var directory = Path.GetDirectoryName(_telemetryFilePath);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var collection = LoadCollection();
                collection.Runs.Add(record);
                if (collection.Runs.Count > maxStoredRuns)
                {
                    collection.Runs.RemoveRange(0, collection.Runs.Count - maxStoredRuns);
                }

                var json = JsonUtility.ToJson(collection, true);
                File.WriteAllText(_telemetryFilePath, json);
                _storedRunCount = collection.Runs.Count;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Telemetry write failed: {exception.Message}");
            }
        }

        private void RefreshStoredRunCount()
        {
            try
            {
                _storedRunCount = LoadCollection().Runs.Count;
            }
            catch
            {
                _storedRunCount = 0;
            }
        }

        private RunRecordCollection LoadCollection()
        {
            if (string.IsNullOrWhiteSpace(_telemetryFilePath) || !File.Exists(_telemetryFilePath))
            {
                return new RunRecordCollection();
            }

            var json = File.ReadAllText(_telemetryFilePath);
            if (string.IsNullOrWhiteSpace(json))
            {
                return new RunRecordCollection();
            }

            var collection = JsonUtility.FromJson<RunRecordCollection>(json);
            return collection ?? new RunRecordCollection();
        }

        private static string FormatFailReason(SnakeDamageSource damageSource, SnakeContactZone zone)
        {
            return damageSource switch
            {
                SnakeDamageSource.EnemyHeadImpact => "EnemyHeadImpact",
                SnakeDamageSource.EnemyBodyContact when zone == SnakeContactZone.Tail => "EnemyTailPressure",
                SnakeDamageSource.EnemyBodyContact => "EnemyBodyContact",
                SnakeDamageSource.BossHeadImpact => "BossHeadImpact",
                SnakeDamageSource.BossBodyContact when zone == SnakeContactZone.Tail => "BossTailPressure",
                SnakeDamageSource.BossBodyContact => "BossBodyContact",
                SnakeDamageSource.BossPulse => "BossPulse",
                _ when zone == SnakeContactZone.Tail => "TailDamage",
                _ when zone == SnakeContactZone.Head => "HeadDamage",
                _ => "Unknown",
            };
        }

        private string GetSelectedRelicId()
        {
            var relic = prototypeMetaProgressionController != null
                ? prototypeMetaProgressionController.SelectedRelic
                : null;

            return relic != null ? relic.RelicId : string.Empty;
        }
    }
}
