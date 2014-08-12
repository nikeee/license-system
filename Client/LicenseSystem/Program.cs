using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace LicenseSystem
{
    class Program
    {
        static void Main(string[] args)
        {
            var license = License.Parse(@"--------BEGIN LICENSE--------
Erika Mustermann
3
44F416FCD6C253F3DEC75938E8F4B
C5715DF28E79CB7BD5A587BCB90A3
A27017398C1871B621DA6B75BE522
D020CD451752F3B248CEFB2AD1024
52A9AF86FFB61B88B6F8897755225
98FB9F3BDE30BBF8164B26893383D
88CDCF90ECF0080217203BC9333D1
37F4E20408DB99D3F01F49E8A22AC
4FDF8DB8447116AC8DA5809A
---------END LICENSE---------");

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
        private const string _publicKey = @"<RSAKeyValue><Modulus>8CKn78RI6h7vNOPMeMCeRCHegEgG1nR+X84B8b3sOZF6hAjDXF80ag1Zw1T0E+NVHmbPB8aLgRPmQPA351ZR8D+BCHooDlGqstLLHiqTu9bbqRVPti46XBeju3Fbi47euO+omH0sq7LCuIZ5s1WBmTc9ejkkfc/0rk3fAYaIRuE=</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>";

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
                provider.FromXmlString(_publicKey);
                provider.PersistKeyInCsp = false;

                // Daten mit VerifyData überprüfen
                // Übergeben wird hier der Datenpuffer, das Hashing-Verfahren für die Signatur und Signatur selbst
                // In diesem Fall verwende ich SHA1
                return provider.VerifyData(dataBuffer, new SHA1CryptoServiceProvider(), signature);
                // Wenn die Daten gültig sind, sind die Lizenzdaten ebenfalls gültig. Wenn nicht, dann nicht.
            }
        }

        public static License Parse(string licenseData)
        {
            // Pattern, um an die Daten zwischen BEGIN und END zu kommen
            const string pattern = "^\\s*-+BEGIN LICENSE-+(?<data>(\\s|.)*?)-+END LICENSE-+\\s*$";

            var match = Regex.Match(licenseData, pattern, RegexOptions.IgnoreCase); // string auf Muster prüfen
            if (!match.Success) // Wenn das Muster nicht gematched wurde, ist der Lizenz-String nicht lesbar und somit ungültig.
                throw new FormatException();

            var rawStringData = match.Groups["data"].Value;
            if (string.IsNullOrWhiteSpace(rawStringData)) // Wenn die Daten zwischen BEGIN und END leer bzw nur WhiteSpace sind -> ungültig
                throw new FormatException();
            rawStringData = rawStringData.Trim(); // sonstiges whitespace trimmen (links udn rechts)

            var splitData = rawStringData.Split('\n'); // Splitten beim Zeilenumbruch
            if (splitData.Length < 3) // Wenn es weniger als 3 Zeilen (Name, Typ, Signatur) waren -> ungültig
                throw new FormatException();

            // Ab hier findet auch Schirtt 1.1 statt:
            // 1.1. Auftrennung der Lizenz in einzelne Datenparameter (Name, Typ, Signatur)

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
            var verificationDataRaw = string.Join(string.Empty, splitData.Skip(2)).StripWhiteSpace();

            // Dekodierung des Strings zu Binärdaten (byte[]).
            var verificationData = DecodeDataFromString(verificationDataRaw);

            // Bis hier hin konnte alles erfolgreich eingelesen werden
            // Ob die Daten aber gültig (== Signatur ist korrekt) sind, wird später überprüft.

            return new License(licenseeRaw, type, verificationData); // Rückgabe des Lizenz-Objektes mit den eingelesenen Daten
        }

        // Zum Dekodieren der Signaturdaten wird diese Funkton verwendet.
        // Wir könnten auch base64 verwenden, dabei hat man jedoch wieder Groß- und Kleinschreibung, was doof ist, sollte sich jemand die Mühe machen, alles in kleinbuchstaben abzutippen.
        // Wenn man das durch Convert.FromBase64String() ersetzt, muss man auf der Server-Seite evenfalls die funktion ersetzen.
        private static byte[] DecodeDataFromString(string value)
        {
            // Hexadezimaen String zurück in Byte-Daten umwandeln
            // macht das gleiche wie PHPs hex2bin; kehrt das bin2hex um.

            if (value == null)
                return new byte[0];

            if ((value.Length & 1) != 0) // Länge der Daten ist nicht durch 2 teilbar -> kein gültiger hexadezimaler string
                throw new FormatException();

            if (string.IsNullOrWhiteSpace(value))
                return new byte[0];

            value = value.ToUpperInvariant();

            byte[] ab = new byte[value.Length >> 1];
            for (int i = 0; i < value.Length; i++)
            {
                int b = value[i];
                b = (b - '0') + ((('9' - b) >> 31) & -7);
                ab[i >> 1] |= (byte)(b << 4 * ((i & 1) ^ 1));
            }
            return ab;
        }

        private static string GeneralizeDataString(string someString)
        {
            return someString.StripWhiteSpace().ToUpperInvariant();
        }
    }

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

    enum LicenseType
    {
        SingleUser = 1,
        Commercial,
        OpenSource
    }
}
