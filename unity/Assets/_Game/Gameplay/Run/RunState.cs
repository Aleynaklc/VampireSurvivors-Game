using System;

namespace SnakeRoguelite.Gameplay.Run
{
    public sealed class RunState
    {
        public event Action<int> LevelChanged;
        public event Action<int, int> XpProgressChanged;

        public RunPhase Phase { get; private set; } = RunPhase.None;
        public int CurrentWaveIndex { get; private set; }
        public int CurrentHealth { get; private set; }
        public int CurrentLevel { get; private set; }
        public int CollectedXp { get; private set; }
        public int CurrentLevelXp { get; private set; }
        public int NextLevelXpRequirement { get; private set; }

        public void Reset(int startingHealth, int startingXpToLevel)
        {
            Phase = RunPhase.Warmup;
            CurrentWaveIndex = 0;
            CurrentHealth = startingHealth;
            CurrentLevel = 1;
            CollectedXp = 0;
            CurrentLevelXp = 0;
            NextLevelXpRequirement = startingXpToLevel;
            XpProgressChanged?.Invoke(CurrentLevelXp, NextLevelXpRequirement);
        }

        public void BeginWave(int waveIndex)
        {
            CurrentWaveIndex = waveIndex;
            Phase = RunPhase.InWave;
        }

        public void EnterDraft()
        {
            Phase = RunPhase.Draft;
        }

        public void BeginBoss()
        {
            Phase = RunPhase.Boss;
        }

        public void ResumeStoredPhase(RunPhase phase)
        {
            Phase = phase;
        }

        public void FinishRun()
        {
            Phase = RunPhase.Summary;
        }

        public void FailRun()
        {
            Phase = RunPhase.Failed;
        }

        public void GainXp(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            CollectedXp += amount;
            CurrentLevelXp += amount;
            XpProgressChanged?.Invoke(CurrentLevelXp, NextLevelXpRequirement);
        }

        public bool CanLevelUp()
        {
            return CurrentLevelXp >= NextLevelXpRequirement;
        }

        public void ResolveLevelUp(int nextLevelXpRequirement)
        {
            CurrentLevelXp -= NextLevelXpRequirement;
            CurrentLevel += 1;
            NextLevelXpRequirement = nextLevelXpRequirement;
            LevelChanged?.Invoke(CurrentLevel);
            XpProgressChanged?.Invoke(CurrentLevelXp, NextLevelXpRequirement);
        }

        public void ApplyDamage(int amount)
        {
            if (amount <= 0 || CurrentHealth <= 0)
            {
                return;
            }

            CurrentHealth -= amount;
            if (CurrentHealth <= 0)
            {
                CurrentHealth = 0;
                FailRun();
            }
        }
    }
}
