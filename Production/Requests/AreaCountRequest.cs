using EFT;

namespace HideoutAutomation.Production.Requests
{
    internal struct AreaCountRequest
    {
        public EAreaType area;

        public bool includeCurrentProduction;
    }
}