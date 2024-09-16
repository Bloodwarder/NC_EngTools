using System.IO;

namespace LoaderCore.Integrity
{
    // TODO: Создать систему поиска и обновления сборок
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
