using Teigha.DatabaseServices;

namespace LoaderCore.NanocadUtilities
{
    public static class BlockReferenceNameExtension
    {
        public static string BlockTableRecordName(this BlockReference bref)
        {
            if (bref.IsDynamicBlock)
            {
                BlockTableRecord btr = (BlockTableRecord)Workstation.TransactionManager.TopTransaction.GetObject(bref.DynamicBlockTableRecord, OpenMode.ForRead);
                return btr.Name;
            }
            else
            {
                BlockTableRecord btr = (BlockTableRecord)Workstation.TransactionManager.TopTransaction.GetObject(bref.BlockTableRecord, OpenMode.ForRead);
                return btr.Name;
            }
        }
    }
}
