using Microsoft.Extensions.Logging;
using System;

namespace LoaderCore.Utilities
{
    public static class PreInitializeSimpleLogger
    {
        public static Action<string>? Log { get; set; }

        public static void RegisterLogMethod(Action<string> action)
        {
            Log += action;
        }

        public static void ClearLoggers()
        {
            Log = null;
        }
    }
}
