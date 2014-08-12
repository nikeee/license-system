using System;

namespace LicenseSystem
{
    class Program
    {
        static void Main(string[] args)
        {
            // Kann via TextBox genommen oder von irgendwo geladen werden.
            var someLicenseString =
@"--------BEGIN LICENSE--------
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
---------END LICENSE---------";

            License license;
            try
            {
                license = License.Parse(someLicenseString); // Achtung, wirft exception, wenn falsches Input.Format!
                // Hausaufgabe: TryParse implementieren
            }
            catch (FormatException)
            {
                Console.WriteLine("Die Lizenz hat ein ungültiges Format!");
                Console.ReadKey();
                return;
            }

            if (license.IsValid)
            {
                Console.WriteLine("Gültige Lizenz!");
                Console.WriteLine("Lizenztyp: " + license.Type);

                switch (license.Type)
                {
                    case LicenseType.SingleUser:
                        Console.WriteLine("Vielen Dank für das Verwenden einer Single-User-License.");
                        break;
                    case LicenseType.Commercial:
                        Console.WriteLine("Sie haben Zugriff auf ein SuperEnterpriseFeature9001.");
                        break;
                    case LicenseType.OpenSource:
                        Console.WriteLine("Du bist der Beste!");
                        break;
                }
            }
            else
            {
                Console.WriteLine("Ungültige Lizenz!");
                for (int i = 0; i < 42; ++i)
                    Console.Write("Kauf");
                Console.WriteLine("!");
            }


            Console.ReadKey();
        }
    }
}
