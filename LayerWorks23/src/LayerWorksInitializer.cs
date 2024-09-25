using LayersIO.DataTransfer;
using LayersIO.ExternalData;
using LayerWorks.EntityFormatters;
using LoaderCore;
using LoaderCore.Interfaces;
using LoaderCore.Integrity;
using Microsoft.Extensions.DependencyInjection;
using LayersIO.Xml;
using LayersIO.Database;
using LayersIO.Database.Readers;
using Microsoft.Extensions.Configuration;
using LoaderCore.Configuration;
using NameClassifiers;

namespace LayerWorks
{
    [NcetModuleInitializer]
    public class LayerWorksInitializer : INcetInitializer
    {
        public void Initialize()
        {
            RegisterServices();
        }
        public void PostInitialize()
        {
            LoadParsers();
        }

        private static void RegisterServices()
        {
            NcetCore.Services.AddSingleton<IRepository<string, LayerProps>, InMemoryLayerPropsRepository>();
            NcetCore.Services.AddSingleton<IRepository<string, LegendData>, InMemoryLayerLegendReader>();
            NcetCore.Services.AddSingleton<IRepository<string, LegendDrawTemplate>, InMemoryLayerLegendDrawRepository>();
            NcetCore.Services.AddSingleton<InMemoryLayerAlterRepository>();

            // TODO: Поменять IDictionary на репозиторий с отдельным интерфейсом
            //NcetCore.Services.AddSingleton<IDictionary<string, LayerProps>, LayerPropertiesDictionary>();
            //NcetCore.Services.AddSingleton<IDictionary<string, LegendData>, LayerLegendDictionary>();
            //NcetCore.Services.AddSingleton<IDictionary<string, LegendDrawTemplate>, LayerLegendDrawDictionary>();
            //NcetCore.Services.AddSingleton<LayerAlteringDictionary>();

            // используются внутри фабрике (регистрируется в следующих строках)
            NcetCore.Services.AddTransient<Func<string, ILayerDataProvider<string, LayerProps>>>(p => new(path => new SQLiteLayerPropsProvider(path)));
            NcetCore.Services.AddTransient<Func<string, ILayerDataProvider<string, LegendData>>>(p => new(path => new SQLiteLayerLegendDataProvider(path)));
            NcetCore.Services.AddTransient<Func<string, ILayerDataProvider<string, LegendDrawTemplate>>>(p => new(path => new SQLiteLegendDrawTemplateProvider(path)));
            NcetCore.Services.AddTransient<Func<string, ILayerDataProvider<string, string?>>>(p => new(path => new SQLiteAlterLayersProvider(path)));

            //NcetCore.Services.AddSingleton<SQLiteDataProviderFactory<string, LayerProps>>();
            //NcetCore.Services.AddSingleton<SQLiteDataProviderFactory<string, LegendData>>();
            //NcetCore.Services.AddSingleton<SQLiteDataProviderFactory<string, LegendDrawTemplate>>();
            //NcetCore.Services.AddSingleton<SQLiteDataProviderFactory<string, string?>>();
            NcetCore.Services.AddSingleton<SQLiteLayerDataContextFactory>();

            NcetCore.Services.AddTransient<IDataProviderFactory<string, LayerProps>, SQLiteDataProviderFactory<string, LayerProps>>();
            NcetCore.Services.AddTransient<IDataProviderFactory<string, LegendData>, SQLiteDataProviderFactory<string, LegendData>>();
            NcetCore.Services.AddTransient<IDataProviderFactory<string, LegendDrawTemplate>, SQLiteDataProviderFactory<string, LegendDrawTemplate>>();
            NcetCore.Services.AddTransient<IDataProviderFactory<string, string>, SQLiteDataProviderFactory<string, string>>();

            NcetCore.Services.AddTransient<IDataWriterFactory<string, LayerProps>, XmlDataWriterFactory<string, LayerProps>>();
            NcetCore.Services.AddTransient<IDataWriterFactory<string, LegendData>, XmlDataWriterFactory<string, LegendData>>();
            NcetCore.Services.AddTransient<IDataWriterFactory<string, LegendDrawTemplate>, XmlDataWriterFactory<string, LegendDrawTemplate>>();
            NcetCore.Services.AddTransient<IDataWriterFactory<string, string>, XmlDataWriterFactory<string, string>>();
        }

        private static void LoadParsers()
        {
            var config = NcetCore.ServiceProvider.GetRequiredService<IConfiguration>();
            var parsersPath = config.GetSection("LayerWorksConfiguration:NameParserPaths:LayerWorksPath")
                                    .Get<LayerWorksPath[]>()
                                    .Where(p => p.Type == PathRoute.Local)
                                    .Single();
            var parserXmlFiles = parsersPath.DirectoryInfo.GetFiles("LayerParser_*.xml").Select(p => p.FullName).ToArray();
            foreach (var file in parserXmlFiles)
            {
                NameParser.Load(file);
            }

        }
    }
}
