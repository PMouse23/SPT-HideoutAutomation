using EFT;
using EFT.Hideout;
using System.Collections.Generic;

#nullable enable

namespace HideoutAutomation.HIP
{
    internal class HIPContributionCandidate
    {
        internal HIPContributionCandidate(AreaData areaData, EAreaType areaType, ItemRequirement[] itemRequirements, List<string> itemIds, List<string> contributionMessages)
        {
            this.AreaData = areaData;
            this.AreaType = areaType;
            this.ItemRequirements = itemRequirements;
            this.ItemIds = itemIds;
            this.ContributionMessages = contributionMessages;
        }

        public AreaData AreaData { get; }
        public EAreaType AreaType { get; }
        public List<string> ContributionMessages { get; }
        public List<string> ItemIds { get; }
        public ItemRequirement[] ItemRequirements { get; }
    }
}