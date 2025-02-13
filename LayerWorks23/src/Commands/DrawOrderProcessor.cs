using LayerWorks.LayerProcessing;
using LoaderCore.NanocadUtilities;
using NameClassifiers;
using NPOI.SS.Formula.Functions;
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
                    // Если не выбраны объекты - пытаемся обработать все
                    if (!LayerWrapper.ActiveWrappers.Any(w => w is EntityLayerWrapper))
                    {
                        DrawOrderTable dot = (DrawOrderTable)Workstation.TransactionManager.TopTransaction.GetObject(Workstation.ModelSpace.DrawOrderTableId, OpenMode.ForWrite);
                        var entities = Workstation.ModelSpace.Cast<ObjectId>().Select(o => o.GetObject<Entity>(OpenMode.ForRead, transaction));
                        EntityLayerWrapper.CreateWrappers(entities, out var _);
                        var overlays = entities.Where(e => e.Layer.EndsWith("Калька")).Select(e => e.Id).ToArray();
                        if (overlays.Any())
                            dot.MoveToTop(new(overlays));
                        // убираем вниз все внешние ссылки
                        Func<DBObject, bool> isFromExternalReferencePredicate =
                            dbo => ((BlockTableRecord)transaction.GetObject(((BlockReference)dbo).BlockTableRecord, OpenMode.ForRead)).IsFromExternalReference;
                        var xRefsIds = Workstation.ModelSpace.Cast<ObjectId>()
                                                             .Select(id => transaction.GetObject(id, OpenMode.ForRead))
                                                             .Where(dbo => dbo is BlockReference)
                                                             .Where(isFromExternalReferencePredicate)
                                                             .Select(dbo => dbo.ObjectId)
                                                             .ToArray();
                        if (xRefsIds.Any())
                            dot.MoveToBottom(new(xRefsIds));
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
