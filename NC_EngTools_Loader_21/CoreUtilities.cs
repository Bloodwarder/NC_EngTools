﻿




using HostMgd.ApplicationServices;
using HostMgd.EditorInput;
using System;
using Teigha.DatabaseServices;


namespace Loader.CoreUtilities
{
    /// <summary>
    /// Предоставляет централизованный доступ к основным элементам управления nanoCAD: Document, Database, TransactionManager, Editor
    /// </summary>
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

        /// <summary>
        /// Определяет основные элементы управления для открытого активного чертежа
        /// </summary>
        public static void Define()
        {
            document = Application.DocumentManager.MdiActiveDocument;
            database = HostApplicationServices.WorkingDatabase;
            transactionManager = Database.TransactionManager;
            editor = Document.Editor;
        }
    }

    /// <summary>
    /// Предоставляет доступ к объекту базы данных чертежа, если необходимо, чтобы объект запоминался между транзакциями
    /// </summary>
    /// <typeparam name="T">Объект базы данных чертежа DBObject</typeparam>
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

        /// <inheritdoc/>
        public void Dispose()
        {
            _object.Dispose();
        }
    }

}
