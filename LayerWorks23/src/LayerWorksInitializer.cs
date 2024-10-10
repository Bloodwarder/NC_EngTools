using LoaderCore;
using LoaderCore.Interfaces;
using LoaderCore.Integrity;
using Microsoft.Extensions.DependencyInjection;
using LayersIO.Database;
using Microsoft.Extensions.Configuration;
using LoaderCore.Configuration;
using NameClassifiers;
using LayersIO.DataTransfer;

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
            //InitializeRepositories();
            _ = InitializeRepositoriesAsync();
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
            var parserXmlFiles = parsersPath.DirectoryInfo!.GetFiles("LayerParser_*.xml").Select(p => p.FullName).ToArray();
            foreach (var file in parserXmlFiles)
            {
                NameParser.Load(file);
            }
        }

        private static void InitializeRepositories()
        {
            // TODO: по возможности сделать асинхронным - вызывает короткое подвисание (потому что читает всю базу)
            // Заранее инициализировать репозитории, чтобы не было задержки при первом выполнении команды, запрашивающей данные из репозитория
            _ = NcetCore.ServiceProvider.GetService<IRepository<string, LayerProps>>();
            _ = NcetCore.ServiceProvider.GetService<IRepository<string, LegendData>>();
            _ = NcetCore.ServiceProvider.GetService<IRepository<string, LegendDrawTemplate>>();
            _ = NcetCore.ServiceProvider.GetService<IRepository<string, string>>();
        }

        private static async Task InitializeRepositoriesAsync()
        {
            await Task.Run(() => InitializeRepositories());
        }
    }
}
