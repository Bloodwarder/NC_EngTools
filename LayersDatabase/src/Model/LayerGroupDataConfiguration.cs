using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;


namespace LayersIO.Model
{
    internal class LayerGroupDataConfiguration : IEntityTypeConfiguration<LayerGroupData>
    {
        public void Configure(EntityTypeBuilder<LayerGroupData> builder)
        {
            builder.HasKey(lgd => lgd.Id).HasName("LayerGroupDataPrimaryKey");
            builder.HasAlternateKey(ld => ld.MainName).HasName("LayerGroupDataAlternateKey");
            builder.Property(lgd => lgd.Id).ValueGeneratedOnAdd();

            builder.HasOne(lgd => lgd.LayerLegendData).WithOne(lld => lld.LayerGroupData).HasForeignKey<LayerGroupData>(lgd => lgd.LayerLegendDataId);
            builder.HasMany(lgd => lgd.AlternateLayers).WithMany(lgd => lgd.AlternateLayers).UsingEntity(a => a.ToTable("AlternateLayers"));
        }
    }
}
