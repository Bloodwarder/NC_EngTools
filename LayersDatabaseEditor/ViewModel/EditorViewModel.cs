using LayersDatabaseEditor.UI;
using LayersDatabaseEditor.Utilities;
using LayersIO.Connection;
using LayersIO.Database;
using LayersIO.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using NameClassifiers;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows;

namespace LayersDatabaseEditor.ViewModel
{
    public class EditorViewModel : INotifyPropertyChanged, IDbRelatedViewModel
    {
        private LayersDatabaseContextSqlite? _db;
        private SQLiteLayerDataContextFactory _dbContextFactory;
        private LayerGroupViewModel? _selectedGroup;
        private LayerDataViewModel? _selectedLayer;
        private RelayCommand? _connectCommand;
        private RelayCommand? _disconnectCommand;
        private RelayCommand? _changeSelectedGroupCommand;
        private RelayCommand? _addNewLayerGroupCommand;
        private RelayCommand? _deleteLayerGroupsCommand;
        private RelayCommand? _updateDatabaseCommand;
        private RelayCommand? _resetDatabaseCommand;
        private RelayCommand? _saveDbAsCommand;
        private RelayCommand? _openZoneEditorCommand;

        private HashSet<int> _selectedIndexes = new();
        private string _groupInputText = string.Empty;
        private string? _databasePath;

        public EditorViewModel(SQLiteLayerDataContextFactory contextFactory)
        {
            _dbContextFactory = contextFactory;
            UpdatedGroups.CollectionChanged += (s, e) =>
            {
                OnPropertyChanged(nameof(IsValid));
                OnPropertyChanged(nameof(IsUpdated));
            };
            GroupIdsToDelete.CollectionChanged += (s, e) =>
            {
                OnPropertyChanged(nameof(IsUpdated));
            };

        }

        /// <summary>
        /// Команда подключения к базе данных
        /// </summary>
        public RelayCommand ConnectCommand
        {
            get => _connectCommand ??= new(obj => DatabaseConnect(obj), obj => true);
            set => _connectCommand = value;
        }

        /// <summary>
        /// Команда отключения от базы данных
        /// </summary>
        public RelayCommand DisconnectCommand
        {
            get => _disconnectCommand ??= new(obj => DatabaseDisconnect(), obj => IsConnected);
            set => _disconnectCommand = value;
        }

        /// <summary>
        /// Команда выбора новой группы слоёв для редактирования
        /// </summary>
        public RelayCommand ChangeSelectedGroupCommand
        {
            get => _changeSelectedGroupCommand ??= new(obj => SelectGroup(obj), obj => IsConnected);
            set => _changeSelectedGroupCommand = value;
        }

        /// <summary>
        /// Команда создания новой группы слоёв
        /// </summary>
        public RelayCommand AddNewLayerGroupCommand
        {
            get => _addNewLayerGroupCommand ??= new(obj => AddNewLayerGroup(obj), obj => IsConnected && GroupInputState == GroupInputState.ValidNew);
            set => _addNewLayerGroupCommand = value;
        }

        /// <summary>
        /// Команда удаления группы слоёв
        /// </summary>
        public RelayCommand DeleteLayerGroupsCommand
        {
            get => _deleteLayerGroupsCommand ??= new(obj => DeleteLayerGroups(obj), obj => IsConnected && (SelectedGroup != null || _selectedIndexes.Any()));
            set => _deleteLayerGroupsCommand = value;
        }

        /// <summary>
        /// Команда обновления базы данных (всех изменений, изменений группы или изменений слоя)
        /// </summary>
        public RelayCommand UpdateDatabaseCommand
        {
            get => _updateDatabaseCommand ??= new(obj => UpdateDatabaseViewModel(obj), obj => IsConnected
                                                                                              && ((obj as IDbRelatedViewModel)?.IsValid ?? false)
                                                                                              && ((obj as IDbRelatedViewModel)?.IsUpdated ?? false));
            set => _updateDatabaseCommand = value;
        }

        /// <summary>
        /// Команда отката изменений в ViewModel до исходных значений из БД
        /// </summary>
        public RelayCommand ResetDatabaseCommand
        {
            get => _resetDatabaseCommand ??= new(obj => ResetDatabaseViewModel(obj), obj => IsConnected && ((obj as IDbRelatedViewModel)?.IsUpdated ?? false));
            set => _resetDatabaseCommand = value;
        }

        /// <summary>
        /// Сохранение базы данных по другому пути (Команда для SQLite - работает на уровне файлов)
        /// </summary>
        public RelayCommand SaveDbAsCommand
        {
            get => _saveDbAsCommand ??= new(obj => SaveDbAs(obj), obj => IsConnected && ((obj as EditorViewModel)?.IsValid ?? false));
            set => _saveDbAsCommand = value;
        }

        /// <summary>
        /// Открыть редактор зон для выбранной группы слоёв
        /// </summary>
        public RelayCommand OpenZoneEditorCommand
        {
            get => _openZoneEditorCommand ??= new(obj => OpenZoneEditor(obj), obj => IsConnected && IsGroupSelected);
            set => _openZoneEditorCommand = value;
        }



        /// <summary>
        /// Имена групп слоёв из БД (с учётом подготовленных изменений)
        /// </summary>
        public ObservableHashSet<string> LayerGroupNames { get; private set; } = new();

        public bool IsConnected => _db != null;
        public bool IsLayerSelected => SelectedLayer != null;
        public bool IsGroupSelected => SelectedGroup != null;

        internal LayersDatabaseContextSqlite? Database { get => _db; set => _db = value; }

        /// <summary>
        /// Выбранная группа слоёв
        /// </summary>
        public LayerGroupViewModel? SelectedGroup
        {
            get => _selectedGroup;
            set
            {

                _selectedGroup = value;
                OnPropertyChanged(nameof(SelectedGroup));
                OnPropertyChanged(nameof(IsGroupSelected));
                OnPropertyChanged(nameof(IsValid));
            }
        }

        /// <summary>
        /// Выбранный слой
        /// </summary>
        public LayerDataViewModel? SelectedLayer
        {
            get => _selectedLayer;
            set
            {
                _selectedLayer = value;
                OnPropertyChanged(nameof(SelectedLayer));
                OnPropertyChanged(nameof(IsLayerSelected));
            }
        }

        /// <summary>
        /// Группы слоёв с несохранёнными обновлениями за исключением выбранной
        /// </summary>
        public ObservableCollection<LayerGroupViewModel> UpdatedGroups { get; private set; } = new();

        /// <summary>
        /// Id групп, помеченных к удалению из БД
        /// </summary>
        public ObservableHashSet<int> GroupIdsToDelete { get; private set; } = new();

        /// <summary>
        /// Текст для ввода и проверки имени слоя перед добавлением/удалением
        /// </summary>
        public string GroupInputText
        {
            get => _groupInputText;
            set
            {
                _groupInputText = value;
                OnPropertyChanged(nameof(GroupInputText));
                OnPropertyChanged(nameof(GroupInputState));
            }
        }

        /// <summary>
        /// Результат проверки введённого имени слоя - существующий, корректный для добавления, некорректный
        /// </summary>
        public GroupInputState GroupInputState
        {
            get
            {
                if (LayerGroupNames.Contains(GroupInputText))
                    return GroupInputState.Existing;
                if (string.IsNullOrEmpty(GroupInputText))
                    return GroupInputState.None;
                LayerInfoResult result = NameParser.ParseLayerName(GroupInputText);
                if ((result.Status == LayerInfoParseStatus.Success && result.Value!.Status == null) ||
                    (result.Status == LayerInfoParseStatus.PartialFailure && result.GetExceptions().All(e => e is WrongStatusException)))
                    return GroupInputState.ValidNew;
                else
                    return GroupInputState.Invalid;
            }
        }

        public bool IsValid => (SelectedGroup?.IsValid ?? true) && UpdatedGroups.All(g => g.IsValid);

        public bool IsUpdated => (SelectedGroup?.IsUpdated ?? false) || UpdatedGroups.Any() || GroupIdsToDelete.Any();

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string property = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }

        /// <summary>
        /// Подключиться к БД
        /// </summary>
        /// <param name="obj">путь к файлу БД (SQLite)</param>
        private void DatabaseConnect(object? obj = null)
        {
            string fileName;
            if (obj == null)
            {
                // Найти папку по умолчанию, где должен лежать файл БД
                DirectoryInfo di = new FileInfo(Assembly.GetExecutingAssembly().Location).Directory!.Parent!.Parent!.GetDirectories("UserData").First();
                OpenFileDialog ofd = new OpenFileDialog()
                {
                    DefaultExt = ".db",
                    Filter = "SQLite database files|*.db;*.sqlite",
                    CheckFileExists = true,
                    Multiselect = false,
                    InitialDirectory = di.FullName
                };
                var result = ofd.ShowDialog();
                if (result == true)
                {
                    fileName = ofd.FileName;
                }
                else
                {
                    return;
                }
            }
            else
            {
                fileName = (string)obj;
            }
            _databasePath = fileName;

            // Очистить существующие выборы и несохранённые операции обновления и удаления, затем уничтожить контекст
            _selectedIndexes.Clear();
            UpdatedGroups.Clear();
            GroupIdsToDelete.Clear();
            SelectedLayer = null;
            SelectedGroup = null;

            _db?.Dispose();

            // Создать новый контекст для файла по ранее полученному пути
            try
            {
                _db = _dbContextFactory.CreateDbContext(fileName);
            }
            catch (Exception ex)
            {
                // Log
                return;
            }

            // Очистить имена групп и получить из открытой БД новые
            LayerGroupNames.Clear();
            _db.LayerGroups.AsNoTracking()
                           .Select(lg => new { lg.Prefix, lg.MainName })
                           .OrderBy(s => s.Prefix)
                           .ThenBy(s => s.MainName)
                           .AsEnumerable()
                           .Select(s => $"{s.Prefix}_{s.MainName}")
                           .ToList()
                           .ForEach(n => LayerGroupNames.Add(n));
            // Оповестить UI
            OnPropertyChanged(nameof(IsConnected));
            OnPropertyChanged(nameof(IsUpdated));
        }

        /// <summary>
        /// Отключиться от БД
        /// </summary>
        private void DatabaseDisconnect()
        {
            SelectedGroup = null;
            SelectedLayer = null;
            _db?.Dispose();
            _db = null;
            LayerGroupNames.Clear();
            OnPropertyChanged(nameof(IsConnected));
            OnPropertyChanged(nameof(IsUpdated));
        }

        /// <summary>
        /// Выбрать группу
        /// </summary>
        /// <param name="obj">строка для поиска (имя или начало имени группы)</param>
        private void SelectGroup(object? obj)
        {
            if (obj == null)
                return;
            string searchString = (string)obj;
            // Определить префикс и основное имя
            var match = Regex.Match(searchString, @"(^[^_\s-\.]+)_?(.*$)?");
            string prefix = match.Groups[1].Value;
            string mainName = match.Groups[2].Value;

            // Если текущая выбранная группа имеет несохранённые обновления - сохранить её в коллекции обновлённых групп (но не в БД)
            if (SelectedGroup?.IsUpdated ?? false)
                UpdatedGroups.Add(SelectedGroup);
            // Проверить, была ли планируемая к выбору группа ранее сохранена в коллекции UpdatedGroups
            var cachedGroup = UpdatedGroups.Where(g => g.Prefix == prefix && g.MainName == mainName).FirstOrDefault();
            if (cachedGroup != null)
            {
                // Если да - вытащить оттуда
                SelectedGroup = cachedGroup;
                UpdatedGroups.Remove(cachedGroup);
                _selectedIndexes.Clear();
                if (SelectedGroup.Id != 0)
                    _selectedIndexes.Add(SelectedGroup.Id);
            }
            else
            {
                // Если нет, запросить из БД
                var query = Database?.LayerGroups.Where(lg => lg.Prefix == prefix && lg.MainName.StartsWith(mainName));
                int? count = query?.Count();
                _selectedIndexes.Clear();
                if (count == 1 && query?.First().MainName == mainName)
                {
                    // Если по запросу ровно один результат - получить остальную информацию по группе и создать ViewModel
                    LayerGroupData result = query!.Include(lg => lg.Layers)
                                                  .ThenInclude(l => l.Zones)
                                                  .ThenInclude(z => z.ZoneLayer)
                                                  .First();
                    _selectedIndexes.Add(result.Id);

                    LayerGroupViewModel layerGroupViewModel = new(result, _db!);
                    AssignLayerGroupEvents(layerGroupViewModel);
                    SelectedGroup = layerGroupViewModel;
                }
                else if (count != 0)
                {
                    // Если по запросу более одного результата или один объект, но не в конце дерева - сохранить Id объектов (для возможности удаления целого узла)
                    _selectedIndexes = query!.Select(lg => lg.Id).ToHashSet();
                    // И обнулить выборы
                    SelectedGroup = null;
                    SelectedLayer = null;
                }
                else
                {
                    // Если ничего нет, просто обнулить все выборы
                    SelectedGroup = null;
                    SelectedLayer = null;
                }
            }
        }

        private void AssignLayerGroupEvents(LayerGroupViewModel layerGroupViewModel)
        {
            layerGroupViewModel.PropertyChanging += (s, e) =>
            {
                if (e.PropertyName == nameof(LayerGroupViewModel.Prefix) || e.PropertyName == nameof(LayerGroupViewModel.MainName))
                {
                    UpdatedGroups.Add((LayerGroupViewModel)s);
                    LayerGroupNames.Remove(((LayerGroupViewModel)s!).Name);
                }
            };
            layerGroupViewModel.PropertyChanged += (s, e) =>
            {
                OnPropertyChanged(nameof(IsUpdated));
                OnPropertyChanged(nameof(IsValid));
                if (e.PropertyName == nameof(LayerGroupViewModel.Prefix) || e.PropertyName == nameof(LayerGroupViewModel.MainName))
                    LayerGroupNames.Add(((LayerGroupViewModel)s!).Name);
            };
        }

        private void DeleteLayerGroups(object? obj)
        {
            int count = _selectedIndexes.Count;
            // Предупреждаем, если к удалению планируется более одной группы
            if (count > 1)
            {
                MessageBoxResult result = MessageBox.Show($"Узел содержит {count} групп слоёв. Действительно удалить узел целиком?",
                                                          "Внимание",
                                                          MessageBoxButton.YesNo,
                                                          MessageBoxImage.Warning);
                if (result != MessageBoxResult.Yes)
                    return;
            }
            // Добавляем в коллекцию Id для удаления
            foreach (var id in _selectedIndexes)
                GroupIdsToDelete.Add(id);

            var entitiesToRemove = Database!.LayerGroups.Where(g => _selectedIndexes.Contains(g.Id)).AsEnumerable();
            var namesToRemove = entitiesToRemove.Select(g => g.Name).ToList();
            var cachedToRemove = UpdatedGroups.Where(g => _selectedIndexes.Contains(g.Id)).AsEnumerable();

            // Если есть выбранная группа с Id==0 (не сохранённая в БД), добавить имя к удаляемым именам (для корректного обновления TreeView)
            if (SelectedGroup != null && SelectedGroup.Id == 0)
                namesToRemove.Add(SelectedGroup.Name);

            foreach (var entityToRemove in cachedToRemove)
                UpdatedGroups.Remove(entityToRemove);

            // Обнуляем выделение
            // (не позже, чем удаляем имена - иначе несохранённые удаляемые группы попадут в коллекцию UpdatedGroups, а из UI исчезнут)
            SelectedGroup = null;
            SelectedLayer = null;

            foreach (string name in namesToRemove)
                LayerGroupNames.Remove(name);

            OnPropertyChanged(nameof(IsUpdated));
            OnPropertyChanged(nameof(IsValid));
        }

        private void AddNewLayerGroup(object? obj)
        {
            string? groupName = obj as string;
            if (groupName == null)
                return;
            if (SelectedGroup?.IsUpdated ?? false)
                UpdatedGroups.Add(SelectedGroup);
            var newGroupVm = new LayerGroupViewModel(groupName, Database!);
            AssignLayerGroupEvents(newGroupVm);
            SelectedGroup = newGroupVm;
            LayerGroupNames.Add(groupName);
        }

        /// <inheritdoc/>
        public void UpdateDatabaseEntities()
        {
            if (Database == null)
                return;

            var entitiesToDelete = Database.LayerGroups.Where(g => GroupIdsToDelete.ExposedSet.Contains(g.Id)).AsEnumerable();
            Database.LayerGroups.RemoveRange(entitiesToDelete);
            GroupIdsToDelete.Clear();

            foreach (var groupVm in UpdatedGroups)
                groupVm.UpdateDatabaseEntities();
            UpdatedGroups.Clear();

            SelectedGroup?.UpdateDatabaseEntities();
        }

        /// <inheritdoc/>
        public void ResetValues()
        {
            ConnectCommand.Execute(_databasePath);
        }

        /// <summary>
        /// Сохранить БД как другой файл (SQLite)
        /// </summary>
        /// <param name="obj">Путь для сохранения файла</param>
        private void SaveDbAs(object? obj)
        {
            string outputFile;
            FileInfo sourceFile = new(_databasePath!);
            if (obj is string path)
            {
                outputFile = path;
            }
            else
            {
                var saveFileDialog = new SaveFileDialog()
                {
                    FileName = sourceFile.Name,
                    DefaultExt = ".db",
                    Filter = "SQLite database files (*.db)|*.db",
                    InitialDirectory = sourceFile.DirectoryName
                };
                bool? result = saveFileDialog.ShowDialog();
                if (result == true)
                {
                    outputFile = saveFileDialog.FileName;
                }
                else
                {
                    // Error message
                    return;
                }
            }

            if (IsUpdated)
            {
                var success = UpdateDatabaseViewModel(this);
                if (!success)
                {
                    return;
                }
            }

            try
            {
                sourceFile.CopyTo(outputFile, true);
            }
            catch (IOException ex)
            {
                // TODO: Log
                return;
            }
            _databasePath = outputFile;
            ConnectCommand.Execute(_databasePath);
        }

        /// <summary>
        /// Отменить подготовленные изменения в БД
        /// </summary>
        /// <param name="obj">Объект IDbRelatedViewModel, хранящий подготовленные изменения</param>
        private static void ResetDatabaseViewModel(object? obj)
        {
            if (obj is not IDbRelatedViewModel viewModel)
                return;
            viewModel.ResetValues();
        }

        /// <summary>
        /// Записать изменения в БД
        /// </summary>
        /// <param name="obj">Объект IDbRelatedViewModel, хранящий подготовленные изменения</param>
        /// <returns>Успех операции</returns>
        private bool UpdateDatabaseViewModel(object? obj)
        {
            if (obj is not IDbRelatedViewModel viewModel)
                return false;
            using (var transaction = Database!.Database.BeginTransaction())
            {
                try
                {
                    viewModel.UpdateDatabaseEntities();
                    Database!.SaveChanges();
                    transaction.Commit();
                }
                catch (DbUpdateException ex)
                {
                    // TODO: Log
                    transaction.Rollback();
                    MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
                catch (InvalidOperationException ex)
                {
                    transaction?.Rollback();
                    MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
            }
            OnPropertyChanged(nameof(IsUpdated));
            OnPropertyChanged(nameof(IsValid));
            return true;
        }

        /// <summary>
        /// Открыть редактор зон
        /// </summary>
        /// <param name="obj">Вызывающее окно (объект Window)</param>
        private void OpenZoneEditor(object? obj)
        {
            var window = obj as Window;
            if (window == null)
                return;
            var group = Database!.LayerGroups.SingleOrDefault(g => g.Id == SelectedGroup!.Id);
            if (group == null)
            {
                MessageBox.Show("Группа не сохранена в базе данных. Сохраните группу", "Несохранённая группа", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            ZoneEditorViewModel viewModel = new(group, Database);
            var zoneEditorWindow = new ZoneEditorWindow(viewModel);
            zoneEditorWindow.Owner = window;
            zoneEditorWindow.ShowDialog();
        }
    }
}