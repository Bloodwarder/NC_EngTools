using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LayersIO.Model
{
    public class ZoneInfo
    {
        public ZoneInfo() { }
        public int Id { get; set; }
        public LayerData SourceLayer { get; set; }
        public LayerData ZoneLayer { get; set; }
        public double Value { get; set; }
        public double DefaultConstructionWidth { get; set; }
    }

    public class ZoneInfoConfiguration : IEntityTypeConfiguration<ZoneInfo>
    {
        public void Configure(EntityTypeBuilder<ZoneInfo> builder)
        {
            builder.ToTable(nameof(ZoneInfo));

            builder.HasKey(z => z.Id).HasName($"{nameof(ZoneInfo)}_PrimaryKey");
            builder.Property(z => z.Id).ValueGeneratedOnAdd();

            builder.HasAlternateKey(z => new { z.SourceLayer, z.ZoneLayer }).HasName($"{nameof(ZoneInfo)}_LayerPairKey");
            builder.Property(z => z.SourceLayer).IsRequired();
            builder.Property(z => z.ZoneLayer).IsRequired();

            builder.HasOne(z => z.SourceLayer).WithMany(ld => ld.Zones);
            builder.HasOne(z => z.ZoneLayer);


            //builder.ToTable("LayerData");
            //builder.Ignore(ld => ld.Name);
            //builder.Ignore(ld => ld.Separator);
            //builder.HasKey(ld => ld.Id).HasName("LayerDataPrimaryKey");
            //builder.HasAlternateKey(ld => new { ld.MainName, ld.Separator, ld.StatusName }).HasName("LayerDataAlternateKey");
            //builder.Property(ld => ld.Id).ValueGeneratedOnAdd();

            //builder.OwnsOne(ld => ld.LayerPropertiesData); //.HasOne(ld => ld.LayerPropertiesData).WithOne(lpd => lpd.LayerData).HasForeignKey<LayerPropertiesData>(lpd => lpd.LayerDataId);
            //builder.OwnsOne(ld => ld.LayerDrawTemplateData); //.HasOne(ld => ld.LayerDrawTemplateData).WithOne(ldtd => ldtd.LayerData).HasForeignKey<LayerDrawTemplateData>(ldtd => ldtd.LayerDataId);
            ////builder.Property(ld => ld.LayerDrawTemplateData).IsRequired();
        }
    }
}
