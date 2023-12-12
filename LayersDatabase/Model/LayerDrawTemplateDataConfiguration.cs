using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LayersDatabase.Model
{
    internal class LayerDrawTemplateDataConfiguration : IEntityTypeConfiguration<LayerDrawTemplateData>
    {
        public void Configure(EntityTypeBuilder<LayerDrawTemplateData> builder)
        {
            builder.HasKey(ldt => ldt.Id).HasName("LayerDrawTemplateDataPrimaryKey");
            builder.Property(ldt => ldt.Id).ValueGeneratedOnAdd();

            builder.Property(ldt => ldt.DrawTemplate).IsRequired();
        }
    }
}
