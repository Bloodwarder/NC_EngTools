using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace LayersDatabase.Model
{
    internal class LayerDataConfiguration : IEntityTypeConfiguration<LayerData>
    {
        public void Configure(EntityTypeBuilder<LayerData> builder)
        {
            builder.HasKey(ld => ld.Id).HasName("LayerDataPrimaryKey");
            builder.HasAlternateKey(ld => ld.Name).HasName("LayerDataAlternateKey");
            builder.Property(ld => ld.Id).ValueGeneratedOnAdd();
        }
    }
}
