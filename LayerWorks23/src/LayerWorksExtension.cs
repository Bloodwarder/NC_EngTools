using LayerWorks.EntityFormatters;
using LoaderCore.Interfaces;
using LoaderCore;
using System.ComponentModel;
using Teigha.Runtime;
using Microsoft.Extensions.DependencyInjection;
using LayerWorks.LayerProcessing;
using LayerWorks.Commands;
using Microsoft.EntityFrameworkCore;
using LayersIO.Connection;

namespace LayerWorks
{
    internal class LayerWorksExtension : IExtensionApplication
    {
        void IExtensionApplication.Initialize()
        {
            TypeDescriptor.AddAttributes(typeof(Teigha.Colors.Color), new TypeConverterAttribute(typeof(TeighaColorTypeConverter)));

            // регистрация сервисов, зависящих от нанокада
            NcetCore.Services.AddSingleton<IEntityFormatter, StandardEntityFormatter>()
                             .AddSingleton<ILayerChecker, LayerChecker>()
                             .AddSingleton<LayerChecker>()
                             .AddSingleton<DrawOrderService>()
                             .AddTransient<LayerAlterer>()
                             .AddTransient<ChapterVisualizer>()
                             .AddTransient<AutoZoner>()
                             .AddTransient<LegendAssembler>()
                             .AddTransient<DrawOrderProcessor>()
                             .AddTransient<LayerEntitiesReportWriter>()
;
        }

        public void Terminate()
        {
            //var contextFactory = NcetCore.ServiceProvider.GetService<IDbContextFactory<LayersDatabaseContextSqlite>>();
            //if (contextFactory is IDisposable disposable)
            //    disposable.Dispose();

        }
    }
}
