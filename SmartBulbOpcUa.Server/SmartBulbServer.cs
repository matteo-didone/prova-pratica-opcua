using Opc.Ua;
using Opc.Ua.Configuration;
using Opc.Ua.Server;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartBulbOpcUa.Server
{
    public class SmartBulbServer
    {
        private ApplicationInstance? _application;

        public async Task StartAsync()
        {
            Console.WriteLine("Configurazione del server OPC UA...");

            // Crea la configurazione programmaticamente (senza file esterni)
            var config = CreateDefaultConfiguration();

            // Configurazione dell'applicazione
            _application = new ApplicationInstance(config);

            // Controlla i certificati
            try
            {
                bool certOk = await _application.CheckApplicationInstanceCertificate(false, 0);
                if (!certOk)
                {
                    Console.WriteLine("Creazione certificati auto-firmati...");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore certificati: {ex.Message} - continuando...");
            }

            // Crea il server con il nostro NodeManager
            var server = new SmartBulbOpcServer();

            // Avvia il server
            await _application.Start(server);

            Console.WriteLine($"Server avviato su: {config.ServerConfiguration.BaseAddresses[0]}");
            Console.WriteLine("Namespace URI: " + Namespaces.SmartBulb);
            Console.WriteLine();
            Console.WriteLine("Dispositivi disponibili:");
            Console.WriteLine("- Smart Bulb Pro 001 (con dimmer)");
            Console.WriteLine("- Smart Bulb Standard 001");
            Console.WriteLine("- Smart Bulb Standard 002");
            Console.WriteLine();
        }

        public void StopAsync()
        {
            Console.WriteLine("Arresto del server...");
            
            if (_application != null)
            {
                _application.Stop();
            }

            Console.WriteLine("Server arrestato.");
        }

        private ApplicationConfiguration CreateDefaultConfiguration()
        {
            var config = new ApplicationConfiguration
            {
                ApplicationName = "Smart Bulb OPC UA Server",
                ApplicationType = ApplicationType.Server,
                ApplicationUri = "urn:localhost:SmartBulbServer",
                ProductUri = "http://smartbulb.opcua.example/",

                ServerConfiguration = new ServerConfiguration
                {
                    BaseAddresses = { "opc.tcp://localhost:4841/SmartBulbServer" },
                    SecurityPolicies = new ServerSecurityPolicyCollection
                    {
                        new ServerSecurityPolicy
                        {
                            SecurityMode = MessageSecurityMode.None,
                            SecurityPolicyUri = SecurityPolicies.None
                        }
                    },
                    UserTokenPolicies = new UserTokenPolicyCollection
                    {
                        new UserTokenPolicy
                        {
                            TokenType = UserTokenType.Anonymous,
                            PolicyId = "Anonymous"
                        }
                    },
                    DiagnosticsEnabled = true,
                    MaxSessionCount = 100,
                    MinSessionTimeout = 10000,
                    MaxSessionTimeout = 3600000
                },

                SecurityConfiguration = new SecurityConfiguration
                {
                    ApplicationCertificate = new CertificateIdentifier
                    {
                        StoreType = "Directory",
                        StorePath = "OPC Foundation/CertificateStores/MachineDefault",
                        SubjectName = "CN=Smart Bulb OPC UA Server, DC=localhost"
                    },
                    AutoAcceptUntrustedCertificates = true
                },

                TransportQuotas = new TransportQuotas
                {
                    OperationTimeout = 120000,
                    MaxStringLength = 1048576,
                    MaxByteStringLength = 1048576,
                    MaxArrayLength = 65535,
                    MaxMessageSize = 4194304,
                    MaxBufferSize = 65535,
                    ChannelLifetime = 300000,
                    SecurityTokenLifetime = 3600000
                }
            };

            config.Validate(ApplicationType.Server);
            return config;
        }
    }

    // Classe server personalizzata che estende StandardServer
    public class SmartBulbOpcServer : StandardServer
    {
        protected override MasterNodeManager CreateMasterNodeManager(IServerInternal server, ApplicationConfiguration configuration)
        {
            Console.WriteLine("Creazione NodeManager...");

            var nodeManagers = new List<INodeManager>
            {
                new CoreNodeManager(server, configuration, 0),
                new SmartBulbNodeManager(server, configuration)
            };

            return new MasterNodeManager(server, configuration, null, nodeManagers.ToArray());
        }

        protected override ServerProperties LoadServerProperties()
        {
            var properties = new ServerProperties
            {
                ManufacturerName = "Smart Bulb Company",
                ProductName = "Smart Bulb OPC UA Server",
                ProductUri = "http://smartbulb.opcua.example/",
                SoftwareVersion = "1.0.0",
                BuildNumber = "1",
                BuildDate = DateTime.Now
            };

            return properties;
        }
    }
}