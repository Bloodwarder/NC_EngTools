using System.IO;

namespace LoaderCore.Interfaces
{
    public interface IFilePathProvider
    {
        public string GetPath(string fileName);

        public bool TryGetPath(string fileName, out string? filePath);

        public FileInfo GetFileInfo(string fileName);

        public bool TryGetFileInfo(string fileName, out FileInfo? file);
    }
}
