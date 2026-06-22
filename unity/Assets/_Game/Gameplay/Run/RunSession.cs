using System.Collections.Generic;
using SnakeRoguelite.Gameplay.Powers;
using SnakeRoguelite.Gameplay.Waves;

namespace SnakeRoguelite.Gameplay.Run
{
    public sealed class RunSession
    {
        private readonly RunConfig _runConfig;
        private readonly PowerLibrary _powerLibrary;
        private readonly WaveCatalog _waveCatalog;
        private readonly PowerDraftRoller _draftRoller;
        private readonly List<PowerDefinition> _selectedPowers = new();
        private readonly RunState _state = new();
        private RunPhase _phaseBeforeDraft = RunPhase.None;
        private int _pendingDraftCount;

        public RunSession(RunConfig runConfig, PowerLibrary powerLibrary, WaveCatalog waveCatalog)
        {
            _runConfig = runConfig;
            _powerLibrary = powerLibrary;
            _waveCatalog = waveCatalog;
            _draftRoller = new PowerDraftRoller();
        }

        public RunState State => _state;
        public IReadOnlyList<PowerDefinition> SelectedPowers => _selectedPowers;
        public int PendingDraftCount => _pendingDraftCount;

        public void StartNewRun()
        {
            _selectedPowers.Clear();
            _pendingDraftCount = 0;
            _phaseBeforeDraft = RunPhase.None;
            _state.Reset(_runConfig.StartingHealth, _runConfig.StartingXpToLevel);
            _state.BeginWave(0);
        }

        public IReadOnlyList<PowerDefinition> RollDraftChoices(
            IReadOnlyList<PowerDefinition> sourceOverride = null)
        {
            return _draftRoller.Roll(
                sourceOverride ?? _powerLibrary.Powers,
                _selectedPowers,
                _runConfig.DraftChoiceCount);
        }

        public void SelectPower(PowerDefinition powerDefinition)
        {
            if (powerDefinition == null)
            {
                return;
            }

            _selectedPowers.Add(powerDefinition);
        }

        public WaveDefinition GetCurrentWave()
        {
            return _waveCatalog.GetWave(_state.CurrentWaveIndex);
        }

        public bool TryAdvance()
        {
            if (_state.CurrentWaveIndex + 1 >= _runConfig.MaxWaveCount)
            {
                return false;
            }

            return true;
        }

        public void BeginNextWave()
        {
            _state.BeginWave(_state.CurrentWaveIndex + 1);
        }

        public void BeginBossEncounter()
        {
            _state.BeginBoss();
        }

        public void CompleteBoss()
        {
            _state.FinishRun();
        }

        public bool ProcessXpGain(int amount)
        {
            _state.GainXp(amount);

            var leveledUp = false;
            while (_state.CanLevelUp())
            {
                var nextRequirement = GetXpRequirementForLevel(_state.CurrentLevel + 1);
                _state.ResolveLevelUp(nextRequirement);
                _pendingDraftCount += 1;
                leveledUp = true;
            }

            if (!leveledUp)
            {
                return false;
            }

            if (_state.Phase != RunPhase.Draft)
            {
                _phaseBeforeDraft = _state.Phase;
                _state.EnterDraft();
            }

            return true;
        }

        public bool FinalizeDraftSelection()
        {
            if (_pendingDraftCount > 0)
            {
                _pendingDraftCount -= 1;
            }

            if (_pendingDraftCount > 0)
            {
                _state.EnterDraft();
                return true;
            }

            _state.ResumeStoredPhase(_phaseBeforeDraft);
            _phaseBeforeDraft = RunPhase.None;
            return false;
        }

        private int GetXpRequirementForLevel(int level)
        {
            var extraLevels = level - 1;
            return _runConfig.StartingXpToLevel + (_runConfig.XpToLevelGrowthPerLevel * extraLevels);
        }
    }
}
