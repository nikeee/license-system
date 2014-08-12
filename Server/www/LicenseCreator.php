<?php

require_once 'Crypt/RSA.php'; // PHPSec's RSA-Klasse einbinden
require_once 'LicenseType.php'; // Unsere Klasse mit den Enum-Werten des Lizenztypen einbinden

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
