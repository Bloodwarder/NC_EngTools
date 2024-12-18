using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;


namespace LayersIO.Model
{
    internal class LayerGroupDataConfiguration : IEntityTypeConfiguration<LayerGroupData>
    {
        public void Configure(EntityTypeBuilder<LayerGroupData> builder)
        {
            builder.ToTable("LayerGroups")
                   .Ignore(ld => ld.Name)
                   .Ignore(ld => ld.Separator);
            builder.HasKey(lgd => lgd.Id).HasName("LayerGroupDataPrimaryKey");
            builder.HasAlternateKey(ld => ld.MainName).HasName("LayerGroupDataAlternateKey");
            builder.Property(lgd => lgd.Id).ValueGeneratedOnAdd();

            builder.OwnsOne(ld => ld.LayerLegendData);
            builder.HasMany(ld => ld.Layers).WithOne(l => l.LayerGroup).HasPrincipalKey(l => l.MainName);
            //builder.HasOne(lgd => lgd.AlternateLayer).WithOne(lgd => lgd.AlternateLayer).HasForeignKey<LayerGroupData>(lgd => lgd.AlternateLayerId);
        }
    }
}
