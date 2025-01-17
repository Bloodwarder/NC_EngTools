using LayersIO.DataTransfer;
using LoaderCore.Interfaces;
using LoaderCore.NanocadUtilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Teigha.DatabaseServices;

namespace LayerWorks.LayerProcessing
{
    internal class DrawOrderService
    {
        IRepository<string, LayerProps> _repository;
        public DrawOrderService(IRepository<string, LayerProps> repository)
        {
            _repository = repository;
        }

        public void ArrangeDrawOrder(IEnumerable<Entity> entities)
        {
            IEnumerable<EntityLayerWrapper> wrappers = EntityLayerWrapper.CreateWrappers(entities);
            ArrangeDrawOrder(wrappers);
        }

        public void ArrangeDrawOrder(IEnumerable<EntityLayerWrapper> wrappers)
        {
            
            DrawOrderTable dot = (DrawOrderTable)Workstation.TransactionManager.TopTransaction.GetObject(Workstation.ModelSpace.DrawOrderTableId, OpenMode.ForWrite);
            var orderedWrappers = wrappers.OrderBy(w => _repository.Get(w.LayerInfo.TrueName).DrawOrderIndex);
            foreach (var wrapper in orderedWrappers)
            {
                dot.MoveToTop(new(wrapper.BoundEntities.Select(e => e.Id).ToArray()));
            }
        }
    }
}
