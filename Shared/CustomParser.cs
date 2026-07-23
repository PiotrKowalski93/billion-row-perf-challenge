namespace Shared
{
    public class CustomParser
    {
        public static double CustomParse(ReadOnlySpan<byte> span)
        {
            // double.Parse() is not used here to avoid unnecessary overhead and to provide a custom implementation for parsing doubles from a byte span.
            // IEEE-754

            if (span.Length > 0 && span[^1] == '\r')
            {
                span = span[..^1]; // Remove trailing '\r' if present
            }

            int index = 0;
            bool isNegative = false;

            if (span[index] == '-')
            {
                isNegative = true;
                index = 1;
            }

            double result = 0;
            var decimalFound = false;
            var decimalMultiplier = 0.1;

            while (index < span.Length)
            {
                var c = span[index];
                if (c == '.')
                {
                    decimalFound = true;
                    index++;
                    continue;
                }

                int digit = c - '0';

                if (decimalFound)
                {
                    result += digit * decimalMultiplier;
                }
                else
                {
                    result = (result * 10) + digit;
                }

                index++;
            }

            return !isNegative ? result : -result;
        }
    }
}
