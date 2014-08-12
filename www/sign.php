<?php

include('Crypt/RSA.php');

class LicenseCreator
{
	private const privateKeyPem = '-----BEGIN RSA PRIVATE KEY-----
MIICXgIBAAKBgQDwIqfvxEjqHu8048x4wJ5EId6ASAbWdH5fzgHxvew5kXqECMNc
XzRqDVnDVPQT41UeZs8HxouBE+ZA8DfnVlHwP4EIeigOUaqy0sseKpO71tupFU+2
LjpcF6O7cVuLjt6476iYfSyrssK4hnmzVYGZNz16OSR9z/SuTd8BhohG4QIDAQAB
AoGBAOmEmhEUrN9XU8D4IVfv4DhbQ1c2M8gKovYhjEx8J6LX8O9C4lAKmRrkfrzv
+Sb59EVLLtrd3b2ZD1lpAMQrciMwC5PAa8da/J++lR1VjM5GbzqKjGtfx3WQlzNE
1ZaZ2FSY8lAPMM4uLczyD79PJQBsGCcx3KDJRR5ENp6an5cRAkEA/m1FEqol/KKh
xOyGsK4GVuansBXhrAgpwMlYLT+vF0gy1jzYQDNNQXzeQFYH6gZY66RTYFl3JPNL
8KXLyhwDLQJBAPGew6xkLBoYi4IO9I+NP/gIHzSiQeEl2OxZsgZiz0Yh5E9ndwMr
87jTX/4ZBwNlDC0E+MXsJpMSvTFNpw4rcwUCQQC5FU5JLKOjq79YnOPChWYxM2vL
Ka/YULvm9dGCYTCDFE9/EBYUZf2OZULctHjfYqyvBwRsM8j7hU26CzI7nbMlAkAA
kVjwXMPlw80AHzzf4XsXAB3ip8bz2nzqAUPz0+OczJOWxC15am8GLij5leF4VpJy
wKI9BNMKYW7kYMRVujBpAkEA7gQ8MGqjjrCAfOzrrC9ZuVdGRfEjUEdHMqiF+js7
XNBvnT5lBznUOd+eta6CGo7S5hjU7D3CEzmVGQfxUsRZ1w==
-----END RSA PRIVATE KEY-----';

	public static function CreateLicense($licensee, $type)
	{
		// Gleiche Generalisierung wie am Client:
		$licenseeGen = self::GeneralizeDataString($licensee);
		$dataStr = $licenseeGen + (int)$type;

		// Setzen der RSA-Optionen auf die, die auch am Client verwendet werden:
		$rsa = new Crypt_RSA();
		$rsa->loadKey(self::privateKeyPem);
		$rsa->setHash('SHA1');
		$rsa->setSignatureMode(CRYPT_RSA_SIGNATURE_PKCS1);

		$signature = $rsa->sign($dataStr);

		return self::FormatLicense($licensee, $type, $signature);
	}

	private static function FormatLicense($licensee, $type, $signature)
	{
		$l = "--------BEGIN LICENSE--------\n";
		$l .= $name . "\n";
		$l .= (int)$type . "\n";
		$l .= $signature . "\n";
		$l .= "---------END LICENSE---------";

		return $l;
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

// Abbildung des Enums, das wir auch in der Client-Anwendung haben
class LicenseType
{
	const Personal = 1;
	const Commercial = 2;
	const OpenSource = 3;
}

$license = LicenseCreator::CreateLicense('Erika Mustermann', LicenseType::Commercial);
echo $license;
