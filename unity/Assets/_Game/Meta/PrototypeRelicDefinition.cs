using UnityEngine;

namespace SnakeRoguelite.Meta
{
    [CreateAssetMenu(
        fileName = "PrototypeRelicDefinition",
        menuName = "Snake Roguelite/Prototype Relic Definition")]
    public sealed class PrototypeRelicDefinition : ScriptableObject
    {
        [field: SerializeField]
        public string RelicId { get; private set; } = string.Empty;

        [field: SerializeField]
        public string DisplayName { get; private set; } = string.Empty;

        [field: SerializeField, TextArea]
        public string Description { get; private set; } = string.Empty;

        [field: SerializeField]
        public bool IsUnlockedByDefault { get; private set; }

        [field: SerializeField, Min(0)]
        public int UnlockCost { get; private set; }

        [field: SerializeField]
        public RelicEffectType EffectType { get; private set; } = RelicEffectType.None;

        [field: SerializeField]
        public float PrimaryValue { get; private set; }

        [field: SerializeField]
        public float SecondaryValue { get; private set; }

        [field: SerializeField]
        public float TertiaryValue { get; private set; }
    }
}
