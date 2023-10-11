




using HostMgd.ApplicationServices;
using HostMgd.EditorInput;
using System;
using Teigha.DatabaseServices;


namespace Loader.CoreUtilities
{
    public static class Workstation
    {
        private static Document document;
        private static Database database;
        private static Teigha.DatabaseServices.TransactionManager transactionManager;
        private static Editor editor;


        public static Document Document => document;
        public static Database Database => database;
        public static Teigha.DatabaseServices.TransactionManager TransactionManager => transactionManager;
        public static Editor Editor => editor;

        public static void Define()
        {
            document = Application.DocumentManager.MdiActiveDocument;
            database = HostApplicationServices.WorkingDatabase;
            transactionManager = Database.TransactionManager;
            editor = Document.Editor;
        }
    }

    public class DBObjectWrapper<T> : IDisposable where T : DBObject
    {
        private readonly ObjectId _id;
        private readonly OpenMode _openMode;
        private readonly T _object;
        private Getter _getHandler;

        public DBObjectWrapper(ObjectId id, OpenMode openMode)
        {
            _id = id;
            _openMode = openMode;
            _object = OpenAndGet();
            _object.ObjectClosed += ObjectClosedHandler;
        }

        public DBObjectWrapper(T obj, OpenMode openMode)
        {
            _object = obj;
            _id = obj.ObjectId;
            _openMode = openMode;
            _getHandler = DirectGet;
            _object.ObjectClosed += ObjectClosedHandler;
        }

        public T Get()
        {
            return _getHandler.Invoke();
        }

        private T DirectGet()
        {
            return _object;
        }

        private T OpenAndGet()
        {
            try
            {
                T obj = Workstation.TransactionManager.TopTransaction.GetObject(_id, _openMode) as T;
                _getHandler = DirectGet;
                return obj;
            }
            catch
            {
                _getHandler = OpenAndGet;
                return null;
            }
        }

        private delegate T Getter();

        private void ObjectClosedHandler(object sender, ObjectClosedEventArgs e)
        {
            _getHandler = OpenAndGet;
        }

        public void Dispose()
        {
            _object.Dispose();
        }
    }

}
