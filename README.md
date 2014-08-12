
## Voraussetzungen
Um die Schritte in diesem Beitrag nachvollziehen zu können, solltest Du grundlegende Kenntnisse zu Public-Private-Key-Kryptografie haben. Hier geht es speziell um das Signieren von Daten.
Außerdem solltest Du die grundlegenden Konzepte von C# (bzw. .NET) beherrschen. Ich zeige es hier mit PHP als Server-Backend, weshalb es auch nicht übel wäre, wenn Du PHP-Code zumindest lesen könntest.

## Verbesser mich!
De ganze Beitrag inklusive Quelltext befindet sich auf GitHub und kann dort von Jedem verbessert werden:
<link>
Falls Dir etwas auffällt oder du ein anderes Anliegen hast, kannst Du mir gerne eine Issue hinterlassen oder mich kontaktieren.

## Was ist das Ziel?
Ziel ist es, einen Namen und zusätzliche, beliebige Daten mit einer Signatur zu versehen, sodass auf dieser Grundlage ein Lizenzsystem implementiert werden kann. Bei diesem Lizenzsystem gibt es einen Client und einen Server. Die Aufgabe des Servers ist es, Lizenzschlüssel auszustellen, die am Client mittels RSA-Signatur validiert werden können. So kann der Client die Lizenz auf Gültigkeit prüfen, ohne den Server zu kontaktieren.

## Okay, dann mal los!
Die Vorgehensweise bei der Methode, wie ich sie hier zeige, lässt sich in folgende Schritte unterteilen:

1. Einlesen der Lizenz
1.1. Auftrennung der Lizenz in einzelne Datenparameter (Name, Typ, Signatur)
2. Standardisierung der übergebenen Daten in einheitliches Format
3. Validierung der Daten mittels überprüfung der RSA-Signatur

### Aufbau der Lizenz
Eine Lizenz ist wie folgt aufgebaut:
```
----------BEGIN LICENSE----------
<Vorname> <Nachname>
<Lizenztyp>
<Signatur>
-----------END LICENSE-----------
```

- `<Vorname> <Nachname>`: stehen für den Lizenznehmer. Das kann auch eine E-Mail-Adresse oder irgendein beliebiger String sein. Ich verwende hier Vor- und Nachname.
- `<Lizenztyp>`: Um noch zu zeigen, dass man im Prinzip alles in so eine Lizenz stecken kann, habe ich dieses Feld hinzugefügt. Es steht für die Art, um die es sich bei der Lizenz handelt. Z. B. `"Free"`, `"Trial"` oder `"Pro"`. Ich habe hier `SingleUser`, `Commercial` und `OpenSource` verwendet.
- `<Signatur>`: Im Prinzip würde es ausreichen, die ersten beiden Parameter zu lesen und zu wissen, um was für eine Lizenz es sich bei was für einem Lizenznehmer handelt. Leider ist sie dann nicht geschützt vor Manipulation. Aus diesem Grund benötigt man etwas, um die anderen Daten der Lizenz zu validieren. Hierfür wird diese RSA-SHA1-Signatur verwendet. Du musst natürlich nicht RSA nehmen.

Lass' Deiner Kreativität oder deinen Ansprüchen freien Lauf! Es wäre z. B. noch möglich, ein Ablaufdatum oder eine E-Mail-Adresse zu hinzuzufügen. Der Einfachheit halber habe ich mich aber auf 2 Eigenschaften beschränkt.
Wie Du die Lizenzdaten letztendlich aufbaust, ist Dir überlassen. Man könnte hierbei auch mit XML oder JSON arbeiten, um die Verarbeitung etwas zu vereinfachen.

Eine Lizenz sieht dann z. B. so aus:
```
----------BEGIN LICENSE----------
Erika Mustermann
2
DEADBEEFCAFEBABEC001D00DEBEEFEA7E5
DEADBEEFCAFEBABEC001D00DEBEEFEA7E5
DEADBEEFCAFEBABEC001D00DEBEEFEA7E5
DEADBEEFCAFEBABEC001D00DEBEEFEA7E5
DEADBEEFCAFEBABEC001D00DEBEEFEA7E5
-----------END LICENSE-----------
```
(Die Signaturdaten sind nicht gültig)

### 0. Die Lizenz-Klasse
Um den Lizenzkram besser vom restlichen Code der Anwendung zu trennen, legen wir eine Klasse für eine Lizenz an. Diese sieht bei mir jetzt so aus:

```
class License
{
    private const string _publicKey = ""; // TODO

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

    private bool ValidateLicense(byte[] signature) { /* TODO */ }

    public static License Parse(string licenseData) { /* TODO */ }

    private static string GeneralizeDataString(string someString) { /* TODO */ }
}
```

Außerdem habe ich noch 3 verschiedene Lizenztypen gewählt, um zu zeigen, dass man noch weitere Daten in die Lizenz packen kann:
```
enum LicenseType
{
    SingleUser = 1,
    Commercial,
    OpenSource
}
```

Die Stellen, die mit "TODO" gekennzeichnet sind, werden wir in den nächsten Schritten behandeln.

#### 0.5 Verwendung der Lizenz-Klasse
Die Lizenzklasse kann am Ende so verwendet werden:
```
var license = License.Parse("----BEGIN LICENSE-----...");
if(license.IsValid)
{
    Console.WriteLine("Gültige Lizenz!");
    Console.WriteLine("Lizenztyp: " + license.Type);
}
else
{
    Console.WriteLine("Ungültige Lizenz!");
}
```

Der Konstruktor ist protected. Ich habe das in diesem Fall so gewählt, da ich möchte, dass man eine Instanz von License nur mit der Parse-Methode erstellen kann. Natürlich könnte man den Konstruktor auch öffentlich machen.

### 1. Einlesen der Lizenz
Dieser Teil hat eigentlich noch nichts mit Kryptografie zu tun. Es geht nur um das einfache Einlesen der Daten aus dem Lizenzstring, um diese dann an den Konstruktor der License-Klasse zu übergeben.
Der Parse-Teil sieht bei mir so aus:

```
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
    var verificationDataRaw = string.Join(string.Empty, splitData.Skip(2)).Trim();

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
```

Wie gesagt. Ich verwende hier ein Format, das ich von Subliem Text abgeschaut habe. Du kannst dir auch ein eigenes ausdenken, das auf z. B. XML oder JSON basiert, um dir das Auslesen zu vereinfachen.


### 2. Standardisierung der übergebenen Daten in einheitliches Format

Da wir nicht sicher ein können, dass unser Benutzer seinen Namen leicht abgeändert hat, müssen wir das Gane in ein Standard-Format bringen. Dies ist sinnvoll, da z. B. "Erika Mustermann" und "erika Mustermann" den gleichen Namen bezeichnen, aber ansich unterschiedliche Strings sind.
Hierfür habe ich folgende Funktion angelegt:
```
private static string GeneralizeDataString(string someString)
{
    return someString.StripWhiteSpace().ToUpperInvariant();
}
```
Die StripWhiteSpace-Funktion ist als String-Extension wie folgt definiert:
```
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
```

Diese funktion entfernt sämtlichen Whitespace aus dem String und konvertiert anschließend alle Buchstaben in Großbuchstaben.
So wird:
`"Erika Mustermann"` zu `"ErikaMustermann"` zu `"ERIKAMUSTERMANN"`
...und dementsprechend
`"erika Mustermann"` zu `"erikaMustermann"` zu `"ERIKAMUSTERMANN"`
Auch `"eRikA musStermAnN"` wird zu `"ERIKAMUSTERMANN"`.
Dadurch erreichen wir, dass die Lizenz weniger anfällig für Änderungen ist, die ein unerfahrener Benutzer eventuell vornehmen könnte (Änderung des String-Casings).

Dieser Schritt ist für alle Daten nötig, die für soetwas anfällig wären. In diesem Beispiel sind das aber keine weiteren.

### 3. Validierung der Daten mittels überprüfung der RSA-Signatur
Nun kommt der eigentlich kryptografische Teil und auch die letzte Funktion der License-Klasse.

```
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
```
Das war's schon fast! Wir benötigen nun noch ein Schlüsselpaar. Dieses kann man z. B. mit OpenSSL erzeugen. Ich nehme hier jetzt mal ein Beispiel-Schlüsselpaar, welches Du nicht improduktiven Einsatz verwenden solltest!(!)

Mein Private Key in dem Fall:
```XML
<RSAKeyValue><Modulus>8CKn78RI6h7vNOPMeMCeRCHegEgG1nR+X84B8b3sOZF6hAjDXF80ag1Zw1T0E+NVHmbPB8aLgRPmQPA351ZR8D+BCHooDlGqstLLHiqTu9bbqRVPti46XBeju3Fbi47euO+omH0sq7LCuIZ5s1WBmTc9ejkkfc/0rk3fAYaIRuE=</Modulus><Exponent>AQAB</Exponent><P>/m1FEqol/KKhxOyGsK4GVuansBXhrAgpwMlYLT+vF0gy1jzYQDNNQXzeQFYH6gZY66RTYFl3JPNL8KXLyhwDLQ==</P><Q>8Z7DrGQsGhiLgg70j40/+AgfNKJB4SXY7FmyBmLPRiHkT2d3AyvzuNNf/hkHA2UMLQT4xewmkxK9MU2nDitzBQ==</Q><DP>uRVOSSyjo6u/WJzjwoVmMTNryymv2FC75vXRgmEwgxRPfxAWFGX9jmVC3LR432KsrwcEbDPI+4VNugsyO52zJQ==</DP><DQ>AJFY8FzD5cPNAB883+F7FwAd4qfG89p86gFD89PjnMyTlsQteWpvBi4o+ZXheFaScsCiPQTTCmFu5GDEVbowaQ==</DQ><InverseQ>7gQ8MGqjjrCAfOzrrC9ZuVdGRfEjUEdHMqiF+js7XNBvnT5lBznUOd+eta6CGo7S5hjU7D3CEzmVGQfxUsRZ1w==</InverseQ><D>6YSaERSs31dTwPghV+/gOFtDVzYzyAqi9iGMTHwnotfw70LiUAqZGuR+vO/5Jvn0RUsu2t3dvZkPWWkAxCtyIzALk8Brx1r8n76VHVWMzkZvOoqMa1/HdZCXM0TVlpnYVJjyUA8wzi4tzPIPv08lAGwYJzHcoMlFHkQ2npqflxE=</D></RSAKeyValue>
```
(Anmerkung: Beim XML-Format ist hier der Public Key mit dabei)

Der dazugehörige Public Key:
```XML
<RSAKeyValue><Modulus>8CKn78RI6h7vNOPMeMCeRCHegEgG1nR+X84B8b3sOZF6hAjDXF80ag1Zw1T0E+NVHmbPB8aLgRPmQPA351ZR8D+BCHooDlGqstLLHiqTu9bbqRVPti46XBeju3Fbi47euO+omH0sq7LCuIZ5s1WBmTc9ejkkfc/0rk3fAYaIRuE=</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>
```

Ich habe beide Schlüssel jetzt im XML-Format. Wenn Du andere Formate bevorzugst, kannst Du diese auch verwenden. Ich nehme jetzt dieses, da dieses Format von Haus aus mit .NET kompatibel ist und die PHP-Library PHPSecLib es ebenfalls unterstützt.

Der Private Key wird zum erstellen einer Lizenzdatei verwendet. Dieser darf niemals preisgegeben werden. Sobald jemand im Besitz dieses Schlüssels ist, kann derjenige sich so viele Lizenzen erstellen, wie er will!(!). Der Private Key darf acuh keinesfalls irgendwo im Quelltext der Anwendung stehen, die an die Benutzer rausgeht!

#### ...weiter im Text.

Den Public Key fügen wir einfach oben als String-Wert der Konstante ein.

```C#
private const string _publicKey = @"<RSAKeyValue><Modulus>8CKn78RI6h7vNOPMeMCeRCHegEgG1nR+X84B8b3sOZF6hAjDXF80ag1Zw1T0E+NVHmbPB8aLgRPmQPA351ZR8D+BCHooDlGqstLLHiqTu9bbqRVPti46XBeju3Fbi47euO+omH0sq7LCuIZ5s1WBmTc9ejkkfc/0rk3fAYaIRuE=</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>";
```

Soweit sind wir fertig! Der Client kann nun eine Lizenz parsen, sie in eine Klasse stecken und mittels RSA-Signatur validieren.

### Lizenzen ausstellen

Um Lizenzen auszustellen benötigen wir den Private Key. Bitte achte darauf, dass _niemand_ außer dir Zugriff auf diesen Schlüssel haben darf.

Um dies zu tun bietet sich ein Server an.

Mit PHP und der PHPSecLib könnte es wie folgt gehen:
```PHP
// Abbildung des Enums, das wir auch in der Client-Anwendung haben
class LicenseType
{
    const Personal = 1;
    const Commercial = 2;
    const OpenSource = 3;
}
```

```PHP
// Generierung von Lizenzen in einer separaten Klasse
class LicenseCreator
{
    // Niemals anderen Leuten zugänglich machen!
    const privateKey = '<RSAKeyValue><Modulus>8CKn78RI6h7vNOPMeMCeRCHegEgG1nR+X84B8b3sOZF6hAjDXF80ag1Zw1T0E+NVHmbPB8aLgRPmQPA351ZR8D+BCHooDlGqstLLHiqTu9bbqRVPti46XBeju3Fbi47euO+omH0sq7LCuIZ5s1WBmTc9ejkkfc/0rk3fAYaIRuE=</Modulus><Exponent>AQAB</Exponent><P>/m1FEqol/KKhxOyGsK4GVuansBXhrAgpwMlYLT+vF0gy1jzYQDNNQXzeQFYH6gZY66RTYFl3JPNL8KXLyhwDLQ==</P><Q>8Z7DrGQsGhiLgg70j40/+AgfNKJB4SXY7FmyBmLPRiHkT2d3AyvzuNNf/hkHA2UMLQT4xewmkxK9MU2nDitzBQ==</Q><DP>uRVOSSyjo6u/WJzjwoVmMTNryymv2FC75vXRgmEwgxRPfxAWFGX9jmVC3LR432KsrwcEbDPI+4VNugsyO52zJQ==</DP><DQ>AJFY8FzD5cPNAB883+F7FwAd4qfG89p86gFD89PjnMyTlsQteWpvBi4o+ZXheFaScsCiPQTTCmFu5GDEVbowaQ==</DQ><InverseQ>7gQ8MGqjjrCAfOzrrC9ZuVdGRfEjUEdHMqiF+js7XNBvnT5lBznUOd+eta6CGo7S5hjU7D3CEzmVGQfxUsRZ1w==</InverseQ><D>6YSaERSs31dTwPghV+/gOFtDVzYzyAqi9iGMTHwnotfw70LiUAqZGuR+vO/5Jvn0RUsu2t3dvZkPWWkAxCtyIzALk8Brx1r8n76VHVWMzkZvOoqMa1/HdZCXM0TVlpnYVJjyUA8wzi4tzPIPv08lAGwYJzHcoMlFHkQ2npqflxE=</D></RSAKeyValue>';

    public static function CreateLicense($licensee, $type)
    {
        // Gleiche Generalisierung wie am Client:
        $licenseeGen = self::GeneralizeDataString($licensee);
        $dataStr = $licenseeGen . (int)$type; // "ERIKAMUSTERMANN2"

        $rsa = new Crypt_RSA(); // Neue RSA-Klasse erstellen

        // Setzen der RSA-Optionen auf die, die auch am Client verwendet werden:
        $rsa->setPrivateKeyFormat(CRYPT_RSA_PRIVATE_FORMAT_XML);
        $rsa->setHash('SHA1');
        $rsa->setSignatureMode(CRYPT_RSA_SIGNATURE_PKCS1);

        // privaten Schlüssel laden
        $rsa->loadKey(self::privateKey);

        // Erstellen der Signatur
        $signature = $rsa->sign($dataStr);

        // Formatierte Lizenzdaten zurückgeben
        return self::FormatLicense($licensee, $type, $signature);
    }

    private static function FormatLicense($licensee, $type, $signature)
    {
        // Binärdaten aus $signature in hexadezimal kodierten String umwandeln
        $formattedSignature = self::EncodeDataToHexString($signature);

        // Signatur in 29-Zeichen-Blöcke aufteilen (sieht schöner aus)
        $formattedSignature = chunk_split($formattedSignature, 29);

        $l = "--------BEGIN LICENSE--------\n"; // Unser Anfangsblock
        $l .= $licensee . "\n"; // Der Name des Lizenznehmers
        $l .= (int)$type . "\n"; // Der Lizenztyp als Int
        $l .= trim($formattedSignature) . "\n"; // die in mehrere Zeilen aufgeteilte, kodierte Signatur
        $l .= "---------END LICENSE---------"; // Ende der Lizenz

        return $l;
    }

    private static function EncodeDataToHexString($data)
    {
        return strtoupper(bin2hex($data));
    }

    private static function GeneralizeDataString($someString)
    {
        // Gleiche Funktion wie am Client
        return strtoupper(self::StripWhiteSpace($someString));
    }

    private static function StripWhiteSpace($someString)
    {
        // Gleiche Funktion wie am Client, nur mit RegEx
        return preg_replace('/\s+/', '', $someString);
    }
}
```

### Abschluss

Das war's.

Auf der Serverseite können wir nun mit Hilfe der LicenseCreator-Klasse eine Lizenz erstellen:
```PHP
$license = LicenseCreator::CreateLicense("Erika Mustermann", LicenseType::Commercial);
```
Heraus kommt sowas:
```
--------BEGIN LICENSE--------
Erika Mustermann
2
0D0E9D62B80195C9C867CF451C312
80593BFAEE80450BDD46A2CEAFFED
6D378CD9408B328B05AC2C8D9A7AE
D8B8B69D44DBF66EA0F814A800393
7AD16197EF4DB28FDD27CFF58B1FC
14DF3CD7912C41C2573BB0A0D59AD
94BE0EFCD804D8A809875F13CAC70
137F24E30478AE8DFD3B94025A38D
80D636637F725887869ED77E
---------END LICENSE---------
```

Dieser String kann am Client validiert werden.
```C#
var license = License.Parse(lizenzString);
Console.WriteLine("Lizenz gültig? " + license.IsValid);
if(license.IsValid)
    Console.WriteLine("Lizenztyp: " + license.Type);
```

#### Vorteile und Nachteile dieser Methode

##### Vorteile
- Keine Internetverbindung zum Validieren der Lizenz notwendig
- Key-Generatoren sind so gut wie unmöglich, solange der Schlüssel lang genug gewählt wurde und der Private key privat bleibt
- Geringe Fehleranfälligkeit, da man nicht auf Firewall-Umgebungen, die UAC oder ähnliches Rücksicht nehmen muss.

##### Nachteile
- Sehr einfach zu Cracken

#### Was noch gemacht werden muss
- License.TryParse(), bei der keine Exception geworfen wird
- Server-Beispiel mit Node.js

#### "Einfach zu Cracken" vs "Keygens unmöglich":
Das hört sich im ersten Moment recht widersprüchlich an, aber so ist es. Jemand könnte die Anwendung leicht cracken, indem an der entsprechenden Stelle einfach ein "return true;" eingefügt wird. Um dies zu tun, muss derjenige allerdings die Anwendung bearbeiten. Das hat den "Nachteil", dass wenn eine Update der Anwendung erscheint, dies erneut machen muss. Das bringt einen zusätzlichen Aufwand mit sich.

#### Meine Meinung zu dem Thema:
Ich finde, man sollte sein Programm nicht mit irgendwelchem "unknackbaren" Lizenzkram verwurschteln, was es am Ende nur fehleranfälliger und unbenutzbarer macht. Wirklich viel mehr geschützt ist es dadurch auch nicht.
Generell sollte man IMO die Zeit lieber in die Funktionalität des Programms statt in ein komplexes Lizenzsystem stecken.
Das hier gezeigte System ähnelt stark dem, welches u. A. bei Sublime Text zum Einsatz kommt.
Ich finde diese Herangehensweise noch gut vertretbar, da sie recht simpel gehalten ist und trotzdem noch eine gute Hürde bietet.
