using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LayersIO.Model
{
    internal class OldLayerReferenceConfiguration : IEntityTypeConfiguration<OldLayerReference>
    {
        public void Configure(EntityTypeBuilder<OldLayerReference> builder)
        {
            builder.HasKey(x => x.OldLayerGroupName);
            builder.HasOne(x => x.NewLayerGroup).WithMany(lg => lg.OldLayerReferences).HasForeignKey(x => x.NewLayerGroupId).OnDelete(DeleteBehavior.Cascade);
        }
    }
}
