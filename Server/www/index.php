<?php

require_once 'LicenseCreator.php';

header('Content-Type: text/plain; charset=utf-8');

$licensee = 'Erika Mustermann';
$licenseType = LicenseType::Commercial;
$license = LicenseCreator::CreateLicense($licensee, $licenseType);

echo "Lizenznehmer: $licensee\n";
echo "Lizenztyp: $licenseType\n";
echo "Bitte fügen Sie folgende Daten in Ihre Anwendung ein:\n\n";

echo $license;