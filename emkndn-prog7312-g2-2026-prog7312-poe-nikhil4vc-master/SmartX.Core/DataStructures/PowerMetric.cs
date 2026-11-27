using System;
using System.Collections.Generic;
using System.Text;

namespace SmartX.Core.DataStructures
{
    public readonly record struct PowerMetric
    {
        public PowerMetric(int watts)
        {
            if (watts < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(watts),
                    "Power wattage cannot be negative.");
            }

            Watts = watts;
        }

        public int Watts { get; }

        public static PowerMetric operator +(
            PowerMetric left,
            PowerMetric right)
        {
            return new PowerMetric(
                checked(left.Watts + right.Watts));
        }

        public static PowerMetric operator -(
            PowerMetric left,
            PowerMetric right)
        {
            return new PowerMetric(
                Math.Max(0, left.Watts - right.Watts));
        }

        public static bool operator >(
            PowerMetric left,
            PowerMetric right)
        {
            return left.Watts > right.Watts;
        }

        public static bool operator <(
            PowerMetric left,
            PowerMetric right)
        {
            return left.Watts < right.Watts;
        }

        public static bool operator >=(
            PowerMetric left,
            PowerMetric right)
        {
            return left.Watts >= right.Watts;
        }

        public static bool operator <=(
            PowerMetric left,
            PowerMetric right)
        {
            return left.Watts <= right.Watts;
        }

        public override string ToString()
        {
            return $"{Watts} W";
        }
    }
}

