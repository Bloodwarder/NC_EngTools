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
    internal class LayerGroupDataConfiguration : IEntityTypeConfiguration<LayerGroupData>
    {
        public void Configure(EntityTypeBuilder<LayerGroupData> builder)
        {
            builder.HasKey(lgd => lgd.Id).HasName("LayerGroupDataPrimaryKey");
            builder.HasAlternateKey(ld => ld.MainName).HasName("LayerGroupDataAlternateKey");
            builder.Property(lgd => lgd.Id).ValueGeneratedOnAdd();
        }
    }
}
