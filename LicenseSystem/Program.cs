using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NTH.Text;
using System.Security.Cryptography;

namespace LicenseSystem
{
    class Program
    {
        static void Main(string[] args)
        {

        }
    }

    class License
    {
        private const string _publicKey = "";

        private readonly bool _isValid;
        public bool IsValid { get { return _isValid; } }

        private readonly string _licensee;
        public string Licensee { get { return _licensee; } }

        private readonly LicenseType _type;
        public LicenseType Type { get { return _type; } }

        public License(string licensee, LicenseType type, byte[] verificationData)
        {
            if (string.IsNullOrEmpty(licensee))
                throw new ArgumentNullException("licensee");
            if (verificationData == null)
                throw new ArgumentNullException("verificationData");

            _licensee = licensee;
            _type = type;
            _isValid = ValidateLicense(verificationData);
        }

        private bool ValidateLicense(byte[] signature)
        {
            var licenseeGen = GeneralizeDataString(_licensee);

            var dataStr = licenseeGen + (int)_type; //VORNAMENACHNAME2

            var dataBuffer = System.Text.Encoding.UTF8.GetBytes(dataStr);

            using (var provider = new RSACryptoServiceProvider())
            {
                // provider.Import...
                return provider.VerifyData(dataBuffer, CryptoConfig.MapNameToOID("SHA2"), signature);
            }
        }

        private const string LicensePrefix = "^((-+?)BEGIN LICENSE(-+?))";
        private const string LicenseSuffix = "((-+?)END LICENSE(-+?)\\s?)$";
        public static License Parse(string licenseData)
        {
            const string pattern = LicensePrefix + "(?<data>.+?)" + LicenseSuffix;

            var match = Regex.Match(licenseData, pattern);
            if (!match.Success)
                throw new FormatException();

            var rawStringData = match.Groups["data"].Value;
            if (string.IsNullOrWhiteSpace(rawStringData))
                throw new FormatException();
            rawStringData = rawStringData.Trim();

            var splitData = rawStringData.Split('\n');
            if (splitData.Length < 3)
                throw new FormatException();

            var licenseeRaw = splitData[0].Trim();
            var licenseTypeRaw = splitData[1].Trim();

            var type = (LicenseType)int.Parse(licenseTypeRaw);

            if (type != LicenseType.SingleUser
                && type != LicenseType.Commercial
                && type != LicenseType.OpenSource)
            {
                throw new FormatException();
            }

            var verificationDataRaw = string.Join(string.Empty, splitData.Skip(2)).Trim();
            var verificationData = Convert.FromBase64String(verificationDataRaw);

            return new License(licenseeRaw, type, verificationData);
        }

        private static string GeneralizeDataString(string someString)
        {
            return someString.StripWhiteSpace().ToUpperInvariant();
        }
    }

    enum LicenseType
    {
        SingleUser = 1,
        Commercial,
        OpenSource
    }
}
