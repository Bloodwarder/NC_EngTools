using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LayersDatabaseEditor.ViewModel
{
    public interface IDbRelatedViewModel
    {
        public bool IsValid { get; }
        public bool IsUpdated { get; }
        public void UpdateDbEntity();
        public void ResetValues();
    }
}
