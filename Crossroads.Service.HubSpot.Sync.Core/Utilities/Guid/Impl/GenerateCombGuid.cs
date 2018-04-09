using RT.Comb;

namespace Crossroads.Service.HubSpot.Sync.Core.Utilities.Guid.Impl
{
    /// <summary>
    /// Implementation of generating a comb guid (mostly used for ids)
    /// this is an abstraction around the magnum static method so is can be mocked 
    /// and tested
    /// </summary>
    public class GenerateCombGuid : IGenerateCombGuid
    {
        public System.Guid Generate()
        {
            return Provider.Sql.Create();
        }
    }
}
