using System;
using System.Linq;
using Teigha.DatabaseServices;

namespace LoaderCore.NanocadUtilities
{
    public static class BlockTableRecordExtensions
    {
        public static string BlockTableRecordName(this BlockTableRecord btr)
        {
            if (btr.IsDynamicBlock)
            {
                TypedValue[] vals = btr.XData.AsArray();
                var val = vals.Where(v => v.TypeCode == 1001 && ((string)v.Value).StartsWith("AcDbDynamicBlockTrueName")).First();
                int index = Array.IndexOf(vals, val) + 1;
                return (string)vals[index].Value;
            }
            else
            {
                return btr.Name;
            }
        }
    }
}
