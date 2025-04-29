using NameClassifiers;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace LayerWorks.UI
{
    public class LayerTreeNode : INotifyPropertyChanged
    {
        private const string RootNodeName = "RootNode";
        private readonly string? _separator;
        private bool _isIncluded = false;

        public LayerTreeNode(IEnumerable<string[]> decompLayers) : this(decompLayers, RootNodeName, null, null) { }

        private LayerTreeNode(IEnumerable<string[]> decompLayers, string name, LayerTreeNode? parentNode, string? separator = null)
        {
            if (separator == null && name != RootNodeName)
            {
                // для узлов-префиксов ищется разделитель, затем передаётся вглубь
                bool success = NameParser.LoadedParsers.TryGetValue(name, out var parser);
                if (!success)
                    throw new ArgumentException($"Не удалось найти парсер для узла-префикса. Имя - \"{name}\". Загруженные парсеры: {string.Join(",", NameParser.LoadedParsers.Keys)}");
                _separator = parser?.Separator;
            }
            else
            {
                // для корневого узла остаётся пустым
                _separator = separator;
            }

            ParentNode = parentNode;
            Name = name;

            CreateChildren(decompLayers);
        }



        public LayerTreeNode? ParentNode { get; }
        public ObservableCollection<LayerTreeNode> Children { get; } = new();

        public string Name { get; }
        public bool IsIncluded
        {
            get => _isIncluded;
            set
            {
                if (_isIncluded != value || Children.Select(c => c.IsIncluded).Distinct().Count() == 2)
                {
                    _isIncluded = value;
                    OnPropertyChanged();
                    foreach (LayerTreeNode node in Children)
                        node.IsIncluded = value;
                }
            }
        }

        public bool IsChildrenIncluded => Children.Any(c => c.IsIncluded || c.IsChildrenIncluded);

        public bool IsEndpointNode => !Children.Any();

        public string Separator => _separator ?? "_";

        public event PropertyChangedEventHandler? PropertyChanged;

        internal IEnumerable<string> GetResultLayers()
        {
            if (IsEndpointNode)
            {
                if (IsIncluded)
                    yield return GetLayerName();
                yield break;
            }
            foreach (var child in Children)
            {
                foreach (var result in child.GetResultLayers())
                    yield return result;
            }
        }

        internal string GetLayerName()
        {
            LayerTreeNode node = this;
            List<string> sections = new();
            while (node.ParentNode != null)
            {
                sections.Add(node.Name);
                node = node.ParentNode;
            }
            sections.Reverse();
            var result = string.Join(_separator, sections);
            return result;
        }

        private void CreateChildren(IEnumerable<string[]> decompLayers)
        {
            if (decompLayers.Any(d => d.Any()))
            {
                Func<string[], string> nodeNameFunc = d => d[0]; // первый элемент - ключ для группировки и имя нового узла
                Func<string[], string[]> childDecompFunc = d => d.Skip(1).ToArray(); // оставшиеся элементы без имени узла (рекурсивно обработаются внутри создаваемого узла)

                var newNodes = decompLayers.GroupBy(nodeNameFunc)
                                           .Select(g => new LayerTreeNode(g.Select(childDecompFunc), g.Key, this, _separator));
                foreach (var newNode in newNodes)
                {
                    Children.Add(newNode);
                    newNode.PropertyChanged += (s, e) =>
                    {
                        // Обновить статус включён/выключен при обновлении в дочерних узлах
                        if (e.PropertyName == nameof(IsIncluded))
                        {
                            OnPropertyChanged(nameof(IsChildrenIncluded));
                            _isIncluded = Children.All(c => c.IsIncluded);
                            OnPropertyChanged(nameof(IsIncluded));
                        }
                    };
                }
            }
        }

        private protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new(propertyName));
        }
    }
}
