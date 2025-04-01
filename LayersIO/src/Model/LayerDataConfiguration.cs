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
                   .Ignore(ld => ld.Prefix)
                   .Ignore(ld => ld.MainName)
                   .HasKey(ld => ld.Id);
            //builder.HasIndex(ld => new { ld.LayerGroupId, ld.StatusName }).IsUnique();
            builder.Property(ld => ld.Id).ValueGeneratedOnAdd();

            builder.HasOne(ld => ld.LayerGroup).WithMany(lg => lg.Layers).HasForeignKey(l => l.LayerGroupId).OnDelete(DeleteBehavior.Cascade);

            builder.OwnsOne(ld => ld.LayerPropertiesData);
            builder.OwnsOne(ld => ld.LayerDrawTemplateData);
            
            builder.HasMany(ld => ld.Zones).WithOne(z => z.SourceLayer);
        }
    }
}
