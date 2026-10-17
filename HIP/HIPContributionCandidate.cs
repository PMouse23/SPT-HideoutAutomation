using EFT;
using EFT.Hideout;
using System.Collections.Generic;

#nullable enable

namespace HideoutAutomation.HIP
{
    internal class HIPContributionCandidate
    {
        internal HIPContributionCandidate(AreaData areaData, EAreaType areaType, ItemRequirement[] itemRequirements, List<string> itemIds, List<string> contributionMessages, List<HIPContributionLine> lines)
        {
            this.AreaData = areaData;
            this.AreaType = areaType;
            this.ItemRequirements = itemRequirements;
            this.ItemIds = itemIds;
            this.ContributionMessages = contributionMessages;
            this.Lines = lines;
        }

        public AreaData AreaData { get; }
        public EAreaType AreaType { get; }
        public List<string> ContributionMessages { get; }
        public List<string> ItemIds { get; }
        public ItemRequirement[] ItemRequirements { get; }

        /// <summary>One line per required item, with every stack of that item summed up.</summary>
        public List<HIPContributionLine> Lines { get; }
    }

    internal class HIPContributionLine
    {
        internal HIPContributionLine(string name, int count, int needed)
        {
            this.Name = name;
            this.Count = count;
            this.Needed = needed;
        }

        public string Name { get; }
        public int Count { get; set; }

        /// <summary>How many the area still needed before this contribution.</summary>
        public int Needed { get; }
    }
}
