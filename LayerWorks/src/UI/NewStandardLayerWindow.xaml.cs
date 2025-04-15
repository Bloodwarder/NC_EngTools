using LoaderCore.UI;
using NameClassifiers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace LayerWorks.UI
{
    /// <summary>
    /// Логика взаимодействия для NewStandardLayerWindow.xaml
    /// </summary>
    public partial class NewStandardLayerWindow : Window
    {
        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register("ViewModel", typeof(NewStandardLayerWindowVm), typeof(NewStandardLayerWindow), new PropertyMetadata());
        public NewStandardLayerWindow(IEnumerable<string> layers)
        {
            InitializeComponent();
            ViewModel = new(layers);
            ViewModel.InputCompleted += (s, e) => this.Close();
            lvNodes.SelectedIndex = 0;
            Dispatcher.Invoke(() => lvNodes.Focus());
            ViewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(NewStandardLayerWindowVm.SelectedNode) && ViewModel.SelectedNode != null)
                {
                    lvNodes.ScrollIntoView(ViewModel.SelectedNode);
                    var container = lvNodes.ItemContainerGenerator.ContainerFromItem(ViewModel.SelectedNode) as ListViewItem;
                    container?.Focus();
                }
            };
            this.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        }
        public NewStandardLayerWindowVm ViewModel
        {
            get => (NewStandardLayerWindowVm)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        public IEnumerable<string> GetResultLayers() => ViewModel.RootNode.GetResultLayers();

    }

    public class NewStandardLayerWindowVm : INotifyPropertyChanged
    {
        private LayerTreeNode? _currentNode;
        private LayerTreeNode? _selectedNode;

        public NewStandardLayerWindowVm(IEnumerable<string> layers)
        {
            Dictionary<string, string> separators = layers.Select(l => NameParser.GetPrefix(l))
                                                          .Where(p => !string.IsNullOrEmpty(p))
                                                          .Distinct()
                                                          .ToDictionary(p => p!, p => NameParser.LoadedParsers[p].Separator);
            var decomps = layers.Select(s => s.Split("_")); // TODO: убрать сепаратор в хардкоде
            LayerTreeNode rootNode = new(null, "RootNode", decomps);
            CurrentNode = rootNode;
            RootNode = rootNode;

            NextNodeCommand = new(NextNode, CanExecuteNextNode);
            PreviousNodeCommand = new(PreviousNode, CanExecutePreviousNode);
            IncludeNodeCommand = new(IncludeNode, CanExecuteChangeNodeIncludeState);
            ExcludeNodeCommand = new(ExcludeNode, CanExecuteChangeNodeIncludeState);
            ChangeNodeIncludeStateCommand = new(ChangeNodeIncludeState, CanExecuteChangeNodeIncludeState);
            IncludeAndCloseCommand = new(IncludeAndClose, o => true);
        }

        public LayerTreeNode? CurrentNode
        {
            get => _currentNode;
            set
            {
                _currentNode = value;
                OnPropertyChanged();
            }
        }

        public LayerTreeNode? SelectedNode
        {
            get => _selectedNode;
            set
            {
                _selectedNode = value;
                OnPropertyChanged();
            }
        }

        public LayerTreeNode RootNode { get; }

        public RelayCommand NextNodeCommand { get; }
        public RelayCommand PreviousNodeCommand { get; }
        public RelayCommand IncludeNodeCommand { get; }
        public RelayCommand ExcludeNodeCommand { get; }
        public RelayCommand ChangeNodeIncludeStateCommand { get; }
        public RelayCommand IncludeAndCloseCommand { get; }


        public event PropertyChangedEventHandler? PropertyChanged;
        internal event EventHandler? InputCompleted;
        private protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new(propertyName));
        }

        private bool CanExecuteNextNode(object? obj) => obj is LayerTreeNode;

        private void NextNode(object? obj)
        {
            LayerTreeNode node = (LayerTreeNode)obj!;
            if (node!.IsEndpointNode)
            {
                node.IsIncluded = true;
                InputCompleted?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                CurrentNode = node;
                SelectedNode = node.Children.FirstOrDefault();
            }
        }

        private static bool CanExecutePreviousNode(object? obj) => obj is LayerTreeNode node && node.ParentNode != null;

        private void PreviousNode(object? obj)
        {
            var node = (LayerTreeNode)obj!;
            CurrentNode = CurrentNode!.ParentNode;
            SelectedNode = node;
        }

        private static bool CanExecuteChangeNodeIncludeState(object? obj) => obj is LayerTreeNode;

        private static void ExcludeNode(object? obj) => ((LayerTreeNode)obj!).IsIncluded = false;

        private void IncludeNode(object? obj) => ((LayerTreeNode)obj!).IsIncluded = true;
        private void ChangeNodeIncludeState(object? obj)
        {
            var node = (LayerTreeNode)obj!;
            node.IsIncluded = !node.IsIncluded;
        }

        private void IncludeAndClose(object? obj)
        {
            LayerTreeNode? node = obj as LayerTreeNode;
            if (node != null)
                node.IsIncluded = true;
            InputCompleted?.Invoke(this, EventArgs.Empty);
        }
    }


    public class LayerTreeNode : INotifyPropertyChanged
    {
        private bool _isIncluded = false;

        public LayerTreeNode(LayerTreeNode? parentNode, string name, IEnumerable<string[]> decompLayers)
        {
            ParentNode = parentNode;
            Name = name;
            if (decompLayers.Any(d => d.Any()))
            {
                var newNodes = decompLayers.GroupBy(d => d[0])
                                           .Select(g => new LayerTreeNode(this, g.Key, g.Select(s => s.Skip(1).ToArray())));
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
        }

        public LayerTreeNode? ParentNode { get; }
        public ObservableCollection<LayerTreeNode> Children { get; } = new();

        public string Name { get; }
        public bool IsIncluded
        {
            get => _isIncluded;
            set
            {
                if (_isIncluded != value)
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
        private string GetLayerName()
        {
            LayerTreeNode node = this;
            List<string> sections = new();
            while (node.ParentNode != null)
            {
                sections.Add(node.Name);
                node = node.ParentNode;
            }
            sections.Reverse();
            var result = string.Join("_", sections); // TODO: ещё один разделитель в хардкоде
            return result;
        }

        private protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new(propertyName));
        }
    }
}
