using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;


namespace LayersIO.Model
{
    internal class LayerGroupDataConfiguration : IEntityTypeConfiguration<LayerGroupData>
    {
        public void Configure(EntityTypeBuilder<LayerGroupData> builder)
        {
            builder.ToTable("LayerGroups");
            builder.HasKey(lgd => lgd.Id).HasName("LayerGroupDataPrimaryKey");
            builder.HasAlternateKey(ld => ld.MainName).HasName("LayerGroupDataAlternateKey");
            builder.Property(lgd => lgd.Id).ValueGeneratedOnAdd();

            builder.OwnsOne(ld => ld.LayerLegendData);
            builder.HasMany(lgd => lgd.AlternateLayers);
        }
    }
}
