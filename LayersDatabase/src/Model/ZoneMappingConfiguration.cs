using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace LayersIO.Model
{
    public class ZoneMappingConfiguration : IEntityTypeConfiguration<ZoneMapping>
    {
        public void Configure(EntityTypeBuilder<ZoneMapping> builder)
        {
            builder.ToTable("ZoneMappings");
            builder.HasKey(z => z.Id);
            builder.Property(z => z.Id).ValueGeneratedOnAdd();
            builder.Property(z => z.SourcePrefix).IsRequired();
            builder.Property(z => z.SourceStatus).IsRequired();
            builder.Property(z => z.TargetPrefix).IsRequired();
            builder.Property(z => z.TargetStatus).IsRequired();

            builder.HasIndex(z => new {z.SourcePrefix, z.SourceStatus, z.AdditionalFilter, z.TargetPrefix , z.TargetStatus}).IsUnique();
        }
    }
}
