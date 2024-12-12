using Teigha.DatabaseServices;

namespace LoaderCore.NanocadUtilities
{
    public static class ObjectIdExtension
    {
        public static T GetObject<T>(this ObjectId id, OpenMode openMode, Transaction? transaction = null) where T : DBObject
        {
            var workTransaction = transaction ?? Workstation.TransactionManager.TopTransaction;
            return (T)workTransaction.GetObject(id, openMode);
        }
    }
}
