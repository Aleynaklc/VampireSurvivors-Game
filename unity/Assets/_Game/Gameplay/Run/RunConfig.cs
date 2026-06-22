using UnityEngine;

namespace SnakeRoguelite.Gameplay.Run
{
    [CreateAssetMenu(
        fileName = "RunConfig",
        menuName = "Snake Roguelite/Run Config")]
    public sealed class RunConfig : ScriptableObject
    {
        [field: SerializeField, Min(1)]
        public int MaxWaveCount { get; private set; } = 3;

        [field: SerializeField, Min(10f)]
        public float WaveDurationSeconds { get; private set; } = 90f;

        [field: SerializeField, Min(1)]
        public int DraftChoiceCount { get; private set; } = 3;

        [field: SerializeField, Min(1)]
        public int StartingHealth { get; private set; } = 5;

        [field: SerializeField, Min(1)]
        public int StartingXpToLevel { get; private set; } = 4;

        [field: SerializeField, Min(0)]
        public int XpToLevelGrowthPerLevel { get; private set; } = 2;
    }
}
