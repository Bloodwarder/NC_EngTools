using LayerWorks.LayerProcessing;
using LoaderCore.NanocadUtilities;
using NameClassifiers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Teigha.DatabaseServices;

namespace LayerWorks.Commands
{
    internal class DrawOrderProcessor
    {
        private readonly DrawOrderService _drawOrderService;
        public DrawOrderProcessor(DrawOrderService drawOrderService)
        {
            _drawOrderService = drawOrderService;
        }

        public void ArrangeEntities()
        {
            try
            {
                using (var transaction = Workstation.TransactionManager.StartTransaction())
                {
                    SelectionHandler.UpdateActiveLayerWrappers();
                    if (!LayerWrapper.ActiveWrappers.Any(w => w is EntityLayerWrapper))
                    {
                        var entities = Workstation.ModelSpace.Cast<Entity>();
                        EntityLayerWrapper.CreateWrappers(entities);
                        DrawOrderTable dot = (DrawOrderTable)Workstation.TransactionManager.TopTransaction.GetObject(Workstation.ModelSpace.DrawOrderTableId, OpenMode.ForWrite);
                        dot.MoveToTop(new(entities.Where(e => e.Layer.EndsWith("Калька")).Select(e => e.Id).ToArray()));
                    }
                    var wrappers = LayerWrapper.ActiveWrappers.Where(w => w is EntityLayerWrapper).Cast<EntityLayerWrapper>();
                    _drawOrderService.ArrangeDrawOrder(wrappers);
                    transaction.Commit();
                }
            }
            finally
            {
                LayerWrapper.ActiveWrappers.Clear();
            }
        }

    }
}
