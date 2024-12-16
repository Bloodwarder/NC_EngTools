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
using GeoMod.Processing;

namespace GeoMod
{
    [NcetModuleInitializer]
    public class GeoModInitializer : INcetInitializer
    {
        public GeoModInitializer() { }

        public void Initialize()
        {
            NcetCore.Services.AddSingleton<INtsGeometryServicesFactory, GeomodNtsGeometryServicesFactory>()
                             .AddSingleton<IPrecisionReducer, GeomodNtsPrecisionReducer>()
                             .AddSingleton<IBuffer, GeomodNtsBufferizer>()
                             .AddTransient<GeomodPrecisionCommands>()
                             .AddTransient<GeomodWktCommands>()
                             .AddTransient<GeomodBufferizationCommands>()
                             .AddTransient<GeomodAutoBufferizationCommand>();
        }

        public void PostInitialize()
        {

        }
    }
}
