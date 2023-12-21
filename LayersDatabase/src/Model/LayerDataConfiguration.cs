using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LayersIO.Model
{
    public class LayerDataConfiguration : IEntityTypeConfiguration<LayerData>
    {
        public void Configure(EntityTypeBuilder<LayerData> builder)
        {
            builder.HasKey(ld => ld.Id).HasName("LayerDataPrimaryKey");
            builder.HasAlternateKey(ld => ld.Name).HasName("LayerDataAlternateKey");
            builder.Property(ld => ld.Id).ValueGeneratedOnAdd();

            builder.OwnsOne(ld => ld.LayerPropertiesData); //.HasOne(ld => ld.LayerPropertiesData).WithOne(lpd => lpd.LayerData).HasForeignKey<LayerPropertiesData>(lpd => lpd.LayerDataId);
            builder.OwnsOne(ld => ld.LayerDrawTemplateData); //.HasOne(ld => ld.LayerDrawTemplateData).WithOne(ldtd => ldtd.LayerData).HasForeignKey<LayerDrawTemplateData>(ldtd => ldtd.LayerDataId);
        }
    }
}
