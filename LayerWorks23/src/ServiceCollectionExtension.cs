using LayersIO.DataTransfer;
using LoaderCore.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using LayersIO.Xml;
using LayersIO.Database;
using LayersIO.Database.Readers;
using LayerWorks.DataRepositories;
using LoaderCore.SharedData;

namespace LayerWorks
{
    internal static class ServiceCollectionExtension
    {
        private static readonly Dictionary<SourceType, Action<IServiceCollection>> _repositoryDictionary = new()
        {
            [SourceType.InMemory] = s => AddInMemoryRepositories(s),
            //[SourceType.Xml] = s => AddXmlRepositories(s),
            //[SourceType.Excel] = s => AddExcelRepositories(s),
            //[SourceType.SQLite] = s => AddSQLiteRepositories(s)
        };
        private static readonly Dictionary<SourceType, Action<IServiceCollection>> _dataWriterFactoryDictionary = new()
        {
            //[SourceType.InMemory] = s => AddInMemoryDataWriters(s),
            [SourceType.Xml] = s => AddXmlDataWriters(s),
            //[SourceType.Excel] = s => AddExcelDataWriters(s),
            [SourceType.SQLite] = s => AddSQLiteDataWriters(s)

        };
        private static readonly Dictionary<SourceType, Action<IServiceCollection>> _dataProviderFactoryDictionary = new()
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
            //services.AddSingleton<IRepository<string, LayerProps>, InMemoryRepository<string, LayerProps>>()
            //        .AddSingleton<IRepository<string, LegendData>, InMemoryRepository<string, LegendData>>()
            //        .AddSingleton<IRepository<string, LegendDrawTemplate>, InMemoryRepository<string, LegendDrawTemplate>>()
            //        .AddSingleton<IRepository<string, string>, InMemoryRepository<string, string>>()
            //        .AddSingleton<IRepository<string, ZoneInfo[]>, InMemoryRepository<string, ZoneInfo[]>>();
            services.AddSingleton(typeof(IRepository<,>), typeof(InMemoryRepository<,>));
        }

        private static void AddXmlDataWriters(IServiceCollection services)
        {
            services.AddTransient<IDataWriterFactory<string, LayerProps>, XmlDataWriterFactory<string, LayerProps>>();
            services.AddTransient<IDataWriterFactory<string, LegendData>, XmlDataWriterFactory<string, LegendData>>();
            services.AddTransient<IDataWriterFactory<string, LegendDrawTemplate>, XmlDataWriterFactory<string, LegendDrawTemplate>>();
            services.AddTransient<IDataWriterFactory<string, string>, XmlDataWriterFactory<string, string>>();
            services.AddTransient<IDataWriterFactory<string, ZoneInfo[]>, XmlDataWriterFactory<string, ZoneInfo[]>>();
        }

        private static void AddSQLiteDataWriters(IServiceCollection services)
        {
            services.AddTransient<IDataWriterFactory<string, LayerProps>, SQLiteDataWriterFactory<string, LayerProps>>();
            services.AddTransient<IDataWriterFactory<string, LegendData>, SQLiteDataWriterFactory<string, LegendData>>();
            services.AddTransient<IDataWriterFactory<string, LegendDrawTemplate>, SQLiteDataWriterFactory<string, LegendDrawTemplate>>();
            services.AddTransient<IDataWriterFactory<string, string>, SQLiteDataWriterFactory<string, string>>();
            services.AddTransient<IDataWriterFactory<string, ZoneInfo[]>, SQLiteDataWriterFactory<string, ZoneInfo[]>>();
        }

        private static void AddSQLiteDataProviders(IServiceCollection services)
        {
            // используются внутри фабрики (регистрируется в следующих строках)
            //services.AddTransient<Func<string, ILayerDataProvider<string, LayerProps>>>(p => new(path => new SQLiteLayerPropsProvider(path)));
            //services.AddTransient<Func<string, ILayerDataProvider<string, LegendData>>>(p => new(path => new SQLiteLayerLegendDataProvider(path)));
            //services.AddTransient<Func<string, ILayerDataProvider<string, LegendDrawTemplate>>>(p => new(path => new SQLiteLegendDrawTemplateProvider(path)));
            //services.AddTransient<Func<string, ILayerDataProvider<string, string?>>>(p => new(path => new SQLiteAlterLayersProvider(path)));
            //services.AddTransient<Func<string, ILayerDataProvider<string, ZoneInfo[]>>>(p => new(path => new SQLiteZoneInfoProvider(path)));
            services.AddTransient<ILayerDataProvider<string,LayerProps>, SQLiteLayerPropsProvider>()
                    .AddTransient<ILayerDataProvider<string, LegendData>, SQLiteLayerLegendDataProvider>()
                    .AddTransient<ILayerDataProvider<string, LegendDrawTemplate>, SQLiteLegendDrawTemplateProvider>()
                    .AddTransient<ILayerDataProvider<string, string?>, SQLiteAlterLayersProvider>()
                    .AddTransient<ILayerDataProvider<string, ZoneInfo[]>, SQLiteZoneInfoProvider>();


            services.AddTransient<IDataProviderFactory<string, LayerProps>, SQLiteDataProviderFactory<string, LayerProps>>()
                    .AddTransient<IDataProviderFactory<string, LegendData>, SQLiteDataProviderFactory<string, LegendData>>()
                    .AddTransient<IDataProviderFactory<string, LegendDrawTemplate>, SQLiteDataProviderFactory<string, LegendDrawTemplate>>()
                    .AddTransient<IDataProviderFactory<string, string>, SQLiteDataProviderFactory<string, string>>()
                    .AddTransient<IDataProviderFactory<string, ZoneInfo[]>, SQLiteDataProviderFactory<string, ZoneInfo[]>>();
        }

    }
}
