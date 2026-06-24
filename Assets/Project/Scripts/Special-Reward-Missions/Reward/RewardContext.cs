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
                    .Distinct()
                    .ToList();
        }

        public bool HasTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
                return false;

            return tags.Contains(tag.Trim());
        }

        public override string ToString()
        {
            return $"SourceType: {SourceType}, Level: {LevelNumber}, SourceId: {SourceId}, Tags: {string.Join(",", tags)}";
        }
    }
}