using System.Collections.Generic;

namespace HideoutAutomation.Production.Responses
{
    internal struct StateResponse
    {
        public List<string> productions;

        public Dictionary<string, int> stackCount;
    }
}