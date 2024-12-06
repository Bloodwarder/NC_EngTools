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
using LayerWorks.EntityFormatters;
using LayerWorks.LayerProcessing;

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
            _ = InitializeRepositoriesAsync();
        }

        private static void RegisterServices()
        {
            NcetCore.Services.AddRepositories(SourceType.InMemory)
                             .AddDataProviderFactories(SourceType.SQLite)
                             .AddDataWriterFactories(SourceType.Xml)
                             .AddSingleton<SQLiteLayerDataContextFactory>()
                             .AddSingleton<ILayerChecker, LayerChecker>()
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
            
            Func<string, bool> predicate = defaultLoadedParsers == null ? s => true : s => defaultLoadedParsers.Any(p => s.EndsWith(p));

            var parserXmlFiles = parsersPath.DirectoryInfo!.GetFiles("LayerParser_*.xml")
                                                           .Select(p => p.FullName)
                                                           .Where(predicate)
                                                           .ToArray();
            foreach (var file in parserXmlFiles)
            {
                NameParser.Load(file);
            }
        }

        private static void InitializeRepositories()
        {
            // Заранее инициализировать репозитории, чтобы не было задержки при первом выполнении команды, запрашивающей данные из репозитория
            _ = NcetCore.ServiceProvider.GetService<IRepository<string, LayerProps>>();
            _ = NcetCore.ServiceProvider.GetService<IRepository<string, LegendData>>();
            _ = NcetCore.ServiceProvider.GetService<IRepository<string, LegendDrawTemplate>>();
            _ = NcetCore.ServiceProvider.GetService<IRepository<string, string>>();
            _ = NcetCore.ServiceProvider.GetService<IRepository<string, ZoneInfo[]>>();
        }

        private static async Task InitializeRepositoriesAsync()
        {
            await Task.Run(() => InitializeRepositories());
        }
    }
}
