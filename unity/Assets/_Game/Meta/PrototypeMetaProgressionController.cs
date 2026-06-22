using System;
using System.Collections.Generic;
using System.IO;
using SnakeRoguelite.Core;
using SnakeRoguelite.Gameplay.Powers;
using SnakeRoguelite.Gameplay.Run;
using UnityEngine;

namespace SnakeRoguelite.Meta
{
    public sealed class PrototypeMetaProgressionController : MonoBehaviour
    {
        [Serializable]
        private sealed class MetaProfileData
        {
            public int CurrencyBalance;
            public int LifetimeCurrencyEarned;
            public int TotalRuns;
            public int TotalClears;
            public List<string> UnlockedPowerIds = new();
            public List<string> UnlockedRelicIds = new();
            public string SelectedRelicId = string.Empty;
        }

        [Header("References")]
        [SerializeField] private GameBootstrap gameBootstrap;
        [SerializeField] private PrototypeRunController prototypeRunController;
        [SerializeField] private PowerLibrary powerLibrary;
        [SerializeField] private PrototypeRelicLibrary relicLibrary;

        [Header("Profile")]
        [SerializeField] private string fileName = "prototype_meta_progression.json";
        [SerializeField, Min(0)] private int startingCurrency = 0;
        [SerializeField, Min(1)] private int minimumUnlockedPowers = 4;

        private readonly List<PowerDefinition> _cachedUnlockedPowers = new();
        private readonly List<PrototypeRelicDefinition> _cachedUnlockedRelics = new();
        private MetaProfileData _profile;
        private string _profileFilePath = string.Empty;
        private bool _subscribed;
        private int _currentRunKills;

        public event Action ProfileChanged;

        public int CurrencyBalance => _profile != null ? _profile.CurrencyBalance : 0;
        public int LifetimeCurrencyEarned => _profile != null ? _profile.LifetimeCurrencyEarned : 0;
        public int TotalRuns => _profile != null ? _profile.TotalRuns : 0;
        public int TotalClears => _profile != null ? _profile.TotalClears : 0;
        public int LastRunReward { get; private set; }
        public string ProfileFilePath => _profileFilePath;
        public PrototypeRelicDefinition SelectedRelic =>
            _profile != null && relicLibrary != null
                ? relicLibrary.FindById(_profile.SelectedRelicId)
                : null;

        private void Awake()
        {
            if (gameBootstrap == null)
            {
                gameBootstrap = GetComponent<GameBootstrap>();
            }

            if (prototypeRunController == null)
            {
                prototypeRunController = GetComponent<PrototypeRunController>();
            }

            _profileFilePath = Path.Combine(Application.persistentDataPath, fileName);
            LoadProfile();
            EnsureUnlockDefaults();
            EnsureRelicUnlockDefaults();
            RebuildUnlockedPowerCache();
            RebuildUnlockedRelicCache();
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

        public IReadOnlyList<PowerDefinition> GetUnlockedPowers()
        {
            return _cachedUnlockedPowers;
        }

        public IReadOnlyList<PrototypeRelicDefinition> GetUnlockedRelics()
        {
            return _cachedUnlockedRelics;
        }

        public IReadOnlyList<PowerDefinition> GetNextUnlockCandidates(int count)
        {
            var result = new List<PowerDefinition>(Mathf.Max(0, count));
            if (powerLibrary == null || count <= 0)
            {
                return result;
            }

            var ordered = powerLibrary.GetOrderedPowers();
            for (var i = 0; i < ordered.Count && result.Count < count; i++)
            {
                var power = ordered[i];
                if (power == null || IsPowerUnlocked(power))
                {
                    continue;
                }

                result.Add(power);
            }

            return result;
        }

        public IReadOnlyList<PrototypeRelicDefinition> GetNextRelicUnlockCandidates(int count)
        {
            var result = new List<PrototypeRelicDefinition>(Mathf.Max(0, count));
            if (relicLibrary == null || count <= 0)
            {
                return result;
            }

            var ordered = relicLibrary.GetOrderedRelics();
            for (var i = 0; i < ordered.Count && result.Count < count; i++)
            {
                var relic = ordered[i];
                if (relic == null || IsRelicUnlocked(relic))
                {
                    continue;
                }

                result.Add(relic);
            }

            return result;
        }

        public bool IsPowerUnlocked(PowerDefinition power)
        {
            if (power == null || _profile == null)
            {
                return false;
            }

            for (var i = 0; i < _profile.UnlockedPowerIds.Count; i++)
            {
                if (string.Equals(_profile.UnlockedPowerIds[i], power.PowerId, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsRelicUnlocked(PrototypeRelicDefinition relic)
        {
            if (relic == null || _profile == null)
            {
                return false;
            }

            for (var i = 0; i < _profile.UnlockedRelicIds.Count; i++)
            {
                if (string.Equals(_profile.UnlockedRelicIds[i], relic.RelicId, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        public bool TryUnlockPower(PowerDefinition power)
        {
            if (power == null || IsPowerUnlocked(power) || CurrencyBalance < power.UnlockCost)
            {
                return false;
            }

            _profile.CurrencyBalance -= power.UnlockCost;
            _profile.UnlockedPowerIds.Add(power.PowerId);
            RebuildUnlockedPowerCache();
            SaveProfile();
            ProfileChanged?.Invoke();
            return true;
        }

        public bool TryUnlockRelic(PrototypeRelicDefinition relic)
        {
            if (relic == null || IsRelicUnlocked(relic) || CurrencyBalance < relic.UnlockCost)
            {
                return false;
            }

            _profile.CurrencyBalance -= relic.UnlockCost;
            _profile.UnlockedRelicIds.Add(relic.RelicId);
            _profile.SelectedRelicId = relic.RelicId;
            RebuildUnlockedRelicCache();
            SaveProfile();
            ProfileChanged?.Invoke();
            return true;
        }

        public bool SelectRelic(PrototypeRelicDefinition relic)
        {
            if (relic == null || !IsRelicUnlocked(relic))
            {
                return false;
            }

            _profile.SelectedRelicId = relic.RelicId;
            SaveProfile();
            ProfileChanged?.Invoke();
            return true;
        }

        private void Subscribe()
        {
            if (_subscribed)
            {
                return;
            }

            if (prototypeRunController != null)
            {
                prototypeRunController.RunStarted += OnRunStarted;
                prototypeRunController.EnemyDefeated += OnEnemyDefeated;
                prototypeRunController.RunEnded += OnRunEnded;
            }

            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed)
            {
                return;
            }

            if (prototypeRunController != null)
            {
                prototypeRunController.RunStarted -= OnRunStarted;
                prototypeRunController.EnemyDefeated -= OnEnemyDefeated;
                prototypeRunController.RunEnded -= OnRunEnded;
            }

            _subscribed = false;
        }

        private void OnRunStarted()
        {
            _currentRunKills = 0;
            LastRunReward = 0;
        }

        private void OnEnemyDefeated(Gameplay.Enemies.PrototypeEnemyBase enemy)
        {
            _currentRunKills += 1;
        }

        private void OnRunEnded(bool cleared)
        {
            if (_profile == null || gameBootstrap == null || gameBootstrap.RunSession == null)
            {
                return;
            }

            var reward = CalculateRunReward(cleared);
            LastRunReward = reward;
            _profile.CurrencyBalance += reward;
            _profile.LifetimeCurrencyEarned += reward;
            _profile.TotalRuns += 1;
            if (cleared)
            {
                _profile.TotalClears += 1;
            }

            SaveProfile();
            ProfileChanged?.Invoke();
        }

        private int CalculateRunReward(bool cleared)
        {
            var runSession = gameBootstrap.RunSession;
            var state = runSession.State;
            var reward = 4;
            reward += Mathf.Max(0, state.CurrentLevel - 1);
            reward += Mathf.Max(0, state.CurrentWaveIndex + 1) * 2;
            reward += Mathf.Min(6, _currentRunKills / 3);
            reward += Mathf.Min(5, state.CollectedXp / 4);
            if (cleared)
            {
                reward += 8;
            }

            var selectedRelic = SelectedRelic;
            if (selectedRelic != null && selectedRelic.EffectType == RelicEffectType.MetaShardBonus)
            {
                reward += Mathf.RoundToInt(selectedRelic.PrimaryValue);
            }

            return Mathf.Max(3, reward);
        }

        private void LoadProfile()
        {
            try
            {
                if (!File.Exists(_profileFilePath))
                {
                    _profile = CreateDefaultProfile();
                    SaveProfile();
                    return;
                }

                var json = File.ReadAllText(_profileFilePath);
                if (string.IsNullOrWhiteSpace(json))
                {
                    _profile = CreateDefaultProfile();
                    SaveProfile();
                    return;
                }

                _profile = JsonUtility.FromJson<MetaProfileData>(json) ?? CreateDefaultProfile();
                if (_profile.UnlockedPowerIds == null)
                {
                    _profile.UnlockedPowerIds = new List<string>();
                }

                if (_profile.UnlockedRelicIds == null)
                {
                    _profile.UnlockedRelicIds = new List<string>();
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Meta profile load failed: {exception.Message}");
                _profile = CreateDefaultProfile();
            }
        }

        private void SaveProfile()
        {
            try
            {
                var directory = Path.GetDirectoryName(_profileFilePath);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var json = JsonUtility.ToJson(_profile, true);
                File.WriteAllText(_profileFilePath, json);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Meta profile save failed: {exception.Message}");
            }
        }

        private MetaProfileData CreateDefaultProfile()
        {
            return new MetaProfileData
            {
                CurrencyBalance = startingCurrency,
                LifetimeCurrencyEarned = startingCurrency,
                TotalRuns = 0,
                TotalClears = 0,
                UnlockedPowerIds = new List<string>(),
                UnlockedRelicIds = new List<string>(),
                SelectedRelicId = string.Empty
            };
        }

        private void EnsureUnlockDefaults()
        {
            if (_profile == null || powerLibrary == null)
            {
                return;
            }

            var changed = false;
            var ordered = powerLibrary.GetOrderedPowers();
            for (var i = 0; i < ordered.Count; i++)
            {
                var power = ordered[i];
                if (power == null || !power.IsUnlockedByDefault || IsPowerUnlocked(power))
                {
                    continue;
                }

                _profile.UnlockedPowerIds.Add(power.PowerId);
                changed = true;
            }

            for (var i = 0; i < ordered.Count && _profile.UnlockedPowerIds.Count < minimumUnlockedPowers; i++)
            {
                var power = ordered[i];
                if (power == null || IsPowerUnlocked(power))
                {
                    continue;
                }

                _profile.UnlockedPowerIds.Add(power.PowerId);
                changed = true;
            }

            if (changed)
            {
                SaveProfile();
            }
        }

        private void RebuildUnlockedPowerCache()
        {
            _cachedUnlockedPowers.Clear();
            if (powerLibrary == null)
            {
                return;
            }

            for (var i = 0; i < powerLibrary.Powers.Count; i++)
            {
                var power = powerLibrary.Powers[i];
                if (power != null && IsPowerUnlocked(power))
                {
                    _cachedUnlockedPowers.Add(power);
                }
            }
        }

        private void EnsureRelicUnlockDefaults()
        {
            if (_profile == null || relicLibrary == null)
            {
                return;
            }

            var changed = false;
            var ordered = relicLibrary.GetOrderedRelics();
            for (var i = 0; i < ordered.Count; i++)
            {
                var relic = ordered[i];
                if (relic == null || !relic.IsUnlockedByDefault || IsRelicUnlocked(relic))
                {
                    continue;
                }

                _profile.UnlockedRelicIds.Add(relic.RelicId);
                changed = true;
            }

            if (string.IsNullOrWhiteSpace(_profile.SelectedRelicId))
            {
                for (var i = 0; i < ordered.Count; i++)
                {
                    var relic = ordered[i];
                    if (relic == null || !IsRelicUnlocked(relic))
                    {
                        continue;
                    }

                    _profile.SelectedRelicId = relic.RelicId;
                    changed = true;
                    break;
                }
            }

            if (changed)
            {
                SaveProfile();
            }
        }

        private void RebuildUnlockedRelicCache()
        {
            _cachedUnlockedRelics.Clear();
            if (relicLibrary == null)
            {
                return;
            }

            for (var i = 0; i < relicLibrary.Relics.Count; i++)
            {
                var relic = relicLibrary.Relics[i];
                if (relic != null && IsRelicUnlocked(relic))
                {
                    _cachedUnlockedRelics.Add(relic);
                }
            }
        }
    }
}
