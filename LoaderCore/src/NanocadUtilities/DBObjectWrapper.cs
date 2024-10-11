using System;
using Teigha.DatabaseServices;


namespace LoaderCore.NanocadUtilities
{

    /// <summary>
    /// Предоставляет доступ к объекту базы данных чертежа, если необходимо, чтобы объект запоминался между транзакциями
    /// </summary>
    /// <typeparam name="T">Объект базы данных чертежа DBObject</typeparam>
    public class DBObjectWrapper<T> : IDisposable where T : DBObject
    {
        private readonly ObjectId _id;
        private readonly OpenMode _openMode;
        private readonly T _object;
        private Getter _getHandler = null!;

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

        /// <summary>
        /// Получить объект из обёртки. В зависимости от предыдущих действий получает его напрямую или добавляет в текущую транзакцию по ObjectId
        /// </summary>
        /// <returns>Объект</returns>
        public T Get()
        {
            return _getHandler.Invoke();
        }

        private T DirectGet()
        {
            bool isAccessable = _openMode == OpenMode.ForWrite ? _object.IsWriteEnabled : _object.IsReadEnabled;
            return isAccessable? _object : OpenAndGet();
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

        /// <inheritdoc/>
        public void Dispose()
        {
            _object.Dispose();
        }
    }

}
