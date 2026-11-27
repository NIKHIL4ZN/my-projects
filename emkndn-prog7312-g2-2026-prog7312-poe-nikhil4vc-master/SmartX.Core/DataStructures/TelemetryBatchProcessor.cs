using System;
using System.Collections.Generic;
using System.Text;

namespace SmartX.Core.DataStructures
{
    public static class TelemetryBatchProcessor
    {
        public static List<TelemetryPacket<T>> ConvertToPackets<T>(
            Guid sensorId,
            T[][] batches,
            string unit,
            int startingSequenceNumber,
            DateTime startedAtUtc,
            TimeSpan interval)
            where T : struct
        {
            if (sensorId == Guid.Empty)
            {
                throw new ArgumentException(
                    "A valid sensor ID is required.",
                    nameof(sensorId));
            }

            ArgumentNullException.ThrowIfNull(batches);

            if (batches.Length == 0)
            {
                throw new ArgumentException(
                    "At least one telemetry batch is required.",
                    nameof(batches));
            }

            if (string.IsNullOrWhiteSpace(unit))
            {
                throw new ArgumentException(
                    "A telemetry unit is required.",
                    nameof(unit));
            }

            if (startingSequenceNumber < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(startingSequenceNumber));
            }

            if (interval <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(interval));
            }

            var totalReadingCount = 0;

            foreach (var batch in batches)
            {
                if (batch is null)
                {
                    throw new ArgumentException(
                        "Telemetry batches cannot contain null arrays.",
                        nameof(batches));
                }

                totalReadingCount += batch.Length;
            }

            if (totalReadingCount == 0)
            {
                throw new ArgumentException(
                    "The telemetry batches contain no readings.",
                    nameof(batches));
            }

            var packets =
                new List<TelemetryPacket<T>>(totalReadingCount);

            var sequenceNumber = startingSequenceNumber;
            var readingOffset = 0;

            foreach (var batch in batches)
            {
                foreach (var value in batch)
                {
                    packets.Add(new TelemetryPacket<T>
                    {
                        SensorId = sensorId,
                        Value = value,
                        Unit = unit.Trim(),
                        SequenceNumber = sequenceNumber,
                        CapturedAtUtc =
                            startedAtUtc
                                .AddTicks(
                                    interval.Ticks
                                    * readingOffset)
                    });

                    sequenceNumber++;
                    readingOffset++;
                }
            }

            return packets;
        }
    }
}

