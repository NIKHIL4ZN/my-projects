using Microsoft.EntityFrameworkCore;
using SmartX.Core.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using SmartX.Core.Topology.Entities;

namespace SmartX.Infrastructure.Data
{
    public class SmartXDbContext : DbContext
    {
        public SmartXDbContext(
            DbContextOptions<SmartXDbContext> options)
            : base(options)
        {
        }

        public DbSet<Sensor> Sensors => Set<Sensor>();

        public DbSet<TelemetryReading> TelemetryReadings =>
            Set<TelemetryReading>();

        public DbSet<SensorAttachment> SensorAttachments =>
            Set<SensorAttachment>();

        public DbSet<MeshNodeEntity> MeshNodes =>
Set<MeshNodeEntity>();

        public DbSet<MeshConnectionEntity> MeshConnections =>
            Set<MeshConnectionEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            ConfigureSensor(modelBuilder);
            ConfigureTelemetryReading(modelBuilder);
            ConfigureSensorAttachment(modelBuilder);
            ConfigureMeshNode(modelBuilder);
            ConfigureMeshConnection(modelBuilder);
        }

        private static void ConfigureSensor(ModelBuilder modelBuilder)
        {
            var sensor = modelBuilder.Entity<Sensor>();

            sensor.HasKey(s => s.Id);

            sensor.Property(s => s.Name)
                .HasMaxLength(100)
                .IsRequired();

            sensor.Property(s => s.MacAddress)
                .HasMaxLength(17)
                .IsRequired();

            sensor.HasIndex(s => s.MacAddress)
                .IsUnique();

            sensor.Property(s => s.DeploymentLocation)
                .HasMaxLength(150)
                .IsRequired();

            sensor.Property(s => s.Category)
                .HasConversion<string>()
                .HasMaxLength(40);

            sensor.Property(s => s.DataType)
                .HasConversion<string>()
                .HasMaxLength(20);

            sensor.Property(s => s.Status)
                .HasConversion<string>()
                .HasMaxLength(20);

            sensor.HasMany(s => s.TelemetryReadings)
                .WithOne(t => t.Sensor)
                .HasForeignKey(t => t.SensorId)
                .OnDelete(DeleteBehavior.Cascade);

            sensor.HasMany(s => s.Attachments)
                .WithOne(a => a.Sensor)
                .HasForeignKey(a => a.SensorId)
                .OnDelete(DeleteBehavior.Cascade);
        }

        private static void ConfigureTelemetryReading(
    ModelBuilder modelBuilder)
        {
            var telemetry = modelBuilder.Entity<TelemetryReading>();

            telemetry.HasKey(t => t.Id);

            telemetry.Property(t => t.DataType)
                .HasConversion<string>()
                .HasMaxLength(20);

            telemetry.Property(t => t.Unit)
                .HasMaxLength(20)
                .IsRequired();

            telemetry.HasIndex(t => new
            {
                t.SensorId,
                t.RecordedAtUtc
            });

            telemetry.HasIndex(t => new
            {
                t.SensorId,
                t.SequenceNumber
            })
            .IsUnique();
        }

        private static void ConfigureSensorAttachment(
            ModelBuilder modelBuilder)
        {
            var attachment = modelBuilder.Entity<SensorAttachment>();

            attachment.HasKey(a => a.Id);

            attachment.Property(a => a.OriginalFileName)
                .HasMaxLength(255)
                .IsRequired();

            attachment.Property(a => a.StoredFileName)
                .HasMaxLength(255)
                .IsRequired();

            attachment.Property(a => a.ContentType)
                .HasMaxLength(100)
                .IsRequired();

            attachment.Property(a => a.RelativePath)
                .HasMaxLength(500)
                .IsRequired();
        }
        private static void ConfigureMeshNode(
    ModelBuilder modelBuilder)
        {
            var node = modelBuilder.Entity<MeshNodeEntity>();

            node.HasKey(item => item.Id);

            node.Property(item => item.Name)
                .HasMaxLength(100)
                .IsRequired();

            node.Property(item => item.MacAddress)
                .HasMaxLength(17)
                .IsRequired();

            node.HasIndex(item => item.MacAddress)
                .IsUnique();

            node.Property(item => item.DeploymentLocation)
                .HasMaxLength(150)
                .IsRequired();

            // Each sensor can appear only once in the mesh.
            // Gateways have no SensorId.
            node.HasIndex(item => item.SensorId)
                .IsUnique()
                .HasFilter("[SensorId] IS NOT NULL");

            node.HasOne(item => item.Sensor)
                .WithMany()
                .HasForeignKey(item => item.SensorId)
                .OnDelete(DeleteBehavior.Restrict);

            node.ToTable("MeshNodes", table =>
            {
                table.HasCheckConstraint(
                    "CK_MeshNodes_NodeRole",
                    "([IsGateway] = 1 AND [SensorId] IS NULL) OR " +
                    "([IsGateway] = 0 AND [SensorId] IS NOT NULL)");
            });
        }

        private static void ConfigureMeshConnection(
            ModelBuilder modelBuilder)
        {
            var connection =
                modelBuilder.Entity<MeshConnectionEntity>();

            connection.HasKey(item => item.Id);

            connection.HasOne(item => item.FirstNode)
                .WithMany()
                .HasForeignKey(item => item.FirstNodeId)
                .OnDelete(DeleteBehavior.Restrict);

            connection.HasOne(item => item.SecondNode)
                .WithMany()
                .HasForeignKey(item => item.SecondNodeId)
                .OnDelete(DeleteBehavior.Restrict);

            connection.HasIndex(item => new
            {
                item.FirstNodeId,
                item.SecondNodeId
            })
                .IsUnique();

            connection.ToTable("MeshConnections", table =>
            {
                table.HasCheckConstraint(
                    "CK_MeshConnections_PositiveCost",
                    "[EnergyCost] > 0");

                // Canonical endpoint ordering prevents both A-B and B-A
                // from being stored as separate undirected connections.
                table.HasCheckConstraint(
                    "CK_MeshConnections_EndpointOrder",
                    "[FirstNodeId] < [SecondNodeId]");
            });
        }
    }
}
