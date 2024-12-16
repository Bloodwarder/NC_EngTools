using System.Collections.Generic;
using Teigha.DatabaseServices;

namespace LoaderCore.Interfaces
{
    public interface IBuffer
    {
        public IEnumerable<Polyline> Buffer(IEnumerable<Entity> entities, double width);
        public IEnumerable<Polyline> Buffer(IEnumerable<Entity> entities, double width, string layerName);
        public IEnumerable<Polyline> Buffer(IEnumerable<Entity> entities, Dictionary<string, double> width, string layerName);
    }
}
