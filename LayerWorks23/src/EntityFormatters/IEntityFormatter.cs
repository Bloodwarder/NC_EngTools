using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Teigha.DatabaseServices;

namespace LayerWorks.EntityFormatters
{
    public interface IEntityFormatter
    {
        public void FormatEntity(Entity entity);
        public void FormatEntity<T>(T entity) where T : Entity;
    }
}
