using System;
using System.Collections.Generic;
using System.Text;
using SmartX.Core.DataStructures;

namespace SmartX.Core.DataStructures
{
    public class PowerMetricTests
    {
        [Fact]
        public void Addition_CombinesMeterLoads()
        {
            var meterOne = new PowerMetric(850);
            var meterTwo = new PowerMetric(620);

            var combined = meterOne + meterTwo;

            Assert.Equal(1470, combined.Watts);
        }

        [Fact]
        public void Subtraction_DoesNotProduceNegativePower()
        {
            var smaller = new PowerMetric(500);
            var larger = new PowerMetric(900);

            var result = smaller - larger;

            Assert.Equal(0, result.Watts);
        }

        [Fact]
        public void Comparison_IdentifiesHigherLoad()
        {
            var current = new PowerMetric(2100);
            var threshold = new PowerMetric(2000);

            Assert.True(current > threshold);
            Assert.True(current >= threshold);
        }

        [Fact]
        public void Constructor_RejectsNegativeWattage()
        {
            Assert.Throws<
                ArgumentOutOfRangeException>(
                () => new PowerMetric(-1));
        }
    }
}
