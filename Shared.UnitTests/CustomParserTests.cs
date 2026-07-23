using System.Globalization;
using System.Text;

namespace Shared.UnitTests
{
    public class CustomParserTests
    {
        [Theory]
        [InlineData("36.6", 36.6)]
        [InlineData("-36.6", -36.6)]
        [InlineData("99.9", 99.9)]
        [InlineData("-99.9", -99.9)]
        [InlineData("6.6", 6.6)]
        [InlineData("-6.6", -6.6)]
        [InlineData("-0.6", -0.6)]
        [InlineData("0.6", 0.6)]
        public void Should_Parse_String_Temperature(string input, double expected)
        {
            var result = CustomParser.CustomParse(Encoding.UTF8.GetBytes(input));

            Assert.Equal(expected, result, 0.01);
        }

        [Fact]
        public void Test()
        {
            var _bytes = Encoding.UTF8.GetBytes("9.1");   

            Span<char> chars = stackalloc char[_bytes.Length];
            var count = Encoding.UTF8.GetChars(_bytes, chars);
            var x = double.Parse(chars[..count], CultureInfo.InvariantCulture);

            Assert.Equal(9.1, x, 0.01);
        }
    }
}
