//System
//Microsoft
// Nanocad
//Internal
//NTS
using NetTopologySuite.Operation.Buffer;

using NtsBufferOps = NetTopologySuite.Operation.Buffer;
using System;

namespace GeoMod.Commands
{
    internal class DynamicRoundBufferParametersProvider
    {
        private const int MinimalQudrantSegments = 4;
        readonly double _minimalbufferWidth;
        readonly int _minimalQSegments;
        readonly double _maxCalculatedWidth;
        readonly int _maxQSegments;
        readonly double _calculatedWidthInterval;
        readonly int _calculatedSegmentsInterval;

        public DynamicRoundBufferParametersProvider(double minimalbufferWidth, int minimalQSegments, double maxCalculatedWidth, int maxQSegments)
        {
            _maxCalculatedWidth = maxCalculatedWidth;
            _minimalbufferWidth = minimalbufferWidth;
            _minimalQSegments = Math.Max(minimalQSegments, MinimalQudrantSegments);
            _maxQSegments = Math.Min(maxQSegments, MinimalQudrantSegments);

            _calculatedSegmentsInterval = _maxQSegments - _minimalQSegments;
            _calculatedWidthInterval = _maxCalculatedWidth - _minimalbufferWidth;
        }

        public BufferParameters GetBufferParameters(double bufferWidth)
        {
            int qSegments;
            if (bufferWidth > _minimalbufferWidth && bufferWidth < _maxCalculatedWidth)
            {
                var segmentsIncrement = (bufferWidth - _minimalbufferWidth) / _calculatedWidthInterval * _calculatedSegmentsInterval;
                qSegments = MinimalQudrantSegments + (int)Math.Floor(segmentsIncrement);
            }
            else if (bufferWidth <= _minimalbufferWidth)
            {
                qSegments = _minimalQSegments;
            }
            else
            {
                qSegments = _maxQSegments;
            }
            BufferParameters bufferParameters = new()
            {
                EndCapStyle = EndCapStyle.Round,
                JoinStyle = NtsBufferOps.JoinStyle.Round,
                QuadrantSegments = qSegments,
                SimplifyFactor = 0.02d,
                IsSingleSided = false
            };
            return bufferParameters;
        }
    }
}
