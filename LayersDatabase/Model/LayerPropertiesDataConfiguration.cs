using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LayersDatabase.Model
{
    internal class LayerPropertiesDataConfiguration : IEntityTypeConfiguration<LayerPropertiesData>
    {
        public void Configure(EntityTypeBuilder<LayerPropertiesData> builder)
        {
        }
    }
}
