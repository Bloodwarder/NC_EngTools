using LayersIO.Connection;
using LayersIO.Model;
using LoaderCore.Utilities;
using NameClassifiers;
using NameClassifiers.SharedProperties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace LayersDatabaseEditor.ViewModel
{
    internal static class LayerDataViewModelFactory
    {
        private static Dictionary<string, PropertyBinder<LayerPropertiesViewModel>> _propertyBinders = new();
        private static Dictionary<string, PropertyBinder<LayerDrawTemplateViewModel>> _drawBinders = new();
        static LayerDataViewModelFactory()
        {
            foreach(PropertyInfo propertyInfo in typeof(LayerPropertiesViewModel).GetProperties())
            {
                _propertyBinders[propertyInfo.Name] = PropertyBinder<LayerPropertiesViewModel>.Create(propertyInfo.Name);
            }
            foreach (PropertyInfo propertyInfo in typeof(LayerDrawTemplateViewModel).GetProperties())
            {
                _drawBinders[propertyInfo.Name] = PropertyBinder<LayerDrawTemplateViewModel>.Create(propertyInfo.Name);
            }
        }

        public static LayerDataViewModel Create(LayerGroupViewModel layerGroupViewModel, LayerGroupData layerGroup, string status)
        {
            NameParser parser = NameParser.LoadedParsers[layerGroup.Prefix!];
            LayerData layer = new(layerGroup, status);
            LayerDataViewModel viewModel = new(layerGroupViewModel, layer, layerGroupViewModel.Database);
            viewModel.ResetValues();
            SharedPropertiesCollection sharedProperties = parser.SharedProperties;
            LayerInfoResult layerInfoResult = parser.GetLayerInfo(viewModel.Name);
            if (layerInfoResult.Status != LayerInfoParseStatus.Success)
            {
                throw layerInfoResult.GetExceptions().First();
            }
            LayerInfo layerInfo = layerInfoResult.Value!;

            foreach(var property in sharedProperties.Properties)
            {
                var match = property.Groups?.FirstOrDefault(g => g.DefaultValue != null && g.GetPredicate()(layerInfo));
                if (match != null)
                    _propertyBinders[property.Name].SetProperty(viewModel.LayerProperties, match.DefaultValue!.Value!);
            }

            foreach (var property in sharedProperties.DrawProperties)
            {
                var match = property.Groups?.FirstOrDefault(g => g.DefaultValue != null && g.GetPredicate()(layerInfo));
                if (match != null)
                    _drawBinders[property.Name].SetProperty(viewModel.LayerDrawTemplate, match.DefaultValue!.Value!);
            }
            return viewModel;
        }

    }
}
