using LayerWorks.EntityFormatters;
using LoaderCore.Interfaces;
using LoaderCore;
using System.ComponentModel;
using Teigha.Runtime;
using Microsoft.Extensions.DependencyInjection;
using LayerWorks.LayerProcessing;
using HostMgd.ApplicationServices;
using HostMgd.Windows;

namespace LayerWorks
{
    internal class LayerWorksExtension : IExtensionApplication
    {
        void IExtensionApplication.Initialize()
        {
            TypeDescriptor.AddAttributes(typeof(Teigha.Colors.Color), new TypeConverterAttribute(typeof(TeighaColorTypeConverter)));

            NcetCore.Services.AddSingleton<IEntityFormatter, StandardEntityFormatter>()
                             .AddSingleton<ILayerChecker, LayerChecker>();
            TestButtons();
        }

        public void Terminate()
        {

        }

        private void TestButtons()
        {
            var menu = Application.MenuBar;
            var o2 = Application.MenuGroups;
        }
    }
}
