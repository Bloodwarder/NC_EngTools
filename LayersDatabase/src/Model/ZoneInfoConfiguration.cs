using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LayersIO.Model
{
    public class ZoneInfoConfiguration : IEntityTypeConfiguration<ZoneInfoData>
    {
        public void Configure(EntityTypeBuilder<ZoneInfoData> builder)
        {
            builder.ToTable("ZoneInfo");

            builder.HasKey(z => z.Id).HasName($"(ZoneInfo)_PrimaryKey");
            builder.Property(z => z.Id).ValueGeneratedOnAdd();

            builder.HasAlternateKey(z => new { z.SourceLayer, z.ZoneLayer }).HasName($"ZoneInfo_LayerPairKey");
            builder.Property(z => z.SourceLayer).IsRequired();
            builder.Property(z => z.ZoneLayer).IsRequired();

            builder.HasOne(z => z.SourceLayer).WithMany(ld => ld.Zones);
            builder.HasOne(z => z.ZoneLayer);
        }
    }
}
