using LayerWorks.EntityFormatters;
using LoaderCore.Interfaces;
using LoaderCore;
using System.ComponentModel;
using Teigha.Runtime;
using Microsoft.Extensions.DependencyInjection;
using LayerWorks.LayerProcessing;

namespace LayerWorks
{
    internal class LayerWorksExtension : IExtensionApplication
    {
        void IExtensionApplication.Initialize()
        {
            TypeDescriptor.AddAttributes(typeof(Teigha.Colors.Color), new TypeConverterAttribute(typeof(TeighaColorTypeConverter)));

            NcetCore.Services.AddSingleton<IEntityFormatter, StandardEntityFormatter>()
                             .AddSingleton<ILayerChecker, LayerChecker>()
                             .AddSingleton<LayerChecker>();
        }

        public void Terminate()
        {

        }
    }
}
