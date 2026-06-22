using System;
using System.Collections.Generic;
using UnityEngine;

namespace SnakeRoguelite.Meta
{
    [CreateAssetMenu(
        fileName = "PrototypeRelicLibrary",
        menuName = "Snake Roguelite/Prototype Relic Library")]
    public sealed class PrototypeRelicLibrary : ScriptableObject
    {
        [field: SerializeField]
        public List<PrototypeRelicDefinition> Relics { get; private set; } = new();

        public PrototypeRelicDefinition FindById(string relicId)
        {
            for (var i = 0; i < Relics.Count; i++)
            {
                var relic = Relics[i];
                if (relic != null && string.Equals(relic.RelicId, relicId, StringComparison.Ordinal))
                {
                    return relic;
                }
            }

            return null;
        }

        public List<PrototypeRelicDefinition> GetOrderedRelics()
        {
            var ordered = new List<PrototypeRelicDefinition>(Relics.Count);
            for (var i = 0; i < Relics.Count; i++)
            {
                if (Relics[i] != null)
                {
                    ordered.Add(Relics[i]);
                }
            }

            ordered.Sort(CompareRelics);
            return ordered;
        }

        private static int CompareRelics(
            PrototypeRelicDefinition left,
            PrototypeRelicDefinition right)
        {
            if (left == right)
            {
                return 0;
            }

            if (left == null)
            {
                return 1;
            }

            if (right == null)
            {
                return -1;
            }

            var costCompare = left.UnlockCost.CompareTo(right.UnlockCost);
            if (costCompare != 0)
            {
                return costCompare;
            }

            return string.Compare(left.DisplayName, right.DisplayName, StringComparison.Ordinal);
        }
    }
}
