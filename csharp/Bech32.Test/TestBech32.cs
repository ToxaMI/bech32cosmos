using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace Bech32.Test
{
    public class Tests
    {
        public TestScenario FromWords { get; set; }
        public TestScenario Bech32 { get; set; }
    }
    public class TestScenario
    {
        public List<TestCase> Valid { get; set; } = new List<TestCase>();
        public List<TestCase> Invalid { get; set; } = new List<TestCase>();
    }
    public class TestCase
    {
        public string String { get; set; }
        public int Limit { get; set; }
        public string Exception { get; set; }

        public override string ToString()
        {
            return $"Str='{String}', Lim={Limit}, Valid={Exception == null}";
        }
    }

    [TestClass]
    public class TestBech32
    {
        public static Tests ReadTestFile(string resName)
        {
            Assembly thisAssembly = Assembly.GetExecutingAssembly();
            var stream = thisAssembly.GetManifestResourceStream($"Bech32.Test.{resName}");
            using (var sr = new StreamReader(stream))
            {
                var json = sr.ReadToEnd();
                return JsonConvert.DeserializeObject<Tests>(json);
            }
        }

        [TestMethod]
        public void TestAllBech32()
        {
            var fixtures = ReadTestFile("fixtures.json");
            foreach (var item in fixtures.Bech32.Valid)
            {
                SingleTest(item);
            }
            foreach (var item in fixtures.Bech32.Invalid)
            {
                SingleTest(item);
            }
        }

        private (string hrp, byte[] decoded) DecodeCase(TestCase item)
        {
            if (item.Limit == default(int))
            {
                return Bech32cosmos.Bech32cosmos.Decode(item.String);
            }
            return Bech32cosmos.Bech32cosmos.Decode(item.String, item.Limit);
        }


        private void SingleTest(TestCase item)
        {
            Console.WriteLine("TestCase: {0}", item);
            if (item.Exception != null)
            {
                Assert.ThrowsException<Exception>(
                    () => DecodeCase(item),
                    $"expected decoding to fail for invalid string {item.String}"
                );
            }
            else
            {
                // Check that it encodes to the same string
                var (hrp, decoded) = DecodeCase(item);
                var encoded = item.Limit == default(int)
                    ? Bech32cosmos.Bech32cosmos.Encode(hrp, decoded)
                    : Bech32cosmos.Bech32cosmos.Encode(hrp, decoded, item.Limit);
                Assert.AreEqual(item.String.ToLowerInvariant(), encoded);

                // Flip a bit in the string an make sure it is caught.
                var pos = item.String.LastIndexOf('1');
                var flipped =
                    item.String.Substring(0, pos + 1) +
                    (item.String[pos + 1] ^ 1).ToString() +
                    item.String.Substring(pos + 2);

                Assert.ThrowsException<Exception>(
                    () => Bech32cosmos.Bech32cosmos.Decode(flipped),
                    $"expected decoding to fail for invalid string {flipped}"
                );
            }
        }
    }
}