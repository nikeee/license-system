<?php

require_once 'LicenseCreator.php';

header('Content-Type: text/plain');

$licensee = 'Erika Mustermann';
$licenseType = LicenseType::Commercial;
$license = LicenseCreator::CreateLicense($licensee, $licenseType);

echo "Lizenznehmer: $licensee\n";
echo "Lizenztyp: $licenseType\n";
echo "Bitte fügen Sie folgende Daten in Ihre Anwendung ein:\n";

echo $license;
