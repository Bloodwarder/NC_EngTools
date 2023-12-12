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
    public class LayerDataConfiguration : IEntityTypeConfiguration<LayerData>
    {
        public void Configure(EntityTypeBuilder<LayerData> builder)
        {
            builder.HasKey(ld => ld.Id).HasName("LayerDataPrimaryKey");
            builder.HasAlternateKey(ld => ld.Name).HasName("LayerDataAlternateKey");
            builder.Property(ld => ld.Id).ValueGeneratedOnAdd();

            builder.HasOne(ld => ld.LayerPropertiesData).WithOne(lpd => lpd.LayerData).HasForeignKey<LayerPropertiesData>(lpd => lpd.LayerDataId);
            builder.HasOne(ld => ld.LayerDrawTemplateData).WithOne(ldtd => ldtd.LayerData).HasForeignKey<LayerDrawTemplateData>(ldtd => ldtd.LayerDataId);
        }
    }
}
