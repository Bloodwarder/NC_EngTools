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
            IEnumerable<EntityLayerWrapper> wrappers = EntityLayerWrapper.CreateWrappers(entities, out var _);
            ArrangeDrawOrder(wrappers);
        }

        public void ArrangeDrawOrder(IEnumerable<EntityLayerWrapper> wrappers)
        {
            DrawOrderTable dot = (DrawOrderTable)Workstation.TransactionManager.TopTransaction.GetObject(Workstation.ModelSpace.DrawOrderTableId, OpenMode.ForWrite);
            var orderedWrappers = wrappers.OrderBy(w => _repository.Get(w.LayerInfo.TrueName).DrawOrderIndex);

            foreach (var wrapper in orderedWrappers)
            {
                var entities = wrapper.BoundEntities.Where(e => e is not MText and not MLeader and not DBText)
                                                    .Select(e => e.Id)
                                                    .ToArray();
                if (entities.Any())
                    dot.MoveToTop(new(entities));
            }
            foreach (var wrapper in orderedWrappers)
            {
                var entities = wrapper.BoundEntities.Where(e => e is MText or MLeader or DBText)
                                                    .Select(e => e.Id)
                                                    .ToArray();
                if (entities.Any())
                    dot.MoveToTop(new(entities));
            }
        }
    }
}
