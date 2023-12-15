using System.IO;

namespace Loader.Integrity
{
    internal struct ComparedFiles
    {
        internal FileInfo LocalFile;
        internal FileInfo SourceFile;
        internal string ModuleTag;

        internal ComparedFiles(FileInfo local, FileInfo source, string moduleTag)
        {
            LocalFile = local;
            SourceFile = source;
            ModuleTag = moduleTag;
        }
    }
}
