using LoaderCore.NanocadUtilities;
using Teigha.DatabaseServices;

namespace Utilities
{
    internal static class BlockReferenceNameExtension
    {
        internal static string BlockTableRecordName(this BlockReference bref)
        {
            if (bref.IsDynamicBlock)
            {
                BlockTableRecord btr = (BlockTableRecord)Workstation.TransactionManager.TopTransaction.GetObject(bref.DynamicBlockTableRecord, OpenMode.ForRead);
                return btr!.Name;
            }
            else
            {
                BlockTableRecord btr = (BlockTableRecord)Workstation.TransactionManager.TopTransaction.GetObject(bref.BlockTableRecord, OpenMode.ForRead);
                return btr!.Name;
            }
        }
    }

}
