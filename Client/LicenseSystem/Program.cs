using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NTH.Text;
using NTH.Security.Cryptography;
using System.Security.Cryptography;

namespace LicenseSystem
{
    class Program
    {
        static void Main(string[] args)
        {
            var license = License.Parse("----BEGIN LICENSE-----...");
            if (license.IsValid)
            {
                Console.WriteLine("Gültige Lizenz!");
                Console.WriteLine("Lizenztyp: " + license.Type);
            }
            else
            {
                Console.WriteLine("Ungültige Lizenz!");
            }


            Console.ReadKey();
        }
    }

    class License
    {
        private const string _publicKey = @"-----BEGIN PUBLIC KEY-----
MIGfMA0GCSqGSIb3DQEBAQUAA4GNADCBiQKBgQDwIqfvxEjqHu8048x4wJ5EId6A
SAbWdH5fzgHxvew5kXqECMNcXzRqDVnDVPQT41UeZs8HxouBE+ZA8DfnVlHwP4EI
eigOUaqy0sseKpO71tupFU+2LjpcF6O7cVuLjt6476iYfSyrssK4hnmzVYGZNz16
OSR9z/SuTd8BhohG4QIDAQAB
-----END PUBLIC KEY-----";

        private readonly bool _isValid;
        public bool IsValid { get { return _isValid; } }

        private readonly string _licensee;
        public string Licensee { get { return _licensee; } }

        private readonly LicenseType _type;
        public LicenseType Type { get { return _type; } }

        protected License(string licensee, LicenseType type, byte[] verificationData)
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
            // Um die Lizenz auf Gültigkeit zu prüfen müssen alle zu prüfenden Parameter (Name, Typ) in einen Buffer gepackt werden
            // Dies kann man wie folgt umsetzen:

            // Standardisierung des Namens des Lizenznehmers
            var licenseeGen = GeneralizeDataString(this._licensee); // "ERIKAMUSTERMANN"

            // Zusammenfüren des Namens "ERIKAMUSTERMANN" mit dem Int-Wert des Lizenztyps (z. B. 2 für "Commercial").
            var dataStr = licenseeGen + (int)this._type; //ERIKAMUSTERMANN2

            // Erstellen eines Byte-Arrays aus dem zusammengefügten String
            var dataBuffer = System.Text.Encoding.UTF8.GetBytes(dataStr);

            // Crypto-Provider erstellen
            using (var provider = new RSACryptoServiceProvider())
            {
                // Den Public Key festlegen
                provider.ImportPublicKeyPem(_publicKey);

                // Daten mit VerifyData überprüfen
                // Übergeben wird hier der Datenpuffer, das Hashing-Verfahren für die Signatur und Signatur selbst
                // In diesem Fall verwende ich SHA1
                return provider.VerifyData(dataBuffer, CryptoConfig.MapNameToOID("SHA1"), signature);
                // Wenn die Daten gültig sind, sind die Lizenzdaten ebenfalls gültig. Wenn nicht, dann nicht.
            }
        }

        private const string LicensePrefix = "^((-+?)BEGIN LICENSE(-+?))";
        private const string LicenseSuffix = "((-+?)END LICENSE(-+?)\\s?)$";
        public static License Parse(string licenseData)
        {
            const string pattern = LicensePrefix + "(?<data>.+?)" + LicenseSuffix; // Pattern, um an die Daten zwischen BEGIN und END zu kommen

            var match = Regex.Match(licenseData, pattern); // string auf Muster prüfen
            if (!match.Success) // Wenn das Muster nicht gematched wurde, ist der Lizenz-String nicht lesbar und somit ungültig.
                throw new FormatException();

            var rawStringData = match.Groups["data"].Value;
            if (string.IsNullOrWhiteSpace(rawStringData)) // Wenn die Daten zwischen BEGIN und END leer bzw nur WhiteSpace sind -> ungültig
                throw new FormatException();
            rawStringData = rawStringData.Trim(); // sonstiges whitespace trimmen (links udn rechts)

            var splitData = rawStringData.Split('\n'); // Splitten beim Zeilenumbruch
            if (splitData.Length < 3) // Wenn es weniger als 3 Zeilen (Name, Typ, Signatur) waren -> ungültig
                throw new FormatException();

            var licenseeRaw = splitData[0].Trim(); // Name des Lizenznehmers in 1. Zeile
            var licenseTypeRaw = splitData[1].Trim(); // Integer-Wert des Enum-Members von LicenseType in 2. Zeile

            var type = (LicenseType)int.Parse(licenseTypeRaw); // Integer-Wert in LicenseType umwandeln

            if (type != LicenseType.SingleUser
                && type != LicenseType.Commercial
                && type != LicenseType.OpenSource)
            {
                // Enums könenn auch Werte annehmen, die nicht im Enum definiert sind, z. B. durch einen Cast.
                // Falls dies bei LicenseType der Fall ist -> ungültig
                throw new FormatException();
            }

            // Die Signatur besteht aus allen verbleibenden Zeilen
            var verificationDataRaw = string.Join(string.Empty, splitData.Skip(2)).Trim();

            // Dekodierung des Strings zu Binärdaten (byte[]).
            var verificationData = Convert.FromBase64String(verificationDataRaw);

            // Bis hier hin konnte alles erfolgreich eingelesen werden
            // Ob die Daten aber gültig (== Signatur ist korrekt) sind, wird später überprüft.

            return new License(licenseeRaw, type, verificationData); // Rückgabe des Lizenz-Objektes mit den eingelesenen Daten
        }

        private static string GeneralizeDataString(string someString)
        {
            return someString.StripWhiteSpace().ToUpperInvariant();

            // StripWhiteSpace() kommt aus der NTH-Library.
            // Die Methode ist wie folgt definiert:
            // https://github.com/nikeee/nth/blob/7813f6b80e54afc539601c4c74edfe880f5bbd26/src/NTH/NTH/Text/StringExtensions.cs#L37
        }
    }

    enum LicenseType
    {
        SingleUser = 1,
        Commercial,
        OpenSource
    }
}
