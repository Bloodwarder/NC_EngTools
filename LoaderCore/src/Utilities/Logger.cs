using System.Threading.Tasks;

namespace LoaderCore.Utilities
{
    public static class Logger
    {
        public static Log? WriteLog { get; set; }

        public delegate void Log(string message);
    }
}
