using LayersIO.DataTransfer;
using LayersIO.ExternalData;
using LayerWorks.EntityFormatters;
using LoaderCore;
using LoaderCore.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;
using Teigha.Runtime;
using LayersIO.Database;
using LayersIO.Database.Readers;

namespace LayerWorks
{
    internal class LayerWorksExtension : IExtensionApplication
    {
        public void Initialize()
        {
            RegisterServices();

            TypeDescriptor.AddAttributes(typeof(Teigha.Colors.Color), new TypeConverterAttribute(typeof(TeighaColorTypeConverter)));
        }

        private static void RegisterServices()
        {
            LoaderExtension.Services.AddTransient<IStandardReader<LayerProps>, InMemoryLayerPropsReader>();
            LoaderExtension.Services.AddTransient<IStandardReader<LegendData>, InMemoryLayerLegendReader>();
            LoaderExtension.Services.AddTransient<IStandardReader<LegendDrawTemplate>, InMemoryLayerLegendDrawReader>();
            LoaderExtension.Services.AddTransient<InMemoryLayerAlterReader>();

            LoaderExtension.Services.AddSingleton<IDictionary<string, LayerProps>, LayerPropertiesDictionary>();
            LoaderExtension.Services.AddSingleton<IDictionary<string, LegendData>, LayerLegendDictionary>();
            LoaderExtension.Services.AddSingleton<IDictionary<string, LegendDrawTemplate>, LayerLegendDrawDictionary>();
            LoaderExtension.Services.AddSingleton<LayerAlteringDictionary>();

            LoaderExtension.Services.AddTransient<ILayerDataProvider<string, LayerProps>>();
            LoaderExtension.Services.AddTransient<Func<string, ILayerDataProvider<string, LayerProps>>>(p => new(path => new SQLiteLayerPropsProvider(path)));
            LoaderExtension.Services.AddTransient<Func<string, ILayerDataProvider<string, LegendData>>>(p => new(path => new SQLiteLayerLegendDataProvider(path)));
            LoaderExtension.Services.AddTransient<Func<string, ILayerDataProvider<string, LegendDrawTemplate>>>(p => new(path => new SQLiteLegendDrawTemplateProvider(path)));
            LoaderExtension.Services.AddTransient<Func<string, ILayerDataProvider<string, string?>>>(p => new(path => new SQLiteAlterLayersProvider(path)));

            LoaderExtension.Services.AddTransient<SQLiteDataProviderFactory<string, LayerProps>>();
            LoaderExtension.Services.AddTransient<SQLiteDataProviderFactory<string, LegendData>>();
            LoaderExtension.Services.AddTransient<SQLiteDataProviderFactory<string, LegendDrawTemplate>>();
            LoaderExtension.Services.AddTransient<SQLiteDataProviderFactory<string, string?>>();
            LoaderExtension.Services.AddTransient<SQLiteLayerDataContextFactory>();

            LoaderExtension.Services.AddSingleton<IEntityFormatter, StandardEntityFormatter>();
        }

        public void Terminate()
        {

        }
    }
}
