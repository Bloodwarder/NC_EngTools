//System
//Microsoft
using Microsoft.Extensions.DependencyInjection;
// Nanocad
//Internal
using LoaderCore;
//NTS
using LoaderCore.Integrity;
using LoaderCore.Interfaces;
using GeoMod.Commands;
using GeoMod.NtsServices;

namespace GeoMod
{
    [NcetModuleInitializer]
    public class GeoModInitializer : INcetInitializer
    {
        public GeoModInitializer() { }

        public void Initialize()
        {
            NcetCore.Services.AddSingleton<INtsGeometryServicesFactory, GeomodNtsGeometryServicesFactory>();
            NcetCore.Services.AddSingleton<IPrecisionReducer, GeomodNtsPrecisionReducer>();
            NcetCore.Services.AddSingleton<GeomodPrecisionCommands>();
            NcetCore.Services.AddSingleton<GeomodWktCommands>();
            NcetCore.Services.AddSingleton<GeomodBufferizationCommands>();
        }

        public void PostInitialize()
        {

        }
    }
}
