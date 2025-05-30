using Opc.Ua;
using Opc.Ua.Configuration;
using System;
using System.Threading.Tasks;

namespace SmartBulbOpcUa.Server
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Smart Bulb OPC UA Server");
            Console.WriteLine("========================");

            try
            {
                // Crea e avvia il server
                var server = new SmartBulbServer();
                await server.StartAsync();

                Console.WriteLine("Server avviato. Premere qualsiasi tasto per terminare...");
                Console.ReadKey();

                server.StopAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                Console.WriteLine("Premere qualsiasi tasto per uscire...");
                Console.ReadKey();
            }
        }
    }
}