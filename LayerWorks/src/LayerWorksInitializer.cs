﻿using LoaderCore;
using LoaderCore.Interfaces;
using LoaderCore.Integrity;
using Microsoft.Extensions.DependencyInjection;
using LayersIO.Database;
using Microsoft.Extensions.Configuration;
using LoaderCore.Configuration;
using NameClassifiers;
using LayersIO.DataTransfer;
using LayersIO.Excel;
using LoaderCore.SharedData;
using LayerWorks.Commands;
using LayerWorks.EntityFormatters;
using System.ComponentModel;
using Microsoft.Extensions.Logging;
using LayersIO.Connection;
using Microsoft.EntityFrameworkCore;

namespace LayerWorks
{
    [NcetModuleInitializer]
    public class LayerWorksInitializer : INcetInitializer
    {
        public void Initialize()
        {
            RegisterServices();
            TypeDescriptor.AddAttributes(typeof(System.Windows.Media.Color), new TypeConverterAttribute(typeof(WindowsColorTypeConverter)));
            TinyMapperConfigurer.Configure();
        }
        public void PostInitialize()
        {
            LoadParsers();
            _ = InitializeRepositoriesAsync();
        }

        private static void RegisterServices()
        {
            // Зарегистрировать сервисы, не зависящие от нанокада
            // Зависящие регистрируются в LayerWorksExtension

            _ = NcetCore.Services.AddRepositories(SourceType.InMemory)
                             .AddDataProviderFactories(SourceType.SQLite)
                             .AddDataWriterFactories(SourceType.SQLite)
                             .AddScoped<IDbContextFactory<LayersDatabaseContextSqlite>, SQLiteFallbackContextFactory>()
                             .AddSingleton<SQLiteLayerDataContextFactory>()
                             .AddTransient(typeof(IReportWriterFactory<>), typeof(ExcelSimpleReportWriterFactory<>));
        }

        private static void LoadParsers()
        {
            var config = NcetCore.ServiceProvider.GetRequiredService<IConfiguration>();
            var parsersPath = config.GetRequiredSection("LayerWorksConfiguration:NameParserPaths:LayerWorksPath")
                                    .Get<LayerWorksPath[]>()!
                                    .Where(p => p.Type == PathRoute.Local)
                                    .Single();
            var defaultLoadedParsers = config.GetSection("LayerWorksConfiguration:DefaultParsers:Parser")?.Get<string[]>();

            Func<string, bool> predicate = defaultLoadedParsers == null ? s => true : s => defaultLoadedParsers.Any(p => s.EndsWith($"{p}.xml"));

            var parserXmlFiles = parsersPath.DirectoryInfo!.GetFiles("LayerParser_*.xml")
                                                           .Select(p => p.FullName)
                                                           .Where(predicate)
                                                           .ToArray();

            for (int i = parserXmlFiles.Length - 1; i >= 0; i--)
            {
                var logger = NcetCore.ServiceProvider.GetService<ILogger>();
                try
                {
                    NameParser.Load(parserXmlFiles[i]);
                }
                catch (Exception ex)
                {
                    logger?.LogError(ex, "{Message}", ex.Message);
                    continue;
                }
            }
        }

        private static void InitializeRepositories()
        {
            // Заранее инициализировать репозитории, чтобы не было задержки при первом выполнении команды, запрашивающей данные из репозитория
            using (IServiceScope scope = NcetCore.ServiceProvider.CreateScope())
            {
                _ = NcetCore.ServiceProvider.GetService<IRepository<string, LayerProps>>();
                _ = NcetCore.ServiceProvider.GetService<IRepository<string, LegendData>>();
                _ = NcetCore.ServiceProvider.GetService<IRepository<string, LegendDrawTemplate>>();
                _ = NcetCore.ServiceProvider.GetService<IRepository<string, string>>();
                _ = NcetCore.ServiceProvider.GetService<IRepository<string, ZoneInfo[]>>();

                var factory = NcetCore.ServiceProvider.GetService<IDbContextFactory<LayersDatabaseContextSqlite>>();
                if (factory is IDisposable disposable)
                    disposable.Dispose();
            }
        }

        private static async Task InitializeRepositoriesAsync()
        {
            await Task.Run(() => InitializeRepositories());
        }
    }
}
