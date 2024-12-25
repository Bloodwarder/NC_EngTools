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

            builder.HasKey(lgd => lgd.Id);
            builder.HasAlternateKey(ld => new { ld.Prefix, ld.MainName });

            builder.Property(lgd => lgd.Id).ValueGeneratedOnAdd();
            builder.Property(lg => lg.Prefix).IsRequired();
            builder.Property(lg => lg.MainName).IsRequired();


            builder.OwnsOne(ld => ld.LayerLegendData);
            builder.HasMany(ld => ld.Layers).WithOne(l => l.LayerGroup).HasForeignKey(l => l.LayerGroupId);
            //builder.HasOne(lgd => lgd.AlternateLayer).WithOne(lgd => lgd.AlternateLayer).HasForeignKey<LayerGroupData>(lgd => lgd.AlternateLayerId);
        }
    }
}
