using LayersIO.DataTransfer;
using LoaderCore.Interfaces;
using LoaderCore.NanocadUtilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Teigha.Colors;
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
            var orderedWrappers = wrappers.OrderBy(w => _repository.TryGet(w.LayerInfo.TrueName, out var props) ? props!.DrawOrderIndex : 0);

            foreach (var wrapper in orderedWrappers)
            {
                // Обработать штриховки
                Color byLayerColor = Color.FromColorIndex(ColorMethod.ByLayer, 256);
                Hatch[] hatches = wrapper.BoundEntities.Where(e => e is Hatch)
                                                       .Cast<Hatch>()
                                                       .ToArray();
                // Сплошные перекрашенные
                ObjectId[] solidColoredHatches = hatches.Where(h => h.PatternName == "SOLID" && h.Color != byLayerColor)
                                                        .OrderByDescending(h => h.Color.Red + h.Color.Green + h.Color.Blue) // те что светлее - ниже
                                                        .Select(h => h.Id)
                                                        .ToArray();
                if (solidColoredHatches.Any())
                {
                    foreach (var id in solidColoredHatches)
                        dot.MoveToTop(new(new[] { id }));
                }

                // Сплошные с цветом - по слою
                ObjectId[] solidByLayerHatches = hatches.Where(h => h.PatternName == "SOLID" && h.Color == byLayerColor)
                                                        .Select(h => h.Id)
                                                        .ToArray();
                if (solidByLayerHatches.Any())
                    dot.MoveToTop(new(solidByLayerHatches));

                // Не сплошные
                var patternHatches = hatches.Where(h => h.PatternName != "SOLID").Select(h => h.Id).ToArray();
                if (patternHatches.Any())
                    dot.MoveToTop(new(patternHatches));

                // Обработать остальные не-текстовые объекты
                var entities = wrapper.BoundEntities.Where(e => e is not MText and not MLeader and not DBText and not Hatch)
                                                    .Select(e => e.Id)
                                                    .ToArray();
                if (entities.Any())
                    dot.MoveToTop(new(entities));
            }
            // Обработать текстовые объекты
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
