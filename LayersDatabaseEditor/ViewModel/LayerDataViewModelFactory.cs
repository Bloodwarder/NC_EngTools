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
        private static Dictionary<string, PropertyBinder<LayerPropertiesVm>> _propertyBinders = new();
        private static Dictionary<string, PropertyBinder<LayerDrawTemplateVm>> _drawBinders = new();
        static LayerDataViewModelFactory()
        {
            foreach(PropertyInfo propertyInfo in typeof(LayerPropertiesVm).GetProperties())
            {
                _propertyBinders[propertyInfo.Name] = PropertyBinder<LayerPropertiesVm>.Create(propertyInfo.Name);
            }
            foreach (PropertyInfo propertyInfo in typeof(LayerDrawTemplateVm).GetProperties())
            {
                _drawBinders[propertyInfo.Name] = PropertyBinder<LayerDrawTemplateVm>.Create(propertyInfo.Name);
            }
        }

        public static LayerDataVm Create(LayerGroupVm layerGroupViewModel, LayerGroupData layerGroup, string status)
        {
            NameParser parser = NameParser.LoadedParsers[layerGroup.Prefix!];
            LayerData layer = new(layerGroup, status);
            LayerDataVm viewModel = new(layerGroupViewModel, layer, layerGroupViewModel.Database);
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
