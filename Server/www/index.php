<?php

require_once 'LicenseCreator.php';

header('Content-Type: text/plain');

$licensee = 'Erika Mustermann';
$licenseType = LicenseType::Commercial;
$license = LicenseCreator::CreateLicense($licensee, $licenseType);

echo "Lizenznehmer: $licensee\n";
echo "Lizenztyp: $licenseType\n";
echo "Bitte fügen Sie folgende Daten in Ihre Anwendung ein:\n\n";

echo $license;

echo "\n\n(die Lizenz kann sich beim Neuladen der Seite ändern, ist aber trotzdem gültig.)";