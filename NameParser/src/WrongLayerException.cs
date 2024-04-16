using System;

namespace NameClassifiers
{
    public class WrongLayerException : Exception
    {
        public WrongLayerException(string message) : base(message) { }
    }
}


