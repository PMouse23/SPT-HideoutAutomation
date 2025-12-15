using EFT.Hideout;

namespace HideoutAutomation.Production
{
    internal class ProductionBuild : ProductionBuildAbstractClass
    {
        public int AreaType
        {
            get { return this.areaType; }
            set { this.areaType = value; }
        }

        public bool Continuous
        {
            get { return this.continuous; }
            set { this.continuous = value; }
        }

        public int Count
        {
            get { return this.count; }
            set { this.count = value; }
        }

        public string Id
        {
            get { return _id; }
            set { _id = value; }
        }

        public bool IsCodeProduction
        {
            get { return this.isCodeProduction; }
            set { this.isCodeProduction = value; }
        }

        public bool NeedFuelForAllProductionTime
        {
            get { return this.needFuelForAllProductionTime; }
            set { this.needFuelForAllProductionTime = value; }
        }

        public int ProductionLimitCount
        {
            get { return this.productionLimitCount; }
            set { this.productionLimitCount = value; }
        }

        public float ProductionTime
        {
            get { return this.productionTime; }
            set { this.productionTime = value; }
        }

        public Requirement[] Requirements
        {
            get { return this.requirements; }
            set { this.requirements = value; }
        }
    }
}