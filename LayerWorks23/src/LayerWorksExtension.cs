using LayersIO.Connection;
using LayersIO.DataTransfer;
using LayersIO.ExternalData;
using LayerWorks.EntityFormatters;
using LoaderCore;
using LoaderCore.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;
using Teigha.Runtime;

namespace LayerWorks
{
    internal class LayerWorksExtension : IExtensionApplication
    {
        public void Initialize()
        {
            
            LoaderExtension.Services.AddTransient<IStandardReader<LayerProps>, InMemoryLayerPropsReader>();
            LoaderExtension.Services.AddTransient<IStandardReader<LegendData>, InMemoryLayerLegendReader>();
            LoaderExtension.Services.AddTransient<IStandardReader<LegendDrawTemplate>, InMemoryLayerLegendDrawReader>();
            LoaderExtension.Services.AddTransient<InMemoryLayerAlterReader>();

            LoaderExtension.Services.AddSingleton<IDictionary<string, LayerProps>, LayerPropertiesDictionary>();
            LoaderExtension.Services.AddSingleton<IDictionary<string, LegendData>, LayerLegendDictionary>();
            LoaderExtension.Services.AddSingleton<IDictionary<string, LegendDrawTemplate>, LayerLegendDrawDictionary>();
            LoaderExtension.Services.AddSingleton<LayerAlteringDictionary>();

            //LoaderExtension.Services.AddDbContext<LayersDatabaseContextSqlite>(ServiceLifetime.Scoped);
            
            TypeDescriptor.AddAttributes(typeof(Teigha.Colors.Color), new TypeConverterAttribute(typeof(TeighaColorTypeConverter)));
        }

        public void Terminate()
        {

        }
    }
}
