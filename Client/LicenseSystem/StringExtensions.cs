using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LicenseSystem
{
    internal static class StringExtensions
    {
        public static string StripWhiteSpace(this string value)
        {
            if (value == null)
                return null;
            if (value.Length == 0 || value.Trim().Length == 0)
                return string.Empty;
            var sb = new StringBuilder(value.Length);
            for (int i = 0; i < value.Length; ++i)
                if (!char.IsWhiteSpace(value[i]))
                    sb.Append(value[i]);
            return sb.ToString();
        }
    }
}
