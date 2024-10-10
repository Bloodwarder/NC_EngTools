using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using LoaderCore.Interfaces;
using LoaderCore.Utilities;
using Exception = System.Exception;
//using Microsoft.Extensions.Logging;

namespace LoaderCore.Integrity
{
    internal class ModuleHandler
    {
        private const string ModulesFolderName = "ExtensionLibraries";

        private Assembly? _mainAssembly = null;
        private string _mainAssemblyPath;
        private DirectoryInfo _moduleDirectory;
        private INcetInitializer? _initializer;
        internal ModuleHandler(string name)
        {
            Name = name;
            DllName = $"{name}.dll";
            var rootDirectory = NcetCore.RootLocalDirectory
                                ?? new FileInfo(Assembly.GetExecutingAssembly().Location).Directory!.Parent!.Parent!.FullName;
            var directoryPath = Path.Combine(rootDirectory, ModulesFolderName, Name);
            _moduleDirectory = new DirectoryInfo(directoryPath);
            _mainAssemblyPath = Path.Combine(directoryPath, DllName);
            if (!File.Exists(_mainAssemblyPath))
                throw new Exception("Не найдена основная сборка модуля");
        }



        internal string Name { get; }
        internal string DllName { get; }

        internal void Load()
        {
            LoadMainAssembly();
            LoadDependencies();
            RunInitializer();
        }

        internal void Update()
        {
            /// Вызовы логгера напрямую, так как в этом методе не должно быть зависимостей

            if (Assembly.GetExecutingAssembly().GetCustomAttributes(false).OfType<DebuggableAttribute>().Any(da => da.IsJITTrackingEnabled))
            {
                LoggingRouter.LogInformation?.Invoke("Отладочная сборка - обновление не производится");
                //NcetCore.Logger?.LogInformation("Отладочная сборка - обновление не производится");
                return;
            }
            var updateSourceDirectory = new DirectoryInfo(Path.Combine(NcetCore.RootUpdateDirectory ?? "", ModulesFolderName));
            if (updateSourceDirectory == null || !updateSourceDirectory.Exists)
            {
                string message = "Ошибка. Источник обновлений недоступен";
                Exception ex = new(message);
                LoggingRouter.LogInformation?.Invoke(message);
                //NcetCore.Logger?.LogCritical(ex, message);
                throw ex;
            }
            var sourceFilesDictionary = updateSourceDirectory.GetFiles("*", SearchOption.AllDirectories)
                                           .ToDictionary(f => f.Name);
            if (!_moduleDirectory.Exists)
                _moduleDirectory.Create();
            var localFilesDictionary = _moduleDirectory.GetFiles("*", SearchOption.AllDirectories)
                                           .ToDictionary(f => f.Name);
            bool updated = false;
            foreach (var sourceFile in sourceFilesDictionary.Values)
            {
                bool success = localFilesDictionary.TryGetValue(sourceFile.Name, out FileInfo? localFile);
                if (!success || localFile!.LastWriteTime < sourceFile.LastWriteTime)
                {
                    var newLocalFile = sourceFile.CopyTo(localFile?.FullName
                                                         ?? sourceFile.FullName.Replace(
                                                             NcetCore.RootUpdateDirectory!,
                                                             NcetCore.RootLocalDirectory),
                                                                true);
                    LoggingRouter.LogInformation?.Invoke($"Файл {newLocalFile.Name} обновлён");
                    //NcetCore.Logger?.LogInformation($"Файл {newLocalFile.Name} обновлён");
                    updated = true;
                }
            }
            foreach (var localFile in localFilesDictionary.Values)
            {
                if (!sourceFilesDictionary.ContainsKey(localFile.Name))
                    localFile.Delete();
                updated = true;
            }
            LoggingRouter.LogInformation?.Invoke(updated ? $"Модуль {Name} обновлён" : $"Модуль {Name} в актуальном состоянии");
            //NcetCore.Logger?.LogInformation(updated ? $"Модуль {Name} обновлён" : $"Модуль {Name} в актуальном состоянии");
        }

        internal bool CheckForUpdates()
        {
            var updateSourceDirectory = new DirectoryInfo(Path.Combine(NcetCore.RootUpdateDirectory, ModulesFolderName));
            if (!updateSourceDirectory.Exists)
                throw new Exception("Источник обновлений недоступен");
            var sourceFilesDictionary = updateSourceDirectory.GetFiles("*", SearchOption.AllDirectories)
                                           .ToDictionary(f => f.Name);
            if (!_moduleDirectory.Exists)
                _moduleDirectory.Create();
            var localFilesDictionary = _moduleDirectory.GetFiles("*", SearchOption.AllDirectories)
                                           .ToDictionary(f => f.Name);

            foreach (var sourceFile in sourceFilesDictionary.Values)
            {
                bool success = localFilesDictionary.TryGetValue(sourceFile.Name, out FileInfo? localFile);
                if (!success || localFile!.LastWriteTime < sourceFile.LastWriteTime)
                {
                    return true;
                }
            }
            foreach (var localFile in localFilesDictionary.Values)
            {
                if (!sourceFilesDictionary.ContainsKey(localFile.Name))
                    return true;
            }
            return false;
        }

        internal void Delete()
        {
            var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();
            bool isAssemblyLoaded = loadedAssemblies.Any(a => a.FullName == AssemblyName.GetAssemblyName(_mainAssemblyPath).FullName);
            if (isAssemblyLoaded)
            {
                throw new Exception("Невозможно удалить загруженный модуль");
            }
            _moduleDirectory.Delete(true);
            // Может вылететь исключение, но случай, когда сборка не загружена,
            // а что-то из остальных файлов модуля открыто, должен быть редким
            // Обработать выше
        }

        internal void PostInitialize()
        {
            _initializer?.PostInitialize();
        }

        private void LoadMainAssembly()
        {
            bool success = TryGetAssemblyName(_mainAssemblyPath, out var assemblyName);
            if (!success)
                throw new Exception("Основная сборка модуля повреждена");
            var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();
            bool isAssemblyLoaded = loadedAssemblies.Any(a => a.FullName == assemblyName!.FullName);
            if (isAssemblyLoaded)
            {
                _mainAssembly = loadedAssemblies.Where(a => a.FullName == assemblyName!.FullName).Single();
            }
            else
            {
                _mainAssembly = Assembly.LoadFrom(_mainAssemblyPath);
            }
            _initializer = GetInitializer();
        }
        private void LoadDependencies()
        {
            foreach (var file in _moduleDirectory.EnumerateFiles("*.dll", SearchOption.AllDirectories))
            {
                bool isValidAssembly = TryGetAssemblyName(file.FullName, out var name);
                bool isNotLoaded = AppDomain.CurrentDomain.GetAssemblies().Any(a => a.FullName == name?.FullName);
                if (isValidAssembly && isNotLoaded)
                    Assembly.LoadFrom(file.FullName);
            }
        }

        private void RunInitializer()
        {
            _initializer?.Initialize();
        }

        private INcetInitializer? GetInitializer()
        {
            Type targetType = typeof(INcetInitializer);
            Type? classType;
            try
            {
                classType = _mainAssembly!.GetTypes()
                                              .SingleOrDefault(t => t.GetCustomAttribute<NcetModuleInitializerAttribute>() != null
                                                                   && targetType.IsAssignableFrom(t));
            }
            catch (ReflectionTypeLoadException ex)
            {
                // Handle types that could not be loaded, including those from missing assemblies
                classType = ex.Types.Where(t => t != null).SingleOrDefault(t => t!.GetCustomAttribute<NcetModuleInitializerAttribute>() != null
                                                                   && targetType.IsAssignableFrom(t));
            }
            if (classType == null)
                return null;
            INcetInitializer? initializer = Activator.CreateInstance(classType) as INcetInitializer;
            if (initializer == null)
                throw new Exception($"Ошибка инициализации модуля {Name}"); // если нужного класса нет, нулл уже бы вернулся до этого
            return initializer;
        }

        private bool TryGetAssemblyName(string dllFilePath, out AssemblyName? assemblyName)
        {
            try
            {
                var name = AssemblyName.GetAssemblyName(dllFilePath);
                assemblyName = name;
                return true;
            }
            catch (BadImageFormatException)
            {
                assemblyName = null;
                return false;
            }
        }
    }
}
