using EFT;

namespace HideoutAutomation.Production.Requests
{
    internal struct ProductionCountRequest
    {
        public EAreaType area;
        public MongoID recipeId;
    }
}