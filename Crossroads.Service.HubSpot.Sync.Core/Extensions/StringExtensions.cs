
using System.Linq;

namespace System
{
    public static class StringExtensions
    {
        /// <summary>
        /// Evaluates whether the string in question is essentially void of meaningful value. Syntactic sugar for
        /// String.IsNullOrWhiteSpace, which is basically what the original String.IsNullOrEmpty
        /// should have been.
        /// </summary>
        public static bool IsNullOrEmpty(this string toCheckForNullEmptyOrWhitespace)
        {
            return string.IsNullOrWhiteSpace(toCheckForNullEmptyOrWhitespace);
        }

        public static bool IsNotNullOrEmpty(this string toConfirmIsNotNullEmptyOrWhitespace)
        {
            return toConfirmIsNotNullEmptyOrWhitespace.IsNullOrEmpty() == false;
        }

        public static string CapitalizeFirstLetter(this string valueToCapitalize)
        {
            if (valueToCapitalize.IsNullOrEmpty())
                return valueToCapitalize;

            if (valueToCapitalize.ToUpperInvariant().First().Equals(valueToCapitalize[0])) // don't mess with it if the first character is already proper case
                return valueToCapitalize;

            return Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(valueToCapitalize);
        }
    }
}