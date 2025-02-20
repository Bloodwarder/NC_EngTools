using System.Reflection;
using System.Xml.Linq;
using System.Linq;
using System.Security.AccessControl;
using System.Diagnostics;
using System.IO;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;

namespace NcetExternalUpdater
{
    internal class Program
    {
        private const string ConfigXmlName = "Configuration.xml";
        private const int MillisecondsTimeout = 5000;

        private static DirectoryInfo? _localDirectory;
        private static DirectoryInfo? _sourceDirectory;
        private static ManualUpdateWindow? _updateWindow;

        internal static event FileCheckedEventHandler? FileCheckedEvent;

        [STAThread]
        static void Main(string[] args)
        {
            FileCheckedEvent += HandleUpdateAction;

            _localDirectory = new FileInfo(Assembly.GetExecutingAssembly().Location).Directory!;
            XDocument doc = XDocument.Load(Path.Combine(_localDirectory.FullName, ConfigXmlName));
            string? updatePath = doc.Root?.Element("Directories")?.Element("UpdateDirectory")?.Value;
            if (updatePath == null)
                return;
            _sourceDirectory = new(updatePath);

            if (args.Length == 0)
            {
                _updateWindow = new(() =>
                {
                    bool success = UpdateAll();
                    if (success)
                    {
                        var finishTask = Task.Run(() => _updateWindow!.PushProgressBar());
                    }
                });
                int count = _sourceDirectory.GetFiles("*", SearchOption.AllDirectories).Count();
                _updateWindow.pbUpdateBar.Maximum = count + 10;
                _updateWindow.pbUpdateBar.SmallChange = 1;
                _updateWindow.ShowDialog();
            }
            else if (args.Length == 1 && args[0] == "-a")
            {
                Task.Delay(MillisecondsTimeout).Wait();
                UpdateAll();
            }
            else
            {
                Task.Delay(MillisecondsTimeout).Wait();
                UpdateDirectory(_localDirectory.FullName, _sourceDirectory.FullName, SearchOption.AllDirectories, args);
            }
        }

        private static bool UpdateAll()
        {
            if (!_sourceDirectory!.Exists)// || !_localDirectory!.GetFiles().All(f => GetAccessInfo(f)))
            {
                FileCheckedEvent?.Invoke(null, new(null, null, FileCheckState.Error, "Ошибка доступа к файлам"));
                return false;
            }
            UpdateDirectory(_localDirectory.FullName, _sourceDirectory.FullName, SearchOption.AllDirectories);
            return true;
        }

        private static bool GetAccessInfo(FileInfo fileInfo)
        {
            return fileInfo.GetAccessControl()
                           .GetAccessRules(true, true, typeof(System.Security.Principal.SecurityIdentifier))
                           .Cast<FileSystemAccessRule>()
                           .All(r => r.FileSystemRights == FileSystemRights.FullControl && r.AccessControlType == AccessControlType.Allow);
        }

        private static void UpdateDirectory(string localPath, string? sourcePath, SearchOption searchOption, params string[] filters)
        {
            if (string.IsNullOrEmpty(sourcePath))
            {
                string message = "Ошибка обновления - не указана директория источник обновлений";
                FileCheckedEvent?.Invoke(null, new(null, null, FileCheckState.Error, message));
                return;
            }
            if (searchOption == SearchOption.AllDirectories)
            {
                var directories = new DirectoryInfo(localPath).GetDirectories();
                foreach (var directory in directories)
                {
                    var source = Path.Combine(sourcePath, directory.Name);

                    if (Directory.Exists(source))
                        UpdateDirectory(directory.FullName, source, searchOption, filters);
                    else
                    {
                        FileCheckedEvent?.Invoke(null, new(null, null, FileCheckState.Deleted, $"Удаление директории {directory}"));
                    }

                }
            }

            var updateDir = new DirectoryInfo(sourcePath);
            var localDir = new DirectoryInfo(localPath);

            static Dictionary<string, FileInfo> GetDirectoryFiles(DirectoryInfo dir, string[] filters)
            {
                if (filters == null || !filters.Any())
                {
                    return dir.GetFiles("*", SearchOption.TopDirectoryOnly).ToDictionary(fi => fi.Name);
                }
                else
                {
                    List<FileInfo> list = new();
                    foreach (string filter in filters)
                        list.AddRange(dir.GetFiles(filter, SearchOption.TopDirectoryOnly));
                    return list.ToDictionary(fi => fi.Name);
                }
            }

            var updateDict = GetDirectoryFiles(updateDir, filters);
            var localDict = GetDirectoryFiles(localDir, filters);

            foreach (string file in updateDict.Keys)
            {
                try
                {
                    if (!localDict.ContainsKey(file))
                        FileCheckedEvent?.Invoke(null, new(new(updateDict[file].FullName.Replace(_sourceDirectory!.FullName, _localDirectory!.FullName)),
                                                           updateDict[file],
                                                           FileCheckState.Added));
                    if (localDict[file].LastWriteTime < updateDict[file].LastWriteTime)
                        FileCheckedEvent?.Invoke(null, new(localDict[file],
                                                           updateDict[file],
                                                           FileCheckState.Outdated));
                    else
                        FileCheckedEvent?.Invoke(null, new(localDict[file],
                                                           updateDict[file],
                                                           FileCheckState.Actual));
                }
                catch (Exception ex)
                {
                    FileCheckedEvent?.Invoke(null, new(new(updateDict[file].FullName.Replace(_sourceDirectory!.FullName, _localDirectory!.FullName)),
                                                       updateDict[file],
                                                       FileCheckState.Error,
                                                       $"Ошибка обновления файла {file}",
                                                       ex));
                    continue;
                }
            }

            foreach (string file in localDict.Keys)
            {
                if (!updateDict.ContainsKey(file))
                    try
                    {
                        FileCheckedEvent?.Invoke(null, new(localDict[file],
                                                           new(localDict[file].FullName.Replace(_localDirectory!.FullName, _sourceDirectory!.FullName)),
                                                           FileCheckState.Deleted));
                    }
                    catch (Exception ex)
                    {
                        FileCheckedEvent?.Invoke(null, new(localDict[file],
                                                       new(localDict[file].FullName.Replace(_localDirectory!.FullName, _sourceDirectory!.FullName)),
                                                       FileCheckState.Error,
                                                       "Ошибка удаления файла",
                                                       ex));
                        continue;
                    }
            }
        }



        internal static void HandleUpdateAction(object? sender, FileCheckedEventArgs e)
        {
            if (e.LocalPath?.Name.StartsWith("NcetExternalUpdater") ?? false)
                return;
#if !DEBUG
            switch (e.CheckState)
            {
                case FileCheckState.Actual:
                    break;
                case FileCheckState.Outdated:
                    e.SourcePath!.CopyTo(e.LocalPath!.FullName, true);
                    break;
                case FileCheckState.Deleted:
                    e.LocalPath!.Delete();
                    break;
                case FileCheckState.Added:
                    e.SourcePath!.CopyTo(e.LocalPath!.FullName);
                    break;
                case FileCheckState.Error:
                    break;
            }
#else
            switch (e.CheckState)
            {
                case FileCheckState.Actual:
                    Debug.WriteLine($"Файл {e.SourcePath!.Name} актуальной версии");
                    break;
                case FileCheckState.Outdated:
                    Debug.WriteLine($"Сообщение об обновлении файла {e.SourcePath!.Name}");
                    break;
                case FileCheckState.Deleted:
                    Debug.WriteLine(e.SourcePath is null ? $"Сообщение: {e.Message}" : $"Сообщение об удалении файла {e.SourcePath.Name}");
                    break;
                case FileCheckState.Added:
                    Debug.WriteLine($"Сообщение об копировании файла {e.SourcePath!.Name}");
                    break;
                case FileCheckState.Error:
                    Debug.WriteLine($"Сообщение об ошибке:{e.Message}\nИсключение: {e.Exception?.Message}");
                    break;
            }
#endif
        }
    }

    internal delegate void FileCheckedEventHandler(object? sender, FileCheckedEventArgs e);
}