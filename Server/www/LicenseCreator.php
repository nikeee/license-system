<?php

require_once 'Crypt/RSA.php';
require_once 'LicenseType.php';

class LicenseCreator
{
	// Niemals anderen Leuten zugänglich machen!
	const privateKey = '<RSAKeyValue><Modulus>8CKn78RI6h7vNOPMeMCeRCHegEgG1nR+X84B8b3sOZF6hAjDXF80ag1Zw1T0E+NVHmbPB8aLgRPmQPA351ZR8D+BCHooDlGqstLLHiqTu9bbqRVPti46XBeju3Fbi47euO+omH0sq7LCuIZ5s1WBmTc9ejkkfc/0rk3fAYaIRuE=</Modulus><Exponent>AQAB</Exponent><P>/m1FEqol/KKhxOyGsK4GVuansBXhrAgpwMlYLT+vF0gy1jzYQDNNQXzeQFYH6gZY66RTYFl3JPNL8KXLyhwDLQ==</P><Q>8Z7DrGQsGhiLgg70j40/+AgfNKJB4SXY7FmyBmLPRiHkT2d3AyvzuNNf/hkHA2UMLQT4xewmkxK9MU2nDitzBQ==</Q><DP>uRVOSSyjo6u/WJzjwoVmMTNryymv2FC75vXRgmEwgxRPfxAWFGX9jmVC3LR432KsrwcEbDPI+4VNugsyO52zJQ==</DP><DQ>AJFY8FzD5cPNAB883+F7FwAd4qfG89p86gFD89PjnMyTlsQteWpvBi4o+ZXheFaScsCiPQTTCmFu5GDEVbowaQ==</DQ><InverseQ>7gQ8MGqjjrCAfOzrrC9ZuVdGRfEjUEdHMqiF+js7XNBvnT5lBznUOd+eta6CGo7S5hjU7D3CEzmVGQfxUsRZ1w==</InverseQ><D>6YSaERSs31dTwPghV+/gOFtDVzYzyAqi9iGMTHwnotfw70LiUAqZGuR+vO/5Jvn0RUsu2t3dvZkPWWkAxCtyIzALk8Brx1r8n76VHVWMzkZvOoqMa1/HdZCXM0TVlpnYVJjyUA8wzi4tzPIPv08lAGwYJzHcoMlFHkQ2npqflxE=</D></RSAKeyValue>';

	public static function CreateLicense($licensee, $type)
	{
		// Gleiche Generalisierung wie am Client:
		$licenseeGen = self::GeneralizeDataString($licensee);
		$dataStr = $licenseeGen . (int)$type; // ERIKAMUSTERMANN2

		// Setzen der RSA-Optionen auf die, die auch am Client verwendet werden:
		$rsa = new Crypt_RSA();
		$rsa->setPrivateKeyFormat(CRYPT_RSA_PRIVATE_FORMAT_XML);
		$rsa->loadKey(self::privateKey);
		$rsa->setHash('SHA1');
		$rsa->setSignatureMode(CRYPT_RSA_SIGNATURE_PKCS1);

		$signature = $rsa->sign($dataStr);

		return self::FormatLicense($licensee, $type, $signature);
	}

	private static function FormatLicense($licensee, $type, $signature)
	{
		$formattedSignature = self::EncodeDataToHeyString($signature);
		$formattedSignature = chunk_split($formattedSignature, 29); // Signatur in 29-Zeichen-Blöcke aufteilen

		$l = "--------BEGIN LICENSE--------\n";
		$l .= $licensee . "\n";
		$l .= (int)$type . "\n";
		$l .= trim($formattedSignature) . "\n";
		$l .= "---------END LICENSE---------";

		return $l;
	}

	private static function EncodeDataToHeyString($data)
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
		// Gleiche Funktion wie am Client
		return preg_replace('/\s+/', '', $someString);
	}
}
