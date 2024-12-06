using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LayersIO.Model
{
    public class LayerDataConfiguration : IEntityTypeConfiguration<LayerData>
    {
        public void Configure(EntityTypeBuilder<LayerData> builder)
        {
            builder.ToTable("LayerData")
                   .Ignore(ld => ld.Name)
                   .Ignore(ld => ld.Separator)
                   .HasKey(ld => ld.Id).HasName("LayerDataPrimaryKey");
            builder.HasIndex(ld => new { ld.Prefix, ld.MainName, ld.StatusName }).IsUnique();
            builder.Property(ld => ld.Id).ValueGeneratedOnAdd();

            builder.OwnsOne(ld => ld.LayerPropertiesData);
            builder.OwnsOne(ld => ld.LayerDrawTemplateData);
            
            builder.HasMany(ld => ld.Zones).WithOne(z => z.SourceLayer);
        }
    }
}
