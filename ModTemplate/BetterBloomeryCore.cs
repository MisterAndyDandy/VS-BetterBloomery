using Vintagestory.API.Common;

namespace BetterBloomeries
{
     public class BetterBloomeries : ModSystem
    { 
        public override void Start(ICoreAPI api)
        {
            base.Start(api);

            api.RegisterBlockClass("BetterBlockBloomery", typeof(BetterBlockBloomery));
        }
    }
}
