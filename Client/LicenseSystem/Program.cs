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
