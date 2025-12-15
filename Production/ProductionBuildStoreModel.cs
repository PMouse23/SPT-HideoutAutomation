using EFT.Hideout;

namespace HideoutAutomation.Production
{
    internal class ProductionBuildStoreModel
    {
        public int AreaType { get; set; }
        public bool Continuous { get; set; }
        public int Count { get; set; }
        public string Id { get; set; }
        public bool IsCodeProduction { get; set; }
        public bool NeedFuelForAllProductionTime { get; set; }
        public int ProductionLimitCount { get; set; }
        public float ProductionTime { get; set; }
        public Requirement[] Requirements { get; set; }
    }
}