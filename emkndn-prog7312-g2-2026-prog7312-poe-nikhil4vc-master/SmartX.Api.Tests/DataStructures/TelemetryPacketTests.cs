using SmartX.Core.DataStructures;
public class TelemetryPacketTests

{
    [Fact]
    public void FloatPacket_AcceptsValidDouble()
    {
        var packet = new TelemetryPacket<double>
        {
            SensorId = Guid.NewGuid(),
            Value = 42.7,
            Unit = "%",
            SequenceNumber = 1
        };

        var result = packet.IsValid(
            double.IsFinite);

        Assert.True(result);
    }

    [Fact]
    public void IntegerPacket_RejectsNegativeValue()
    {
        var packet = new TelemetryPacket<int>
        {
            SensorId = Guid.NewGuid(),
            Value = -100,
            Unit = "W",
            SequenceNumber = 1
        };

        var result = packet.IsValid(
            value => value >= 0);

        Assert.False(result);
    }

    [Fact]
    public void BooleanPacket_RetainsTypedValue()
    {
        var packet = new TelemetryPacket<bool>
        {
            SensorId = Guid.NewGuid(),
            Value = true,
            Unit = "state",
            SequenceNumber = 1
        };

        Assert.True(packet.Value);
        Assert.IsType<bool>(packet.Value);
    }

    [Fact]
    public void Packet_RejectsEmptySensorIdentifier()
    {
        var packet = new TelemetryPacket<double>
        {
            SensorId = Guid.Empty,
            Value = 22.5,
            Unit = "°C",
            SequenceNumber = 1
        };

        Assert.False(
            packet.IsValid(double.IsFinite));
    }
}