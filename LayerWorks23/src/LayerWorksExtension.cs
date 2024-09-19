using LayersIO.DataTransfer;
using LayersIO.ExternalData;
using LayerWorks.EntityFormatters;
using LoaderCore;
using LoaderCore.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;
using Teigha.Runtime;
using LayersIO.Xml;
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
            NcetCore.Services.AddTransient<IStandardReader<LayerProps>, InMemoryLayerPropsReader>();
            NcetCore.Services.AddTransient<IStandardReader<LegendData>, InMemoryLayerLegendReader>();
            NcetCore.Services.AddTransient<IStandardReader<LegendDrawTemplate>, InMemoryLayerLegendDrawReader>();
            NcetCore.Services.AddTransient<InMemoryLayerAlterReader>();

            NcetCore.Services.AddSingleton<IDictionary<string, LayerProps>, LayerPropertiesDictionary>();
            NcetCore.Services.AddSingleton<IDictionary<string, LegendData>, LayerLegendDictionary>();
            NcetCore.Services.AddSingleton<IDictionary<string, LegendDrawTemplate>, LayerLegendDrawDictionary>();
            NcetCore.Services.AddSingleton<LayerAlteringDictionary>();

            NcetCore.Services.AddTransient<ILayerDataProvider<string, LayerProps>>();
            NcetCore.Services.AddTransient<Func<string, ILayerDataProvider<string, LayerProps>>>(p => new(path => new SQLiteLayerPropsProvider(path)));
            NcetCore.Services.AddTransient<Func<string, ILayerDataProvider<string, LegendData>>>(p => new(path => new SQLiteLayerLegendDataProvider(path)));
            NcetCore.Services.AddTransient<Func<string, ILayerDataProvider<string, LegendDrawTemplate>>>(p => new(path => new SQLiteLegendDrawTemplateProvider(path)));
            NcetCore.Services.AddTransient<Func<string, ILayerDataProvider<string, string?>>>(p => new(path => new SQLiteAlterLayersProvider(path)));

            NcetCore.Services.AddTransient<SQLiteDataProviderFactory<string, LayerProps>>();
            NcetCore.Services.AddTransient<SQLiteDataProviderFactory<string, LegendData>>();
            NcetCore.Services.AddTransient<SQLiteDataProviderFactory<string, LegendDrawTemplate>>();
            NcetCore.Services.AddTransient<SQLiteDataProviderFactory<string, string?>>();
            NcetCore.Services.AddTransient<SQLiteLayerDataContextFactory>();

            NcetCore.Services.AddTransient<IDataProviderFactory<string, LayerProps>, SQLiteDataProviderFactory<string, LayerProps>>();
            NcetCore.Services.AddTransient<IDataProviderFactory<string, LegendData>, SQLiteDataProviderFactory<string, LegendData>>();
            NcetCore.Services.AddTransient<IDataProviderFactory<string, LegendDrawTemplate>, SQLiteDataProviderFactory<string, LegendDrawTemplate>>();
            NcetCore.Services.AddTransient<IDataProviderFactory<string, string>, SQLiteDataProviderFactory<string, string>>();

            NcetCore.Services.AddTransient<IDataWriterFactory<string, LayerProps>, XmlDataWriterFactory<string, LayerProps>>();
            NcetCore.Services.AddTransient<IDataWriterFactory<string, LegendData>, XmlDataWriterFactory<string, LegendData>>();
            NcetCore.Services.AddTransient<IDataWriterFactory<string, LegendDrawTemplate>, XmlDataWriterFactory<string, LegendDrawTemplate>>();
            NcetCore.Services.AddTransient<IDataWriterFactory<string, string>, XmlDataWriterFactory<string, string>>();


            NcetCore.Services.AddSingleton<IEntityFormatter, StandardEntityFormatter>();
        }

        public void Terminate()
        {

        }
    }
}
