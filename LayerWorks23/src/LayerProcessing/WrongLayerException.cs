using System;

namespace LayerWorks.LayerProcessing
{
    internal class WrongLayerException : Exception
    {
        public WrongLayerException(string message) : base(message) { }
    }
}


