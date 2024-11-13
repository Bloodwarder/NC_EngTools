

using HostMgd.ApplicationServices;
using HostMgd.EditorInput;
using Microsoft.Extensions.Logging;
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
        public static BlockTableRecord ModelSpace
        {
            get
            {
                var blockTable = (BlockTable)_transactionManager.TopTransaction.GetObject(Database.BlockTableId, OpenMode.ForWrite);
                return (BlockTableRecord)_transactionManager.TopTransaction.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
            }
        }

        public static ILogger? Logger { get; internal set; } = NcetCore.Logger;


        internal static bool IsCommandLoggingEnabled = false;


        /// <summary>
        /// Определяет основные элементы управления для открытого активного чертежа
        /// </summary>
        public static void Define()
        {
            _document = Application.DocumentManager.MdiActiveDocument;
            _database = HostApplicationServices.WorkingDatabase;
            _transactionManager = Database.TransactionManager;
            _editor = Document.Editor;
        }

        public static void SetLogger(ILogger logger)
        {
            Logger = logger;
        }
    }

}
