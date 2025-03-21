using HostMgd.EditorInput;
using LoaderCore.NanocadUtilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Teigha.Colors;
using Teigha.DatabaseServices;

namespace Utilities
{
    internal class BrightnessShifter
    {
        public static void BrightnessShift()
        {
            using (Transaction transaction = Workstation.TransactionManager.StartTransaction())
            {
                // Получить объекты и значение осветления от пользователя
                PromptSelectionResult psr = Workstation.Editor.GetSelection();
                if (psr.Status != PromptStatus.OK)
                    return;
                PromptDoubleOptions pdo = new("Введите значение осветления/затемнения от -1 до 1")
                { 
                    AllowZero = true,
                    AllowNegative = true,
                    AllowNone = false
                };
                PromptDoubleResult pdr = Workstation.Editor.GetDouble(pdo);
                if (pdr.Status != PromptStatus.OK)
                    return;

                var entities = psr.Value.GetObjectIds().Select(id => id.GetObject<Entity>(OpenMode.ForWrite, transaction));
                double shift = pdr.Value;

                if (shift == 0)
                {
                    foreach (var entity in entities)
                    {
                        entity.Color = Color.FromColorIndex(ColorMethod.ByLayer, 256);
                    }
                    transaction.Commit();
                    return;
                }

                foreach (var entity in entities)
                {
                    Color initialColor = entity.Color == Color.FromColorIndex(ColorMethod.ByLayer, 256) ?
                                                         entity.LayerId.GetObject<LayerTableRecord>(OpenMode.ForRead, transaction).Color : 
                                                         entity.Color;
                    entity.Color = initialColor.BrightnessShift(shift);
                }
                transaction.Commit();
            }
        }
    }
}
