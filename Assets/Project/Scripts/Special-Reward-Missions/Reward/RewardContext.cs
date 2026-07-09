using System;
using System.Collections.Generic;
using System.Linq;

namespace ZenMatch.Runtime.Rewards
{
    [Serializable]
    public sealed class RewardContext
    {
        public RewardSourceType SourceType { get; }
        public int LevelNumber { get; }

        public string SourceId { get; }
        public string SourceDisplayName { get; }

        public IReadOnlyList<string> Tags => tags;

        private readonly List<string> tags;

        public RewardContext(
            RewardSourceType sourceType,
            int levelNumber = -1,
            string sourceId = "",
            string sourceDisplayName = "",
            IEnumerable<string> tags = null)
        {
            SourceType = sourceType;
            LevelNumber = levelNumber;
            SourceId = sourceId ?? string.Empty;
            SourceDisplayName = sourceDisplayName ?? string.Empty;

            this.tags = tags == null
                ? new List<string>()
                : tags
                    .Where(t => !string.IsNullOrWhiteSpace(t))
                    .Select(t => t.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
        }

        public bool HasTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
                return false;

            string normalized = tag.Trim();

            for (int i = 0; i < tags.Count; i++)
            {
                if (string.Equals(tags[i], normalized, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        public override string ToString()
        {
            return $"SourceType: {SourceType}, Level: {LevelNumber}, SourceId: {SourceId}, Tags: {string.Join(",", tags)}";
        }
    }
}