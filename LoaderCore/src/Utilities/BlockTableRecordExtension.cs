using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Teigha.DatabaseServices;

namespace LoaderCore.Utilities
{
    public static class BlockTableRecordExtension
    {
        public static void AppendEntitiesRange(this BlockTableRecord record, IEnumerable<Entity> entities)
        {
            foreach (var entity in entities)
            {
                record.AppendEntity(entity);
            }
        }
    }

    public static class TransactionExtension
    {
        public static void AddDbObjects(this Transaction transaction, IEnumerable<Entity> entities)
        {
            foreach (var entity in entities) 
            {
                transaction.AddNewlyCreatedDBObject(entity, true);
            }
        }
    }
}
