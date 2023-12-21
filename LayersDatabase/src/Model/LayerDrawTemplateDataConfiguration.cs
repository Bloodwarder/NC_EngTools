using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LayersIO.Model
{
    internal class LayerDrawTemplateDataConfiguration : IEntityTypeConfiguration<LayerDrawTemplateData>
    {
        public void Configure(EntityTypeBuilder<LayerDrawTemplateData> builder)
        {
            builder.HasNoKey();
            builder.Property(ldt => ldt.DrawTemplate).IsRequired();
        }
    }
}
