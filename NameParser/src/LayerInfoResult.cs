using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NameClassifiers
{
    public class LayerInfoResult
    {
        internal LayerInfoResult(string failureMessage)
        {
            Status = LayerInfoParseStatus.Failure;
            Exceptions.Add(new NameParserInitializeException(failureMessage));
            Value = null;
        }
        internal LayerInfoResult(LayerInfo layerInfo)
        {
            Value = layerInfo;
        }

        public LayerInfoParseStatus Status { get; internal set; }
        public LayerInfo? Value { get; private set; }

        internal List<Exception> Exceptions { get; } = new();

        public Exception[] GetExceptions() => Exceptions.ToArray();
    }

    public enum LayerInfoParseStatus
    {
        NotProcessed = 0,
        Success = 1,
        PartialFailure = 2,
        Failure = 3
    }
}
