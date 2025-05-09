﻿// <auto-generated />
using System;
using LayersIO.Connection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace LayerDbMigrations.Migrations
{
    [DbContext(typeof(LayersDatabaseContextSqlite))]
    [Migration("20250204072044_OldLayersAndGroupsReindex")]
    partial class OldLayersAndGroupsReindex
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "7.0.20");

            modelBuilder.Entity("LayersIO.Model.LayerData", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("LayerGroupId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("StatusName")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("LayerGroupId");

                    b.ToTable("LayerData", (string)null);
                });

            modelBuilder.Entity("LayersIO.Model.LayerGroupData", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("AlternateLayer")
                        .HasColumnType("TEXT");

                    b.Property<string>("MainName")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Prefix")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("Prefix");

                    b.HasIndex("Prefix", "MainName")
                        .IsUnique();

                    b.ToTable("LayerGroups", (string)null);
                });

            modelBuilder.Entity("LayersIO.Model.OldLayerReference", b =>
                {
                    b.Property<string>("OldLayerGroupName")
                        .HasColumnType("TEXT");

                    b.Property<int>("NewLayerGroupId")
                        .HasColumnType("INTEGER");

                    b.HasKey("OldLayerGroupName");

                    b.HasIndex("NewLayerGroupId");

                    b.ToTable("OldLayers");
                });

            modelBuilder.Entity("LayersIO.Model.ZoneInfoData", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("AdditionalFilter")
                        .HasColumnType("TEXT");

                    b.Property<double>("DefaultConstructionWidth")
                        .HasColumnType("REAL");

                    b.Property<bool>("IgnoreConstructionWidth")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("IsSpecial")
                        .HasColumnType("INTEGER");

                    b.Property<int>("SourceLayerId")
                        .HasColumnType("INTEGER");

                    b.Property<double>("Value")
                        .HasColumnType("REAL");

                    b.Property<int>("ZoneLayerId")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id")
                        .HasName("ZoneInfo_PrimaryKey");

                    b.HasIndex("ZoneLayerId");

                    b.HasIndex("SourceLayerId", "ZoneLayerId", "AdditionalFilter")
                        .IsUnique();

                    b.ToTable("ZoneInfo", (string)null);
                });

            modelBuilder.Entity("LayersIO.Model.ZoneMapping", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("AdditionalFilter")
                        .HasColumnType("TEXT");

                    b.Property<string>("SourcePrefix")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("SourceStatus")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("TargetPrefix")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("TargetStatus")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("SourcePrefix", "SourceStatus", "AdditionalFilter", "TargetPrefix", "TargetStatus")
                        .IsUnique();

                    b.ToTable("ZoneMappings", (string)null);
                });

            modelBuilder.Entity("LayersIO.Model.LayerData", b =>
                {
                    b.HasOne("LayersIO.Model.LayerGroupData", "LayerGroup")
                        .WithMany("Layers")
                        .HasForeignKey("LayerGroupId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.OwnsOne("LayersIO.Model.LayerDrawTemplateData", "LayerDrawTemplateData", b1 =>
                        {
                            b1.Property<int>("LayerDataId")
                                .HasColumnType("INTEGER");

                            b1.Property<string>("BlockName")
                                .HasColumnType("TEXT");

                            b1.Property<string>("BlockPath")
                                .HasColumnType("TEXT");

                            b1.Property<double?>("BlockXOffset")
                                .HasColumnType("REAL");

                            b1.Property<double?>("BlockYOffset")
                                .HasColumnType("REAL");

                            b1.Property<string>("DrawTemplate")
                                .HasColumnType("TEXT");

                            b1.Property<string>("FenceHeight")
                                .HasColumnType("TEXT");

                            b1.Property<string>("FenceLayer")
                                .HasColumnType("TEXT");

                            b1.Property<string>("FenceWidth")
                                .HasColumnType("TEXT");

                            b1.Property<string>("Height")
                                .HasColumnType("TEXT");

                            b1.Property<double?>("InnerBorderBrightness")
                                .HasColumnType("REAL");

                            b1.Property<double?>("InnerHatchAngle")
                                .HasColumnType("REAL");

                            b1.Property<double?>("InnerHatchBrightness")
                                .HasColumnType("REAL");

                            b1.Property<string>("InnerHatchPattern")
                                .HasColumnType("TEXT");

                            b1.Property<double?>("InnerHatchScale")
                                .HasColumnType("REAL");

                            b1.Property<string>("MarkChar")
                                .HasColumnType("TEXT");

                            b1.Property<double?>("OuterHatchAngle")
                                .HasColumnType("REAL");

                            b1.Property<double?>("OuterHatchBrightness")
                                .HasColumnType("REAL");

                            b1.Property<string>("OuterHatchPattern")
                                .HasColumnType("TEXT");

                            b1.Property<double?>("OuterHatchScale")
                                .HasColumnType("REAL");

                            b1.Property<double?>("Radius")
                                .HasColumnType("REAL");

                            b1.Property<string>("Width")
                                .HasColumnType("TEXT");

                            b1.HasKey("LayerDataId");

                            b1.ToTable("LayerData");

                            b1.WithOwner()
                                .HasForeignKey("LayerDataId");
                        });

                    b.OwnsOne("LayersIO.Model.LayerPropertiesData", "LayerPropertiesData", b1 =>
                        {
                            b1.Property<int>("LayerDataId")
                                .HasColumnType("INTEGER");

                            b1.Property<byte>("Blue")
                                .HasColumnType("INTEGER");

                            b1.Property<double>("ConstantWidth")
                                .HasColumnType("REAL");

                            b1.Property<int>("DrawOrderIndex")
                                .HasColumnType("INTEGER");

                            b1.Property<byte>("Green")
                                .HasColumnType("INTEGER");

                            b1.Property<int>("LineWeight")
                                .HasColumnType("INTEGER");

                            b1.Property<string>("LinetypeName")
                                .IsRequired()
                                .HasColumnType("TEXT");

                            b1.Property<double>("LinetypeScale")
                                .HasColumnType("REAL");

                            b1.Property<byte>("Red")
                                .HasColumnType("INTEGER");

                            b1.HasKey("LayerDataId");

                            b1.ToTable("LayerData");

                            b1.WithOwner()
                                .HasForeignKey("LayerDataId");
                        });

                    b.Navigation("LayerDrawTemplateData")
                        .IsRequired();

                    b.Navigation("LayerGroup");

                    b.Navigation("LayerPropertiesData")
                        .IsRequired();
                });

            modelBuilder.Entity("LayersIO.Model.LayerGroupData", b =>
                {
                    b.OwnsOne("LayersIO.Model.LayerLegendData", "LayerLegendData", b1 =>
                        {
                            b1.Property<int>("LayerGroupDataId")
                                .HasColumnType("INTEGER");

                            b1.Property<bool>("IgnoreLayer")
                                .HasColumnType("INTEGER");

                            b1.Property<string>("Label")
                                .IsRequired()
                                .HasColumnType("TEXT");

                            b1.Property<int>("Rank")
                                .HasColumnType("INTEGER");

                            b1.Property<string>("SubLabel")
                                .HasColumnType("TEXT");

                            b1.HasKey("LayerGroupDataId");

                            b1.ToTable("LayerGroups");

                            b1.WithOwner()
                                .HasForeignKey("LayerGroupDataId");
                        });

                    b.Navigation("LayerLegendData")
                        .IsRequired();
                });

            modelBuilder.Entity("LayersIO.Model.OldLayerReference", b =>
                {
                    b.HasOne("LayersIO.Model.LayerGroupData", "NewLayerGroup")
                        .WithMany("OldLayerReferences")
                        .HasForeignKey("NewLayerGroupId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("NewLayerGroup");
                });

            modelBuilder.Entity("LayersIO.Model.ZoneInfoData", b =>
                {
                    b.HasOne("LayersIO.Model.LayerData", "SourceLayer")
                        .WithMany("Zones")
                        .HasForeignKey("SourceLayerId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("LayersIO.Model.LayerData", "ZoneLayer")
                        .WithMany()
                        .HasForeignKey("ZoneLayerId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("SourceLayer");

                    b.Navigation("ZoneLayer");
                });

            modelBuilder.Entity("LayersIO.Model.LayerData", b =>
                {
                    b.Navigation("Zones");
                });

            modelBuilder.Entity("LayersIO.Model.LayerGroupData", b =>
                {
                    b.Navigation("Layers");

                    b.Navigation("OldLayerReferences");
                });
#pragma warning restore 612, 618
        }
    }
}
