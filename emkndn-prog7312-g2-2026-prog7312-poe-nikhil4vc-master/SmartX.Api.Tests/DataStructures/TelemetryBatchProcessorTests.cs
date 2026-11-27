using System;
using System.Collections.Generic;
using System.Text;
using SmartX.Core.DataStructures;

namespace SmartX.Core.DataStructures
{
    public class TelemetryBatchProcessorTests
    {
        [Fact]
        public void ConvertToPackets_PreservesJaggedArrayOrder()
        {
            var sensorId = Guid.NewGuid();

            double[][] batches =
            [
                [42.5, 42.1, 41.8],
            [41.4],
            [40.9, 40.6]
            ];

            var startedAt =
                new DateTime(
                    2026,
                    8,
                    30,
                    12,
                    0,
                    0,
                    DateTimeKind.Utc);

            var packets =
                TelemetryBatchProcessor
                    .ConvertToPackets(
                        sensorId,
                        batches,
                        "%",
                        10,
                        startedAt,
                        TimeSpan.FromSeconds(5));

            Assert.IsType<
                List<TelemetryPacket<double>>>(
                packets);

            Assert.Equal(6, packets.Count);
            Assert.Equal(10, packets[0].SequenceNumber);
            Assert.Equal(15, packets[5].SequenceNumber);
            Assert.Equal(42.5, packets[0].Value);
            Assert.Equal(40.6, packets[5].Value);

            Assert.Equal(
                startedAt.AddSeconds(25),
                packets[5].CapturedAtUtc);
        }

        [Fact]
        public void ConvertToPackets_RejectsEmptyBatch()
        {
            Assert.Throws<ArgumentException>(
                () =>
                    TelemetryBatchProcessor
                        .ConvertToPackets<double>(
                            Guid.NewGuid(),
                            [],
                            "%",
                            0,
                            DateTime.UtcNow,
                            TimeSpan.FromSeconds(5)));
        }

        [Fact]
        public void ConvertToPackets_RejectsInvalidInterval()
        {
            double[][] batches =
            [
                [1.0]
            ];

            Assert.Throws<
                ArgumentOutOfRangeException>(
                () =>
                    TelemetryBatchProcessor
                        .ConvertToPackets(
                            Guid.NewGuid(),
                            batches,
                            "%",
                            0,
                            DateTime.UtcNow,
                            TimeSpan.Zero));
        }
    }
}
