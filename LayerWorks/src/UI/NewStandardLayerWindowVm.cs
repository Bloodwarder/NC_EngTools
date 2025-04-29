using LoaderCore.UI;
using NameClassifiers;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

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
            LayerTreeNode rootNode = new(decomps);
            RootNode = rootNode;
            CurrentNode = rootNode;
            SelectedNode = CurrentNode.Children.FirstOrDefault();
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

        /// <summary>
        /// Текущий рабочий узел, для которого отображается список дочерних узлов
        /// </summary>
        public LayerTreeNode? CurrentNode
        {
            get => _currentNode;
            set
            {
                _currentNode = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Выбранный дочерний узел
        /// </summary>
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

        /// <summary>
        /// Часть строки поиска, содержащая имя, составленная из текущего узла и всех родительских
        /// </summary>
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

        /// <summary>
        /// Часть строки поиска, содержащая ввод пользователя для поиска среди дочерних слоёв
        /// </summary>
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

        /// <summary>
        /// Полная строка поиска для отображения в UI
        /// </summary>
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

        /// <summary>
        /// Доступные фильтры по статусу для текущего узла
        /// </summary>
        public List<string> AvailableStatusFilters
        {
            get => _availableStatusFilters;
            set
            {
                _availableStatusFilters = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Выбранный фильтр, в соответствии с которым будет осуществляться фильтрация при завершении работы окна
        /// </summary>
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

        /// <summary>
        /// Событие, вызываемое при нормальном завершении работы окна (при выборе конечного узла, при досрочном завершении с выбором (Enter) и без (Esc)
        /// </summary>
        internal event EventHandler? InputCompleted;

        /// <summary>
        /// Добавить строку к строке поиска и найти подходящий дочерний узел. 
        /// При наличии одного - сразу сделает текущим. 
        /// При наличии нескольких - выберет первый. 
        /// При отсутствии сбросит строку поиска
        /// </summary>
        /// <param name="str">Строка для добавления (как правило из одного символа)</param>
        /// <exception cref="InvalidOperationException">Поиск возможен только при непустом текущем узле</exception>
        public void AppendSearch(string str)
        {
            CurrentSearchString += str;
            var search = CurrentNode?.Children.Where(c => c.Name.StartsWith(CurrentSearchString, StringComparison.OrdinalIgnoreCase)) ??
                throw new InvalidOperationException("Поиск возможен только при непустом текущем узле");
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

        /// <summary>
        /// Сделать узел текущим. При выборе концевого узла включает его и завершает работу окна.
        /// </summary>
        /// <param name="obj" cref="LayerTreeNode">Узел, который следует сделать текущим (как правило - выбранный)</param>
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
            // В узлах дочерних корневому содержатся префиксы, по которым следует обновить список доступных фильтров 
            // (так как, когда текщущий узел соответствует корневом, список содержит одно значение по умолчанию)
            if (CurrentNode?.ParentNode == RootNode)
                ResetFilter();
        }

        private static bool CanExecutePreviousNode(object? obj) => obj is LayerTreeNode node && node.ParentNode != null;

        /// <summary>
        /// Сделать текущим родительский узел узла, переданного как параметр. Сам переданный узел становится выбранным.
        /// </summary>
        /// <param name="obj" cref="LayerTreeNode">Узел, родительский узел которого следует сделать текущим (как правило - текущий)</param>
        private void PreviousNode(object? obj)
        {
            var node = (LayerTreeNode)obj!;
            CurrentNode = node.ParentNode;
            SelectedNode = node;
            FixedSearchString = CurrentNode!.GetLayerName();
            CurrentSearchString = string.Empty;
            // При выборе корневого узла фильтры необходимо сбросить
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

        /// <summary>
        /// Включить узел, переданный как параметр, и завершить работу окна. При отсутствии узла или неверном типе параметра - просто завершить работу окна.
        /// </summary>
        /// <param name="obj" cref="LayerTreeNode?">Узел для включения</param>
        private void IncludeAndClose(object? obj)
        {
            if (obj is LayerTreeNode node)
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

        /// <summary>
        /// Обновить список фильтров по имени текущего узла. Для корректной работы имя узла должно быть префиксом, что корректно для дочерних узлов 1 уровня корневого узла
        /// </summary>
        /// <param name="clear">Очистить ли список фильтров до состояния по умолчанию (с одним значением "Без фильтра")</param>
        private void ResetFilter(bool clear = false)
        {
            List<string> filters = new() { NoFilterString };
            if (!clear)
                filters.AddRange(NameParser.LoadedParsers[CurrentNode!.Name].GetStatusArray());

            AvailableStatusFilters = filters;
            SelectedStatusFilter = NoFilterString;
        }

        /// <summary>
        /// Найти все включенные концевые узлы, собрать их имена, и отфильтровать по выбранному фильтру
        /// </summary>
        /// <returns>Имена всех выбранных и отфильтрованных слоёв</returns>
        internal IEnumerable<string> GetResultLayers()
        {
            if (SelectedStatusFilter != NoFilterString)
            {
                Regex regex = new(@$"[^a-zA-Zа-яА-Я]{SelectedStatusFilter}($|[^a-zA-Zа-яА-Я])");
                return RootNode.GetResultLayers().Where(l => regex.IsMatch(l));
            }
            else
            {
                return RootNode.GetResultLayers();
            }
        }
    }
}
