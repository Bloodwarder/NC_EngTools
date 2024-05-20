using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Xml.Serialization;
using NameClassifiers;
using NameClassifiers.Filters;
using NameClassifiers.References;
using Teigha.DatabaseServices.Filters;
using Teigha.GraphicsInterface;

namespace LayerWorks.Legend
{
    internal static partial class GlobalFiltersExtension
    {
        /// <summary>
        /// Получить массивы ключевых слов для выбора фильтров по каждой секции фильтров
        /// </summary>
        /// <param name="globalFilters"></param>
        /// <returns></returns>
        internal static string[][]? GetFilterKeyWords(this GlobalFilters globalFilters, out int sectionsCount)
        {
            if (globalFilters.Sections == null)
            {
                sectionsCount = 0;
                return null;
            }
            sectionsCount = globalFilters.Sections.Length;
            string[][] result = new string[sectionsCount][];
            for (int i = 0; i < sectionsCount - 1; i++)
            {
                string[] filterKeywords = globalFilters.Sections[i].Filters.Select(f => f.Name).ToArray();
                result[i] = filterKeywords;
            }
            return result;
        }

        internal static IEnumerable<GridData> GetGridData(this GlobalFilters filter, string[] filterKeywords)
        {
            // Если секций нет - возвращаем сетку с дефолтным наименованием и без фильтрации
            if (filter.Sections == null)
            {
                var gd = new GridData() { GridName = filter.DefaultLabel ?? "ERROR", Predicate = c => true };
                return new GridData[] { gd };
            }

            // Сравниваем количество секций и переданных ключевых слов
            if (!ValidateKeywordsArray(filter, filterKeywords))
                throw new ArgumentException("Неверный размер массива ключевых слов");

            // Инициализируем вспомогательные данные
            int sectionsNumber = filter.Sections.Length;
            LayerInfo cellToInfo(LegendGridCell c) => c.Layer.LayerInfo;
            GridData[][] gridData = new GridData[sectionsNumber][];

            // Заполняем массивы для сеток
            for (int i = 0; i < sectionsNumber; i++)
            {
                // Выбираем в секции фильтр по имени, переданном в наборе ключевых слов
                LegendFilter legendFilter = filter.Sections![i].Filters.Where(f => f.Name == filterKeywords[i]).First();
                // Создаём список фильтров сетки для каждой секции
                List<GridFilter> fullGridList = new();
                foreach (var grid in legendFilter.Grids)
                {
                    // Проверяем для каждой сетке, есть ли у неё Distinct-режим - если да, то создаст по одной сетке для каждого присутствующего классификатора
                    // Если нет - вернёт одну эту же сетку
                    fullGridList.AddRange(grid.CheckAndUnfoldDistinctMode());
                }
                int gridsNumber = fullGridList.Count;
                gridData[i] = new GridData[gridsNumber];
                for (int j = 0; j < gridsNumber - 1; j++)
                {
                    GridFilter gridFilter = fullGridList[j];
                    Func<LayerInfo, bool> predicate = gridFilter.GetGridPredicate();
                    gridData[i][j].Predicate = c => predicate(cellToInfo(c));
                    gridData[i][j].GridName = gridFilter.Label ?? "*";
                }
            }
            return gridData.Aggregate<IEnumerable<GridData>>((d1, d2) => filter.MultiplyGridDataArrays(d1, d2));
        }

        private static bool ValidateKeywordsArray(GlobalFilters filters, string[] filterKeywords) => (filters.Sections?.Length ?? 0) == filterKeywords.Length;

        private static string ProcessName(string? nameString, string? defaultName) => nameString?.Replace("*", defaultName ?? "ERROR") ?? defaultName ?? "ERROR";

        private static IEnumerable<GridData> MultiplyGridDataArrays(this GlobalFilters filter, IEnumerable<GridData> data1, IEnumerable<GridData> data2)
        {
            foreach (var item1 in data1)
            {
                foreach (var item2 in data2)
                {
                    GridData resultData = new GridData();
                    resultData.Predicate = c => item1.Predicate(c) && item2.Predicate(c);
                    resultData.GridName = ProcessName(item1.GridName, item2.GridName ?? filter.DefaultLabel);
                    yield return resultData;
                }
            }
        }
    }
}
