

using HostMgd.ApplicationServices;
using HostMgd.EditorInput;
using Teigha.DatabaseServices;


namespace NanocadUtilities
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

}
