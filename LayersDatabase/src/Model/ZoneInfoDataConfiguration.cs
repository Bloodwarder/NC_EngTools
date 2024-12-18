using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LayersIO.Model
{
    public class ZoneInfoDataConfiguration : IEntityTypeConfiguration<ZoneInfoData>
    {
        public void Configure(EntityTypeBuilder<ZoneInfoData> builder)
        {
            builder.ToTable("ZoneInfo");

            builder.HasKey(z => z.Id).HasName($"ZoneInfo_PrimaryKey");
            builder.Property(z => z.Id).ValueGeneratedOnAdd();

            builder.HasIndex(z => new { z.SourceLayerId, z.ZoneLayerId, z.AdditionalFilter }).IsUnique(); // UNDONE: добавить в индекс поле доп-фильтра
            builder.Property(z => z.SourceLayerId).IsRequired();
            builder.Property(z => z.ZoneLayerId).IsRequired();

            builder.HasOne(z => z.SourceLayer).WithMany(ld => ld.Zones).HasForeignKey(z => z.SourceLayerId);
            builder.HasOne(z => z.ZoneLayer).WithMany().HasForeignKey(z => z.ZoneLayerId);
        }
    }
}
