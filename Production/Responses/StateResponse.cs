using EFT;
using System.Collections.Generic;

namespace HideoutAutomation.Production.Responses
{
    internal struct StateResponse
    {
        public Dictionary<EAreaType, int> areaCount;

        public List<string> productions;

        public Dictionary<string, int> stackCount;
    }
}