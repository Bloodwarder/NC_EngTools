using LoaderCore.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace LoaderCore.Integrity
{
    internal static class FileUpdater
    {
        /// <summary>
        /// Сет с тегами обновляемых модулей
        /// </summary>
        internal static HashSet<string> UpdatedModules { get; } = new HashSet<string>() { "General" };

        internal static event EventHandler? FileUpdatedEvent;

        /// <summary>
        /// Является ли сборка отладочной (для отключения реального обновления в отладочной сборке)
        /// </summary>
        private static readonly bool _testRun = Assembly.GetExecutingAssembly().GetCustomAttributes(false).OfType<DebuggableAttribute>().Any(da => da.IsJITTrackingEnabled);

        internal static void UpdateFile(FileInfo local, FileInfo source)
        {
            bool localExists = local.Exists;
            bool sourceExists = source.Exists;
            // Если нет ни локального файла, ни источника, выдаем ошибку
            if (!localExists && !sourceExists)
            {
                throw new FileNotFoundException($"\nОтсутствует локальный файл {local.Name} и нет доступа к файлам обновления");
            }
            // Если доступен источник, сравниваем даты обновления и при необходимости перезаписываем локальный файл. Если нет - работаем с локальным без обновления
            if (sourceExists && (!localExists || local.LastWriteTime < source.LastWriteTime))
            {
                if (_testRun)
                {
                    LoggingRouter.WriteLog?.Invoke($"Отладочная сборка. Вывод сообщения об обновлении {local.Name}");
                    return;
                }
                source.CopyTo(local.FullName, true);
                FileUpdatedEvent?.Invoke(local, new EventArgs());
                LoggingRouter.WriteLog?.Invoke($"Файл {local.Name} обновлён");
                return;
            }
        }

        internal static void UpdateFile(ComparedFiles comparedFiles)
        {
            if (UpdatedModules.Contains(comparedFiles.ModuleTag))
                UpdateFile(comparedFiles.LocalFile, comparedFiles.SourceFile);
        }

        internal static void UpdateRange(IEnumerable<ComparedFiles> comparedFiles, string? singleTagUpdate = null)
        {
            // Если не задан конкретный тег - заранее фильтрует набор по нему и обновляет с пропуском стандартной проверки
            // Если задан - сразу передаёт в метод со стандартной проверкой на содержание тега в наборе обновляемых модулей
            if (singleTagUpdate == null)
            {
                foreach (ComparedFiles fileSet in comparedFiles)
                {
                    if (!fileSet.LocalFile.Directory!.Exists)
                        fileSet.LocalFile.Directory.Create();
                    UpdateFile(fileSet);
                }
            }
            else
            {
                foreach (ComparedFiles fileSet in comparedFiles.Where(fs => fs.ModuleTag == singleTagUpdate).ToList())
                    UpdateFile(fileSet.LocalFile, fileSet.SourceFile);
            }
        }
    }
}
