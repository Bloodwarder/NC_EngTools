using LayersIO.Connection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.IO;

namespace LayerDbMigrations
{
    internal class Program
    {
        private const string TestEmptyDatabasePath = @"C:\Users\konovalove\source\repos\Bloodwarder\NC_EngTools\LayersDatabase\Data\LayerData_Empty.db";

        static Dictionary<string, Action> CommandsDictionary { get; } = new()
        {
            ["pr"] = CreatePrototype,
            ["restore"] = RestoreBackUp,
            ["bk"] = CreateBackUp,
            ["push"] = PushToProduction,
            ["pull"] = PullToPrototypingProject
        };

        static void Main(string[] args)
        {
            Console.WriteLine("Введите команду");
            while (true)
            {
                Console.WriteLine("\nВведите команду:");
                string command = Console.ReadLine() ?? "";
                bool success = CommandsDictionary.TryGetValue(command, out var action);
                if (success)
                {
                    action!();
                }
                else if (command!.ToLower() == "quit" || command! == string.Empty)
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
            FileInfo fileInfo = new FileInfo(TestEmptyDatabasePath);
            try
            {
                using (OverwriteLayersDatabaseContextSqlite context = new(TestEmptyDatabasePath, null))
                {
                    Console.WriteLine($"Прототип базы данных создан\nПолный путь - {fileInfo.FullName}");
                }
                fileInfo.OpenFolder();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        private static void CreateBackUp()
        {
            throw new NotImplementedException();
        }

        private static void RestoreBackUp()
        {
            throw new NotImplementedException();
        }

        private static void PushToProduction()
        {
            throw new NotImplementedException();
        }

        private static void PullToPrototypingProject()
        {
            throw new NotImplementedException();
        }
    }
}