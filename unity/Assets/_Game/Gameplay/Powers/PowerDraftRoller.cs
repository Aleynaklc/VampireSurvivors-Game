using System;
using System.Collections.Generic;

namespace SnakeRoguelite.Gameplay.Powers
{
    public sealed class PowerDraftRoller
    {
        private const float CommonWeight = 1f;
        private const float RareWeight = 0.62f;
        private const float LegendaryWeight = 0.24f;
        private const float EarlyRunDefiningBonus = 1.9f;
        private const float ExtraRunDefiningPenalty = 0.72f;
        private const float UtilityEarlyPenalty = 0.82f;
        private const float MinimumWeight = 0.01f;

        private readonly Random _random = new();

        public IReadOnlyList<PowerDefinition> Roll(
            IReadOnlyList<PowerDefinition> source,
            IReadOnlyList<PowerDefinition> alreadyPicked,
            int choiceCount)
        {
            if (source == null || source.Count == 0 || choiceCount <= 0)
            {
                return Array.Empty<PowerDefinition>();
            }

            var selectedTagCounts = CollectTagCounts(alreadyPicked);
            var hasRunDefiningPower = HasRunDefiningPower(alreadyPicked);
            var offeredTagCounts = new Dictionary<PowerTag, int>();
            var result = new List<PowerDefinition>(Math.Min(choiceCount, source.Count));

            var primaryPool = BuildPool(source, alreadyPicked, result, false);
            RollFromPool(
                primaryPool,
                result,
                choiceCount,
                alreadyPicked.Count,
                hasRunDefiningPower,
                selectedTagCounts,
                offeredTagCounts);

            if (result.Count >= choiceCount)
            {
                return result;
            }

            var fallbackPool = BuildPool(source, alreadyPicked, result, true);
            RollFromPool(
                fallbackPool,
                result,
                choiceCount,
                alreadyPicked.Count,
                hasRunDefiningPower,
                selectedTagCounts,
                offeredTagCounts);

            return result;
        }

        private static bool HasPicked(
            IReadOnlyList<PowerDefinition> alreadyPicked,
            PowerDefinition candidate)
        {
            for (var i = 0; i < alreadyPicked.Count; i++)
            {
                if (alreadyPicked[i] == candidate)
                {
                    return true;
                }
            }

            return false;
        }

        private static Dictionary<PowerTag, int> CollectTagCounts(
            IReadOnlyList<PowerDefinition> powers)
        {
            var counts = new Dictionary<PowerTag, int>();
            for (var i = 0; i < powers.Count; i++)
            {
                var power = powers[i];
                if (power == null || power.PrimaryTag == PowerTag.None)
                {
                    continue;
                }

                counts.TryGetValue(power.PrimaryTag, out var count);
                counts[power.PrimaryTag] = count + 1;
            }

            return counts;
        }

        private static bool HasRunDefiningPower(IReadOnlyList<PowerDefinition> powers)
        {
            for (var i = 0; i < powers.Count; i++)
            {
                if (powers[i] != null && powers[i].IsRunDefining)
                {
                    return true;
                }
            }

            return false;
        }

        private static List<PowerDefinition> BuildPool(
            IReadOnlyList<PowerDefinition> source,
            IReadOnlyList<PowerDefinition> alreadyPicked,
            IReadOnlyList<PowerDefinition> currentDraft,
            bool includeAlreadyPicked)
        {
            var pool = new List<PowerDefinition>(source.Count);
            for (var i = 0; i < source.Count; i++)
            {
                var candidate = source[i];
                if (candidate == null || Contains(currentDraft, candidate))
                {
                    continue;
                }

                if (!includeAlreadyPicked && HasPicked(alreadyPicked, candidate))
                {
                    continue;
                }

                pool.Add(candidate);
            }

            return pool;
        }

        private void RollFromPool(
            List<PowerDefinition> pool,
            List<PowerDefinition> result,
            int choiceCount,
            int alreadyPickedCount,
            bool hasRunDefiningPower,
            IReadOnlyDictionary<PowerTag, int> selectedTagCounts,
            Dictionary<PowerTag, int> offeredTagCounts)
        {
            while (pool.Count > 0 && result.Count < choiceCount)
            {
                var candidate = RollWeightedChoice(
                    pool,
                    alreadyPickedCount,
                    hasRunDefiningPower,
                    selectedTagCounts,
                    offeredTagCounts);

                if (candidate == null)
                {
                    break;
                }

                result.Add(candidate);
                pool.Remove(candidate);

                if (candidate.PrimaryTag == PowerTag.None)
                {
                    continue;
                }

                offeredTagCounts.TryGetValue(candidate.PrimaryTag, out var count);
                offeredTagCounts[candidate.PrimaryTag] = count + 1;
            }
        }

        private PowerDefinition RollWeightedChoice(
            IReadOnlyList<PowerDefinition> pool,
            int alreadyPickedCount,
            bool hasRunDefiningPower,
            IReadOnlyDictionary<PowerTag, int> selectedTagCounts,
            IReadOnlyDictionary<PowerTag, int> offeredTagCounts)
        {
            var totalWeight = 0f;
            for (var i = 0; i < pool.Count; i++)
            {
                totalWeight += GetWeight(
                    pool[i],
                    alreadyPickedCount,
                    hasRunDefiningPower,
                    selectedTagCounts,
                    offeredTagCounts);
            }

            if (totalWeight <= 0f)
            {
                return pool.Count > 0 ? pool[_random.Next(pool.Count)] : null;
            }

            var roll = (float)_random.NextDouble() * totalWeight;
            for (var i = 0; i < pool.Count; i++)
            {
                var candidate = pool[i];
                roll -= GetWeight(
                    candidate,
                    alreadyPickedCount,
                    hasRunDefiningPower,
                    selectedTagCounts,
                    offeredTagCounts);

                if (roll <= 0f)
                {
                    return candidate;
                }
            }

            return pool[pool.Count - 1];
        }

        private static float GetWeight(
            PowerDefinition candidate,
            int alreadyPickedCount,
            bool hasRunDefiningPower,
            IReadOnlyDictionary<PowerTag, int> selectedTagCounts,
            IReadOnlyDictionary<PowerTag, int> offeredTagCounts)
        {
            var weight = GetBaseRarityWeight(candidate.Rarity);
            if (candidate.PrimaryTag != PowerTag.None)
            {
                if (selectedTagCounts.TryGetValue(candidate.PrimaryTag, out var selectedTagCount))
                {
                    weight *= 1f + Math.Min(1.1f, selectedTagCount * 0.45f);
                }
                else if (selectedTagCounts.Count > 0)
                {
                    weight *= 1.14f;
                }

                if (offeredTagCounts.TryGetValue(candidate.PrimaryTag, out var offeredTagCount))
                {
                    weight *= Math.Max(0.58f, 1f - (offeredTagCount * 0.28f));
                }
            }

            if (candidate.IsRunDefining)
            {
                if (!hasRunDefiningPower && alreadyPickedCount <= 1)
                {
                    weight *= EarlyRunDefiningBonus;
                }
                else if (hasRunDefiningPower)
                {
                    weight *= ExtraRunDefiningPenalty;
                }
            }

            if (candidate.PrimaryTag == PowerTag.Utility && alreadyPickedCount == 0)
            {
                weight *= UtilityEarlyPenalty;
            }

            return Math.Max(MinimumWeight, weight);
        }

        private static float GetBaseRarityWeight(PowerRarity rarity)
        {
            return rarity switch
            {
                PowerRarity.Common => CommonWeight,
                PowerRarity.Rare => RareWeight,
                PowerRarity.Legendary => LegendaryWeight,
                _ => CommonWeight,
            };
        }

        private static bool Contains(
            IReadOnlyList<PowerDefinition> powers,
            PowerDefinition candidate)
        {
            for (var i = 0; i < powers.Count; i++)
            {
                if (powers[i] == candidate)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
