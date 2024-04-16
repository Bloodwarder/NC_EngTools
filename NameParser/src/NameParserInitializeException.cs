using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NameClassifiers
{
    internal class NameParserInitializeException : Exception
    {
        public NameParserInitializeException() : base() { }
        public NameParserInitializeException(string message) : base(message) { }
    }
}
