using LayersIO.DataTransfer;
using LayerWorks.EntityFormatters;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;
using Teigha.Runtime;

namespace LayerWorks
{
    internal class LayerWorksExtension : IExtensionApplication
    {
        ServiceCollection _services = new();
        internal static IServiceProvider LayerWorksServiceProvider = null!;
        public void Initialize()
        {
            _services.AddSingleton<IStandardReader<LayerProps>, InMemoryLayerPropsReader>();



            LayerWorksServiceProvider = _services.BuildServiceProvider();

            TypeDescriptor.AddAttributes(typeof(Teigha.Colors.Color), new TypeConverterAttribute(typeof(TeighaColorTypeConverter)));
        }

        public void Terminate()
        {

        }
    }
}
