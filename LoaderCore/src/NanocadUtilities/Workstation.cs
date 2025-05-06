

using HostMgd.ApplicationServices;
using HostMgd.EditorInput;
using Microsoft.Extensions.Logging;
using System;
using Teigha.DatabaseServices;


namespace LoaderCore.NanocadUtilities
{
    /// <summary>
    /// Предоставляет централизованный доступ к основным элементам управления nanoCAD: Document, Database, TransactionManager, Editor
    /// </summary>
    public static class Workstation
    {
        private static Document _document = null!;
        private static Database _database = null!;
        private static Teigha.DatabaseServices.TransactionManager _transactionManager = null!;
        private static Editor _editor = null!;

        static Workstation()
        {
            Define();
        }

        public static Document Document => _document;
        public static Database Database => _database;
        public static Teigha.DatabaseServices.TransactionManager TransactionManager => _transactionManager;
        public static Editor Editor => _editor;
        public static BlockTable BlockTable => (BlockTable)_transactionManager.TopTransaction.GetObject(Database.BlockTableId, OpenMode.ForWrite);
        public static BlockTableRecord ModelSpace =>
            (BlockTableRecord)_transactionManager.TopTransaction.GetObject(BlockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite);



        public static ILogger? Logger { get; internal set; } = NcetCore.Logger;


        internal static bool IsCommandLoggingEnabled { get; set; } = false;

        public static event EventHandler? BeginRedefine;
        public static event WorkstationRedefinedEventHandler? Redefined;
        /// <summary>
        /// Определяет основные элементы управления для открытого активного чертежа
        /// </summary>
        public static void Define()
        {
            BeginRedefine?.Invoke(_document, EventArgs.Empty);

            var document = Application.DocumentManager.MdiActiveDocument;
            var args = new WorkstationRedefinedEventArgs(_document, document);

            _document = document;
            _database = HostApplicationServices.WorkingDatabase;
            _transactionManager = Database.TransactionManager;
            _editor = Document.Editor;
            try
            {
                Redefined?.Invoke(_document, args);
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Ошибка обработки перезагрузки рабочей станции. Программа попытается продолжить работу.\nСообщение:\t{Message}", ex.Message);
            }
        }

        public static void AppendEntity(Entity entity, Transaction transaction)
        {
            ModelSpace.AppendEntity(entity);
            transaction.AddNewlyCreatedDBObject(entity, true);
        }

        internal static void SetLogger(ILogger logger)
        {
            Logger = logger;
        }
    }

    public class WorkstationRedefinedEventArgs : EventArgs
    {
        public WorkstationRedefinedEventArgs(Document oldDocument, Document newDocument)
        {
            OldDocument = oldDocument;
            NewDocument = newDocument;
            DocumentChanged = oldDocument != newDocument;
        }

        public Document OldDocument { get; }
        public Document NewDocument { get; }
        public bool DocumentChanged { get; }
    }

    public delegate void WorkstationRedefinedEventHandler(object sender, WorkstationRedefinedEventArgs e);
}
