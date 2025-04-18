using NameClassifiers;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace LayerWorks.UI
{
    public class LayerTreeNode : INotifyPropertyChanged
    {
        private readonly string? _separator;
        private bool _isIncluded = false;

        public LayerTreeNode(LayerTreeNode? parentNode, string name, IEnumerable<string[]> decompLayers, string? separator = null)
        {
            if (separator == null)
            {
                bool success = NameParser.LoadedParsers.TryGetValue(name, out var parser);
                _separator = parser?.Separator;
            }
            else
            {
                _separator = separator!;
            }
            ParentNode = parentNode;
            Name = name;
            if (decompLayers.Any(d => d.Any()))
            {
                var newNodes = decompLayers.GroupBy(d => d[0])
                                           .Select(g => new LayerTreeNode(this, g.Key, g.Select(s => s.Skip(1).ToArray()), _separator));
                foreach (var newNode in newNodes)
                {
                    Children.Add(newNode);
                    newNode.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName == nameof(IsIncluded))
                        {
                            OnPropertyChanged(nameof(IsChildrenIncluded));
                            _isIncluded = Children.All(c => c.IsIncluded);
                            OnPropertyChanged(nameof(IsIncluded));
                        }
                    };
                }
            }
            _separator = separator;
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

        private protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new(propertyName));
        }
    }
}
