// SmartBulbOpcUa.Client/Program.cs
using System;
using System.Threading.Tasks;

namespace SmartBulbOpcUa.Client
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Smart Bulb OPC UA Client - Discovery Dinamico");
            Console.WriteLine("=============================================");

            try
            {
                var client = new SmartBulbClient();
                await client.ConnectAsync();
                
                // Lettura e scoperta dinamica della struttura
                client.ReadBulbStatesDynamic();
                
                // Mostra la struttura scoperta
                client.ShowDiscoveredStructure();
                
                // Test dei metodi dinamici
                await client.TestMethodsDynamic();
                
                // Lettura stato finale
                Console.WriteLine("\n=== STATO FINALE ===");
                client.ReadBulbStatesDynamic();
                
                // Opzionale: monitoraggio in tempo reale
                Console.WriteLine("\nVuoi monitorare i cambiamenti in tempo reale? (y/n)");
                var key = Console.ReadKey();
                if (key.KeyChar == 'y' || key.KeyChar == 'Y')
                {
                    await client.MonitorChangesDynamic();
                }
                
                client.Disconnect();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }

            Console.WriteLine("\nPremere qualsiasi tasto per uscire...");
            Console.ReadKey();
        }
    }
}