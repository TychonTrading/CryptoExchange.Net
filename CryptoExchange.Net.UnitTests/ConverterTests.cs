﻿using CryptoExchange.Net.Attributes;
using CryptoExchange.Net.Converters;
using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CryptoExchange.Net.UnitTests
{
    [TestFixture()]
    public class ConverterTests
    {
        [TestCase("2021-05-12")]
        [TestCase("20210512")]
        [TestCase("210512")]
        [TestCase("1620777600.000")]
        [TestCase("1620777600000")]
        [TestCase("2021-05-12T00:00:00.000Z")]
        [TestCase("", true)]
        [TestCase("  ", true)]
        public void TestDateTimeConverterString(string input, bool expectNull = false)
        {
            var output = JsonConvert.DeserializeObject<TimeObject>($"{{ \"time\": \"{input}\" }}");
            Assert.AreEqual(output.Time, expectNull ? null: new DateTime(2021, 05, 12, 0, 0, 0, DateTimeKind.Utc));
        }

        [TestCase(1620777600.000)]
        [TestCase(1620777600000d)]
        public void TestDateTimeConverterDouble(double input)
        {
            var output = JsonConvert.DeserializeObject<TimeObject>($"{{ \"time\": {input} }}");
            Assert.AreEqual(output.Time, new DateTime(2021, 05, 12, 0, 0, 0, DateTimeKind.Utc));
        }

        [TestCase(1620777600)]
        [TestCase(1620777600000)]
        [TestCase(1620777600000000)]
        [TestCase(1620777600000000000)]
        [TestCase(0, true)]
        public void TestDateTimeConverterLong(long input, bool expectNull = false)
        {
            var output = JsonConvert.DeserializeObject<TimeObject>($"{{ \"time\": {input} }}");
            Assert.AreEqual(output.Time, expectNull ? null : new DateTime(2021, 05, 12, 0, 0, 0, DateTimeKind.Utc));
        }

        [TestCase()]
        public void TestDateTimeConverterNull()
        {
            var output = JsonConvert.DeserializeObject<TimeObject>($"{{ \"time\": null }}");
            Assert.AreEqual(output.Time, null);
        }

        // TODO add tests for ToMilliseconds static methods

        [TestCase(TestEnum.One, "1")]
        [TestCase(TestEnum.Two, "2")]
        [TestCase(TestEnum.Three, "three")]
        [TestCase(TestEnum.Four, "Four")]
        [TestCase(null, null)]
        public void TestEnumConverterNullableGetStringTests(TestEnum? value, string expected)
        {
            var output = EnumConverter.GetString(value);
            Assert.AreEqual(output, expected);
        }

        [TestCase(TestEnum.One, "1")]
        [TestCase(TestEnum.Two, "2")]
        [TestCase(TestEnum.Three, "three")]
        [TestCase(TestEnum.Four, "Four")]
        public void TestEnumConverterGetStringTests(TestEnum value, string expected)
        {
            var output = EnumConverter.GetString(value);
            Assert.AreEqual(output, expected);
        }

        [TestCase("1", TestEnum.One)]
        [TestCase("2", TestEnum.Two)]
        [TestCase("3", TestEnum.Three)]
        [TestCase("three", TestEnum.Three)]
        [TestCase("Four", TestEnum.Four)]
        [TestCase("four", TestEnum.Four)]
        [TestCase("Four1", null)]
        [TestCase(null, null)]
        public void TestEnumConverterNullableDeserializeTests(string? value, TestEnum? expected)
        {
            var val = value == null ? "null" : $"\"{value}\"";
            var output = JsonConvert.DeserializeObject<EnumObject?>($"{{ \"Value\": {val} }}");
            Assert.AreEqual(output.Value, expected);
        }
    }

    public class TimeObject
    {
        [JsonConverter(typeof(DateTimeConverter))]
        public DateTime? Time { get; set; }
    }

    public class EnumObject
    {
        public TestEnum? Value { get; set; }
    }

    [JsonConverter(typeof(EnumConverter))]
    public enum TestEnum
    {
        [Map("1")]
        One,
        [Map("2")]
        Two,
        [Map("three", "3")]
        Three,
        Four
    }
}
