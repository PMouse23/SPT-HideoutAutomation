using EFT.Hideout;

namespace HideoutAutomation.Production
{
    internal class ProductionBuild : ProductionBuildAbstractClass
    {
        public string Id
        {
            get { return _id; }
            set { _id = value; }
        }

        public Requirement[] Requirements
        {
            get { return this.requirements; }
            set { this.requirements = value; }
        }
    }
}