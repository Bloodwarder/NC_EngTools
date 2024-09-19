using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using LoaderCore;
using LoaderCore.Utilities;
using Exception = System.Exception;

namespace LoaderCore.Integrity
{
    internal class ModuleHandler
    {
        private const string ModulesFolderName = "ExtensionLibraries";

        private Assembly? _mainAssembly = null;
        private string _mainAssemblyPath;
        private DirectoryInfo _moduleDirectory;
        internal ModuleHandler(string name)
        {
            Name = name;
            DllName = $"{name}.dll";
            var directoryPath = Path.Combine(NcetCore.RootLocalDirectory, ModulesFolderName, Name);
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
        }


        internal void Update()
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
            bool updated = false;
            foreach (var sourceFile in sourceFilesDictionary.Values)
            {
                bool success = localFilesDictionary.TryGetValue(sourceFile.Name, out FileInfo? localFile);
                if (!success || localFile!.LastWriteTime < sourceFile.LastWriteTime)
                {
                    var newLocalFile = sourceFile.CopyTo(localFile?.FullName
                                                         ?? sourceFile.FullName.Replace(
                                                             NcetCore.RootUpdateDirectory,
                                                             NcetCore.RootLocalDirectory), 
                                                                true);
                    LoggingRouter.WriteLog?.Invoke($"Файл {newLocalFile.Name} обновлён ");
                    updated = true;
                }
            }
            foreach (var localFile in localFilesDictionary.Values)
            {
                if (!sourceFilesDictionary.ContainsKey(localFile.Name))
                    localFile.Delete();
                updated = true;
            }
            LoggingRouter.WriteLog?.Invoke(updated ? $"Модуль {Name} обновлён" : $"Модуль {Name} в актуальном состоянии");
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
