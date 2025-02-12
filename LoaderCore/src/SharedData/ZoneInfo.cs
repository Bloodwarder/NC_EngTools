
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace LoaderCore.SharedData
{
    public class ZoneInfo : ICloneable
    {
        public ZoneInfo(string zoneLayer, double value, double defaultWidth, string? filter, bool ignoreDefaultWidth, bool isSpecial)
        {
            ZoneLayer = zoneLayer;
            Value = value;
            DefaultConstructionWidth = defaultWidth;
            AdditionalFilter = filter;
            IgnoreConstructionWidth = ignoreDefaultWidth;
            IsSpecial = isSpecial;
        }

        public string ZoneLayer { get; set; }
        public string? AdditionalFilter { get; set; }
        public bool IgnoreConstructionWidth { get; set; }

        public double Value { get; set; }
        public double DefaultConstructionWidth { get; set; }
        public bool IsSpecial { get; set; }

        public object Clone() => MemberwiseClone();
    }

    public static class ZoneInfoAggregationExtension
    {
        public static IEnumerable<ZoneInfo> AggregateFilters(this IEnumerable<ZoneInfo> zones)
        {
            // TODO: тестировать, дорабатывать

            // Если особых правил нет, вернуть исходную коллекцию
            if (!zones.Any(z => z.IsSpecial))
                return zones;

            HashSet<ZoneInfo> set = zones.ToHashSet();
            var nameMatchGroups = zones.Select(z => new { Zone = z, Decomp = Decompose(z.AdditionalFilter ?? "") }).GroupBy(z => z.Zone.ZoneLayer);
            foreach (var group in nameMatchGroups)
            {
                // случай: filter1 + filter2 + ... + filter-n + cancel = remove-all
                if (group.Any(z => z.Zone.AdditionalFilter == "Cancel"))
                {
                    foreach (var z in group)
                        set.Remove(z.Zone);
                    continue;
                }
                // случай: no-filter + filter+cancel = ^filter
                foreach (var z in group.Where(z => z.Decomp.Length > 1 && z.Decomp[^1] == "Cancel"))
                {
                    foreach (var subZ in group.Where(zee => zee.Decomp.Length == 1 && z.Decomp.Any(s => s == zee.Decomp[1])))
                        set.Remove(subZ.Zone);
                }
                // случай no-filter + filter1+cancel + filter2+cancel = ^filter1;^filter2
                if (group.Any(z => !z.Decomp.Any()) && group.Any(z => z.Decomp.Length > 1 && z.Decomp[^1] == "Cancel"))
                {
                    var cancelZones = group.Where(z => z.Decomp.Length > 1 && z.Decomp[^1] == "Cancel");
                    string[] cancelFilters = cancelZones.SelectMany(z => z.Decomp).Where(s => s != "Cancel").Select(s => ReverseFilter(s)).ToArray();
                    foreach (var z in cancelZones)
                        set.Remove(z.Zone);
                    ZoneInfo targetZone = group.First(z => !z.Decomp.Any()).Zone;
                    ZoneInfo newZone = (ZoneInfo)targetZone.Clone();
                    set.Remove(targetZone);
                    newZone.AdditionalFilter = string.Join(";", cancelFilters);
                    set.Add(newZone);
                }
                // случай: non-special no-filter value1 + special filter value2 = ^filter value1 + filter value2
                if (group.Any(z => !z.Decomp.Any() && group.Any(z => z.Zone.IsSpecial && z.Decomp.Any() && z.Decomp[^1] != "Cancel")))
                {
                    ZoneInfo nonSpecialZone = group.First(z => !z.Decomp.Any()).Zone;
                    string[] specialFilters = group.Where(z => z.Zone.IsSpecial && z.Decomp.Any() && z.Decomp[^1] != "Cancel")
                                                   .SelectMany(z => z.Decomp)
                                                   .Select(s => ReverseFilter(s))
                                                   .ToArray();
                    ZoneInfo newZone = (ZoneInfo)nonSpecialZone.Clone();
                    newZone.AdditionalFilter= string.Join(";", specialFilters);
                    set.Add(newZone);
                    set.Remove(nonSpecialZone);
                }
            }
            var nameAndFilterMatchGroups = zones.GroupBy(z => new { z.ZoneLayer, z.AdditionalFilter });
            foreach (var group in nameAndFilterMatchGroups)
            {
                // случай: no-special value1 + specal value2 = special value2
                if (group.Any(z => z.IsSpecial))
                {
                    foreach (var z in group.Where(z => !z.IsSpecial))
                        set.Remove(z);
                }
            }
            // TODO: реализовать случай: filter1 + filter1;cancel + someZones = someZones
            return set;
        }

        private static string[] Decompose(string str) => str.Split(";");
        private static string ReverseFilter(string str) => str.StartsWith(@"^") ? str.Replace(@"^", "") : $"^{str}";
    }
}
