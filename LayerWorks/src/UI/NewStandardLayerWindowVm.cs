using LoaderCore.UI;
using NameClassifiers;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace LayerWorks.UI
{
    public class NewStandardLayerWindowVm : INotifyPropertyChanged
    {
        private const string NoFilterString = "Без фильтра";
        private LayerTreeNode? _currentNode;
        private LayerTreeNode? _selectedNode;
        private string _currentSearchString = string.Empty;
        private string _fixedSearchString = string.Empty;
        private List<string> _availableStatusFilters = new();
        private string _selectedStatusFilter = NoFilterString;

        public NewStandardLayerWindowVm(IEnumerable<string> layers)
        {
            var decomps = from string layer in layers
                          let prefix = NameParser.GetPrefix(layer)
                          let separator = NameParser.LoadedParsers[prefix].Separator
                          select layer.Split(separator);
            LayerTreeNode rootNode = new(null, "RootNode", decomps);
            RootNode = rootNode;
            CurrentNode = rootNode;
            ResetFilter(clear: true);

            NextNodeCommand = new(NextNode, CanExecuteNextNode);
            PreviousNodeCommand = new(PreviousNode, CanExecutePreviousNode);
            IncludeNodeCommand = new(IncludeNode, CanExecuteChangeNodeIncludeState);
            ExcludeNodeCommand = new(ExcludeNode, CanExecuteChangeNodeIncludeState);
            ChangeNodeIncludeStateCommand = new(ChangeNodeIncludeState, CanExecuteChangeNodeIncludeState);
            IncludeAndCloseCommand = new(IncludeAndClose, o => true);
            RollFilterUpCommand = new(RollFilterUp, CanRollFilterUp);
            RollFilterDownCommand = new(RollFilterDown, CanRollFilterDown);
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

        public string FixedSearchString
        {
            get => _fixedSearchString;
            set
            {
                _fixedSearchString = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(FullSearchString));
            }
        }

        public string CurrentSearchString
        {
            get => _currentSearchString;
            set
            {
                _currentSearchString = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(FullSearchString));
            }
        }

        public string FullSearchString
        {
            get
            {
                if (!string.IsNullOrEmpty(CurrentSearchString) || !string.IsNullOrEmpty(FixedSearchString))
                    return string.Join(CurrentNode?.Separator, FixedSearchString, CurrentSearchString);
                else
                    return string.Empty;
            }
        }

        public List<string> AvailableStatusFilters
        {
            get => _availableStatusFilters;
            set
            {
                _availableStatusFilters = value;
                OnPropertyChanged();
            }
        }

        public string SelectedStatusFilter
        {
            get => _selectedStatusFilter;
            set
            {
                _selectedStatusFilter = value;
                OnPropertyChanged();
            }
        }

        public RelayCommand NextNodeCommand { get; }
        public RelayCommand PreviousNodeCommand { get; }
        public RelayCommand IncludeNodeCommand { get; }
        public RelayCommand ExcludeNodeCommand { get; }
        public RelayCommand ChangeNodeIncludeStateCommand { get; }
        public RelayCommand IncludeAndCloseCommand { get; }
        public RelayCommand RollFilterUpCommand { get; }
        public RelayCommand RollFilterDownCommand { get; }


        public event PropertyChangedEventHandler? PropertyChanged;
        internal event EventHandler? InputCompleted;

        public void AppendSearch(string str)
        {
            CurrentSearchString += str;
            var search = CurrentNode?.Children.Where(c => c.Name.StartsWith(CurrentSearchString, StringComparison.OrdinalIgnoreCase)) ??
                throw new InvalidOperationException("Поиск возможен только при непустом выбранном узле");
            if (!search.Any())
            {
                CurrentSearchString = string.Empty;
            }
            else if (search.Count() == 1)
            {
                var node = search.First();
                if (NextNodeCommand.CanExecute(node))
                    NextNodeCommand.Execute(node);
            }
            else
            {
                SelectedNode = search.First();
            }
        }

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
                FixedSearchString = node.GetLayerName();
                CurrentSearchString = string.Empty;
            }
            if (CurrentNode?.ParentNode == RootNode)
                ResetFilter();
        }

        private static bool CanExecutePreviousNode(object? obj) => obj is LayerTreeNode node && node.ParentNode != null;

        private void PreviousNode(object? obj)
        {
            var node = (LayerTreeNode)obj!;
            CurrentNode = CurrentNode!.ParentNode;
            SelectedNode = node;
            FixedSearchString = CurrentNode!.GetLayerName();
            CurrentSearchString = string.Empty;
            if (CurrentNode == RootNode)
                ResetFilter(clear: true);
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

        private bool CanRollFilterUp(object? obj)
        {
            if (obj is string str)
            {
                return AvailableStatusFilters.IndexOf(str) > 0;
            }
            return false;
        }

        private void RollFilterUp(object? obj)
        {
            int index = AvailableStatusFilters.IndexOf((string)obj!);
            SelectedStatusFilter = AvailableStatusFilters.ElementAt(index - 1);
        }

        private bool CanRollFilterDown(object? obj)
        {
            if (obj is string str)
            {
                return AvailableStatusFilters.IndexOf(str) < AvailableStatusFilters.Count - 1;
            }
            return false;
        }

        private void RollFilterDown(object? obj)
        {
            int index = AvailableStatusFilters.IndexOf((string)obj!);
            SelectedStatusFilter = AvailableStatusFilters.ElementAt(index + 1);
        }

        private void ResetFilter(bool clear = false)
        {
            List<string> filters = new() { NoFilterString };
            if (!clear)
                filters.AddRange(NameParser.LoadedParsers[CurrentNode!.Name].GetStatusArray());

            AvailableStatusFilters = filters;
            SelectedStatusFilter = NoFilterString;
        }

        internal IEnumerable<string> GetResultLayers()
        {
            if (SelectedStatusFilter != NoFilterString)
                return RootNode.GetResultLayers().Where(l => l.Contains(SelectedStatusFilter));
            else
                return RootNode.GetResultLayers();
        }
    }
}
