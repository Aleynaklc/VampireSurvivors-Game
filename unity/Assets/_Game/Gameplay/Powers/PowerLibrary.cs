using System;
using System.Collections.Generic;
using UnityEngine;

namespace SnakeRoguelite.Gameplay.Powers
{
    [CreateAssetMenu(
        fileName = "PowerLibrary",
        menuName = "Snake Roguelite/Power Library")]
    public sealed class PowerLibrary : ScriptableObject
    {
        [field: SerializeField]
        public List<PowerDefinition> Powers { get; private set; } = new();

        public PowerDefinition FindById(string powerId)
        {
            foreach (var power in Powers)
            {
                if (string.Equals(power.PowerId, powerId, StringComparison.Ordinal))
                {
                    return power;
                }
            }

            return null;
        }

        public List<PowerDefinition> GetOrderedPowers()
        {
            var ordered = new List<PowerDefinition>(Powers.Count);
            for (var i = 0; i < Powers.Count; i++)
            {
                if (Powers[i] != null)
                {
                    ordered.Add(Powers[i]);
                }
            }

            ordered.Sort(ComparePowers);
            return ordered;
        }

        private static int ComparePowers(PowerDefinition left, PowerDefinition right)
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

            var rarityCompare = left.Rarity.CompareTo(right.Rarity);
            if (rarityCompare != 0)
            {
                return rarityCompare;
            }

            return string.Compare(left.DisplayName, right.DisplayName, StringComparison.Ordinal);
        }
    }
}
