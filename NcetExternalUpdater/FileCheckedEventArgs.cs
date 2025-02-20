using System.IO;

namespace NcetExternalUpdater
{
    internal class FileCheckedEventArgs : EventArgs
    {
        internal FileCheckedEventArgs(FileInfo? localFile, FileInfo? sourceFile, FileCheckState checkState)
        {
            SourcePath = sourceFile;
            LocalPath = localFile;
            CheckState = checkState;
        }

        internal FileCheckedEventArgs(FileInfo? localFile, FileInfo? sourceFile, FileCheckState checkState, string? message = null, Exception? ex = null)
            : this(localFile, sourceFile, checkState)
        {
            Exception = ex;
            Message = message;
        }

        public FileInfo? SourcePath { get; init; }
        public FileInfo? LocalPath { get; init; }
        public FileCheckState CheckState { get; init; }
        public Exception? Exception { get; init; }
        public string? Message { get; init; }
    }
}