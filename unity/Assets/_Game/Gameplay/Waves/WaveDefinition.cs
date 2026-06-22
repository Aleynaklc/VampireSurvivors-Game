using UnityEngine;

namespace SnakeRoguelite.Gameplay.Waves
{
    [CreateAssetMenu(
        fileName = "WaveDefinition",
        menuName = "Snake Roguelite/Wave Definition")]
    public sealed class WaveDefinition : ScriptableObject
    {
        [field: SerializeField, Min(1)]
        public int Index { get; private set; } = 1;

        [field: SerializeField, Min(1f)]
        public float DurationSeconds { get; private set; } = 90f;

        [field: SerializeField, Min(1)]
        public int ChaserCount { get; private set; } = 10;

        [field: SerializeField, Min(0)]
        public int DasherCount { get; private set; }

        [field: SerializeField, Min(0)]
        public int TankCount { get; private set; }

        [field: SerializeField]
        public bool IsEliteWave { get; private set; }
    }
}
