using System.Diagnostics;

namespace LayerDbMigrations
{
    public static class FileInfoExtension
    {
        public static void OpenFolder(this FileInfo file) => Process.Start("explorer.exe", file.DirectoryName!);
    }
}