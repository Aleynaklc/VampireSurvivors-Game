using UnityEngine;

namespace SnakeRoguelite.Gameplay.Powers
{
    [CreateAssetMenu(
        fileName = "PowerDefinition",
        menuName = "Snake Roguelite/Power Definition")]
    public sealed class PowerDefinition : ScriptableObject
    {
        [field: SerializeField]
        public string PowerId { get; private set; } = string.Empty;

        [field: SerializeField]
        public string DisplayName { get; private set; } = string.Empty;

        [field: SerializeField, TextArea]
        public string Description { get; private set; } = string.Empty;

        [field: SerializeField]
        public PowerRarity Rarity { get; private set; } = PowerRarity.Common;

        [field: SerializeField]
        public PowerTag PrimaryTag { get; private set; } = PowerTag.None;

        [field: SerializeField]
        public bool IsRunDefining { get; private set; }

        [field: Header("Meta Progression")]
        [field: SerializeField]
        public bool IsUnlockedByDefault { get; private set; } = true;

        [field: SerializeField, Min(0)]
        public int UnlockCost { get; private set; }

        [field: Header("Prototype Runtime")]
        [field: SerializeField]
        public PowerEffectType EffectType { get; private set; } = PowerEffectType.None;

        [field: SerializeField]
        public float PrimaryValue { get; private set; }

        [field: SerializeField]
        public float SecondaryValue { get; private set; }

        [field: SerializeField]
        public float TertiaryValue { get; private set; }
    }
}
