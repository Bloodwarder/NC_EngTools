using LayersIO.Connection;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npoi.Mapper;

namespace LayerDbMigrations
{
    internal class Program
    {
        private const string ProductionPath = @"C:\Users\konovalove\source\repos\Bloodwarder\NC_EngTools\LayersIO\Data\";
        private const string WorkPath = @"C:\Users\konovalove\source\repos\Bloodwarder\NC_EngTools\LayerDbMigrations\bin\Debug\Data";
        private const string SharedPath = @"\\COMP010\Data\364\пыщь\ncet_shared_files";

        private const string PrototypeFilename = "LayerData_Prototype.db";
        private const string TestFilename = "LayerData_Testing.db";
        private const string ProductionFilename = "LayerData.db";
        private const string BackUpFilename = "LayerData_Backup.db";

        static Dictionary<string, Action> CommandsDictionary { get; } = new()
        {
            ["prototype"] = CreatePrototype,
            ["restore"] = RestoreBackUp,
            ["backup"] = CreateBackUp,
            ["push"] = PushToProduction,
            ["pull"] = PullToPrototypingProject,
            ["datatransfer"] = DataTransfer,
            ["migrate"] = ApplyMigrations,
            ["prod"] = ApplyMigrationsSharedDb
        };

        private static void ApplyMigrations()
        {
            using (LayersDatabaseContextSqlite db = new(Path.Combine(WorkPath, TestFilename), null))
            {
                var migrator = db.Database.GetService<IMigrator>();
                migrator.Migrate();
            }
        }

        private static void ApplyMigrationsSharedDb()
        {
            using (LayersDatabaseContextSqlite db = new(Path.Combine(SharedPath, ProductionFilename), null))
            {
                var migrator = db.Database.GetService<IMigrator>();
                migrator.Migrate();
            }
        }

        private static void DataTransfer()
        {
            throw new NotImplementedException();
            //using (LayersDatabaseContextSqlite context = new(Path.Combine(WorkPath,TestFilename), null))
            //{
            //    context.LayerGroups.ForEach(g => g.Prefix = "ИС");
            //    context.Layers.ForEach(l => l.Prefix = "ИС");
            //    context.SaveChanges();
            //}
        }

        static void Main(string[] args)
        {
            if (args.Any(arg => arg == "-p"))
            {
                ApplyMigrationsSharedDb();
                return;
            }
            Console.WriteLine("Инструкция:");
            Console.WriteLine("1.\tpull - копировать основную базу в рабочую папку (также создаёт бэкап)");
            Console.WriteLine("2.\tИзменить модель");
            Console.WriteLine("3.\tprototype - создать пустой прототип базы и проверить его");
            Console.WriteLine("4.\tЕсли прототип в порядке, создаём миграции с помощью package manager console");
            Console.WriteLine("\tAdd-Migration [Имя миграции] -Context LayersDatabaseContextSqlite");
            Console.WriteLine("\tUpdate-Database -Context LayersDatabaseContextSqlite или здесь команда migrate");
            Console.WriteLine("5.\tЕсли результат не соответствует ожиданиям - restore (меняет рабочий файл базы на файл бэкапа");
            Console.WriteLine("6.\tpush - заменить основной файл базы рабочим файлом");
            while (true)
            {
                Console.WriteLine("\nВведите команду:");
                string commandName = Console.ReadLine() ?? "";
                bool success = CommandsDictionary.TryGetValue(commandName, out var action);
                if (success)
                {
                    try
                    {
                        action!();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Ошибка выполнения команды: {ex.Message}");
                    }
                }
                else if (commandName!.ToLower() == "quit" || commandName! == string.Empty)
                {
                    break;
                }
                else
                {
                    Console.WriteLine("Неверная команда\n");
                }
            }
        }

        private static void CreatePrototype()
        {
            FileInfo fileInfo = new(Path.Combine(WorkPath, PrototypeFilename));
            using (PrototypeLayersDatabaseContextSqlite context = new(Path.Combine(WorkPath, PrototypeFilename), null))
            {
                Console.WriteLine($"Прототип базы данных создан\nПолный путь - {fileInfo.FullName}");
            }
            fileInfo.OpenFolder();
        }
        private static void CreateBackUp()
        {
            FileInfo testFile = new(Path.Combine(WorkPath, TestFilename));
            FileInfo backupFile = new(Path.Combine(WorkPath, BackUpFilename));
            testFile.CopyTo(backupFile.FullName, true);
        }

        private static void RestoreBackUp()
        {
            FileInfo testFile = new(Path.Combine(WorkPath, TestFilename));
            FileInfo backupFile = new(Path.Combine(WorkPath, BackUpFilename));
            backupFile.CopyTo(testFile.FullName, true);
        }

        private static void PushToProduction()
        {
            FileInfo targetFile = new(Path.Combine(ProductionPath, ProductionFilename));
            FileInfo sourceFile = new(Path.Combine(WorkPath, TestFilename));
            sourceFile.CopyTo(targetFile.FullName, true);
            targetFile.OpenFolder();
        }

        private static void PullToPrototypingProject()
        {
            FileInfo sourceFile = new(Path.Combine(ProductionPath, ProductionFilename));
            FileInfo targetFile = new(Path.Combine(WorkPath, TestFilename));
            FileInfo backupFile = new(Path.Combine(WorkPath, BackUpFilename));
            sourceFile.CopyTo(targetFile.FullName, true);
            sourceFile.CopyTo(backupFile.FullName, true);
            targetFile.OpenFolder();
        }
    }
}