using System.Collections.Generic;
using UnityEngine;

namespace SnakeRoguelite.Gameplay.Waves
{
    [CreateAssetMenu(
        fileName = "WaveCatalog",
        menuName = "Snake Roguelite/Wave Catalog")]
    public sealed class WaveCatalog : ScriptableObject
    {
        [field: SerializeField]
        public List<WaveDefinition> Waves { get; private set; } = new();

        public WaveDefinition GetWave(int index)
        {
            if (Waves.Count == 0)
            {
                return null;
            }

            if (index < 0)
            {
                index = 0;
            }

            if (index >= Waves.Count)
            {
                index = Waves.Count - 1;
            }

            return Waves[index];
        }
    }
}
