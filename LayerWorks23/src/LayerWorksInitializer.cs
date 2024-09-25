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
            NcetCore.Services.AddRepositories(SourceType.InMemory)
                             .AddDataProviderFactories(SourceType.SQLite)
                             .AddDataWriterFactories(SourceType.Xml)
                             .AddSingleton<SQLiteLayerDataContextFactory>();
        }

        private static void LoadParsers()
        {
            var config = NcetCore.ServiceProvider.GetRequiredService<IConfiguration>();
            var parsersPath = config.GetRequiredSection("LayerWorksConfiguration:NameParserPaths:LayerWorksPath")
                                    .Get<LayerWorksPath[]>()!
                                    .Where(p => p.Type == PathRoute.Local)
                                    .Single();
            var parserXmlFiles = parsersPath.DirectoryInfo.GetFiles("LayerParser_*.xml").Select(p => p.FullName).ToArray();
            foreach (var file in parserXmlFiles)
            {
                NameParser.Load(file);
            }
        }
    }

    internal static class ServiceCollectionExtension
    {
        private static Dictionary<SourceType, Action<IServiceCollection>> _repositoryDictionary = new()
        {
            [SourceType.InMemory] = s => AddInMemoryRepositories(s),
            //[SourceType.Xml] = s => AddXmlRepositories(s),
            //[SourceType.Excel] = s => AddExcelRepositories(s),
            //[SourceType.SQLite] = s => AddSQLiteRepositories(s)
        };
        private static Dictionary<SourceType, Action<IServiceCollection>> _dataWriterFactoryDictionary = new()
        {
            //[SourceType.InMemory] = s => AddInMemoryDataWriters(s),
            [SourceType.Xml] = s => AddXmlDataWriters(s),
            //[SourceType.Excel] = s => AddExcelDataWriters(s),
            //[SourceType.SQLite] = s => AddSQLiteDataWriters(s)

        };
        private static Dictionary<SourceType, Action<IServiceCollection>> _dataProviderFactoryDictionary = new()
        {
            //[SourceType.InMemory] = s => AddInMemoryDataProviders(s),
            //[SourceType.Xml] = s => AddXmlDataProviders(s),
            //[SourceType.Excel] = s => AddExcelDataProviders(s),
            [SourceType.SQLite] = s => AddSQLiteDataProviders(s)
        };

        internal static IServiceCollection AddRepositories(this IServiceCollection serviceCollection, SourceType source)
        {
            _repositoryDictionary[source].Invoke(serviceCollection);
            return serviceCollection;
        }

        internal static IServiceCollection AddDataWriterFactories(this IServiceCollection serviceCollection, SourceType source)
        {
            _dataWriterFactoryDictionary[source].Invoke(serviceCollection);
            return serviceCollection;
        }

        internal static IServiceCollection AddDataProviderFactories(this IServiceCollection serviceCollection, SourceType source)
        {
            _dataProviderFactoryDictionary[source].Invoke(serviceCollection);
            return serviceCollection;
        }

        private static void AddInMemoryRepositories(IServiceCollection services)
        {
            services.AddSingleton<IRepository<string, LayerProps>, InMemoryLayerPropsRepository>();
            services.AddSingleton<IRepository<string, LegendData>, InMemoryLayerLegendRepository>();
            services.AddSingleton<IRepository<string, LegendDrawTemplate>, InMemoryLayerLegendDrawRepository>();
            services.AddSingleton<InMemoryLayerAlterRepository>();
        }

        private static void AddXmlDataWriters(IServiceCollection services)
        {
            services.AddTransient<IDataWriterFactory<string, LayerProps>, XmlDataWriterFactory<string, LayerProps>>();
            services.AddTransient<IDataWriterFactory<string, LegendData>, XmlDataWriterFactory<string, LegendData>>();
            services.AddTransient<IDataWriterFactory<string, LegendDrawTemplate>, XmlDataWriterFactory<string, LegendDrawTemplate>>();
            services.AddTransient<IDataWriterFactory<string, string>, XmlDataWriterFactory<string, string>>();
        }

        private static void AddSQLiteDataProviders(IServiceCollection services)
        {
            // используются внутри фабрики (регистрируется в следующих строках)
            services.AddTransient<Func<string, ILayerDataProvider<string, LayerProps>>>(p => new(path => new SQLiteLayerPropsProvider(path)));
            services.AddTransient<Func<string, ILayerDataProvider<string, LegendData>>>(p => new(path => new SQLiteLayerLegendDataProvider(path)));
            services.AddTransient<Func<string, ILayerDataProvider<string, LegendDrawTemplate>>>(p => new(path => new SQLiteLegendDrawTemplateProvider(path)));
            services.AddTransient<Func<string, ILayerDataProvider<string, string?>>>(p => new(path => new SQLiteAlterLayersProvider(path)));

            services.AddTransient<IDataProviderFactory<string, LayerProps>, SQLiteDataProviderFactory<string, LayerProps>>();
            services.AddTransient<IDataProviderFactory<string, LegendData>, SQLiteDataProviderFactory<string, LegendData>>();
            services.AddTransient<IDataProviderFactory<string, LegendDrawTemplate>, SQLiteDataProviderFactory<string, LegendDrawTemplate>>();
            services.AddTransient<IDataProviderFactory<string, string>, SQLiteDataProviderFactory<string, string>>();
        }

    }

    internal enum SourceType
    {
        InMemory = 0,
        Xml = 1,
        Excel = 2,
        SQLite = 3
    }
}
