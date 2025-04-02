using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LayersIO.Model
{
    public class DrawOrderGroup
    {
#nullable disable warnings
        public DrawOrderGroup() { }
#nullable restore warnings
        public int Id { get; set; }
        public string Name { get; set; }
        public int Index { get; set; }
        public List<LayerData> Layers { get; } = new();
    }

    public class DrawOrderGroupConfiguration : IEntityTypeConfiguration<DrawOrderGroup>
    {
        public void Configure(EntityTypeBuilder<DrawOrderGroup> builder)
        {
            builder.HasKey(d => d.Id);
            builder.Property(d => d.Id).ValueGeneratedOnAdd();
            builder.HasMany(d => d.Layers).WithOne(l => l.DrawOrderGroup).HasForeignKey(l => l.DrawOrderGroupId);
        }
    }
}
