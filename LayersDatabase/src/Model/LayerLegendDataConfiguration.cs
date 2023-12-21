using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LayersIO.Model
{
    internal class LayerLegendDataConfiguration : IEntityTypeConfiguration<LayerLegendData>
    {
        public void Configure(EntityTypeBuilder<LayerLegendData> builder)
        {
            builder.HasKey(l => l.Id).HasName("LayerLegendDataPrimaryKey");
            builder.Property(l => l.Id).ValueGeneratedOnAdd();
        }
    }
}
