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
using Teigha.Runtime;

namespace GeoMod
{
    [NcetModuleInitializer]
    public class GeoModInitializer : INcetInitializer
    {
        public GeoModInitializer() { }

        public void Initialize()
        {
            NcetCore.Services.AddSingleton<INtsGeometryServicesFactory, GeomodNtsGeometryServicesFactory>()
                             .AddSingleton<IPrecisionReducer, GeomodNtsPrecisionReducer>();
        }

        public void PostInitialize()
        {

        }
    }

    public class GeoModExtension : IExtensionApplication
    {
        public void Initialize()
        {
            NcetCore.Services.AddSingleton<IBuffer, GeomodNtsBufferizer>()
                             .AddTransient<GeomodPrecisionCommands>()
                             .AddTransient<GeomodWktCommands>()
                             .AddTransient<GeomodBufferizationCommands>()
                             .AddTransient<GeomodAutoBufferizationCommand>();
        }

        public void Terminate()
        {
        }
    }
}
