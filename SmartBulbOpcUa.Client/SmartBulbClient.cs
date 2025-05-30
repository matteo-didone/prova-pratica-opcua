// SmartBulbOpcUa.Client/SmartBulbClient.cs
using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Configuration;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace SmartBulbOpcUa.Client
{
    public class SmartBulbClient
    {
        private const string ServerUrl = "opc.tcp://localhost:4841/SmartBulbServer";
        private Session? _session;
        private ApplicationConfiguration? _config;

        // Mappa dei NodeId scoperta dinamicamente
        private readonly Dictionary<string, Dictionary<string, NodeId>> _knownNodeIds = new()
        {
            ["Smart Bulb Pro 001"] = new Dictionary<string, NodeId>(),
            ["Smart Bulb Standard 001"] = new Dictionary<string, NodeId>(),
            ["Smart Bulb Standard 002"] = new Dictionary<string, NodeId>()
        };

        public async Task ConnectAsync()
        {
            Console.WriteLine("Connessione al server OPC UA...");

            // Configurazione minimale
            _config = new ApplicationConfiguration
            {
                ApplicationName = "Smart Bulb Client",
                ApplicationType = ApplicationType.Client,
                ApplicationUri = "urn:SmartBulbClient",
                
                SecurityConfiguration = new SecurityConfiguration
                {
                    AutoAcceptUntrustedCertificates = true
                },
                
                ClientConfiguration = new ClientConfiguration
                {
                    DefaultSessionTimeout = 60000
                },

                TransportQuotas = new TransportQuotas
                {
                    OperationTimeout = 15000,
                    MaxStringLength = 1048576,
                    MaxByteStringLength = 1048576,
                    MaxArrayLength = 65535,
                    MaxMessageSize = 4194304,
                    MaxBufferSize = 65535,
                    ChannelLifetime = 300000,
                    SecurityTokenLifetime = 3600000
                }
            };

            _config.Validate(ApplicationType.Client);

            // Usa DiscoveryClient per trovare gli endpoint
            EndpointDescriptionCollection endpoints;
            using (var discoveryClient = DiscoveryClient.Create(new Uri(ServerUrl)))
            {
                endpoints = discoveryClient.GetEndpoints(null);
            }
            
            // Seleziona il primo endpoint None (senza sicurezza)
            var endpoint = endpoints.FirstOrDefault(e => e.SecurityMode == MessageSecurityMode.None);
            
            if (endpoint == null)
            {
                throw new Exception("Nessun endpoint senza sicurezza trovato!");
            }

            var configuredEndpoint = new ConfiguredEndpoint(null, endpoint, EndpointConfiguration.Create(_config));

            // Crea la sessione
            _session = await Session.Create(
                _config,
                configuredEndpoint,
                false,
                "SmartBulbClientSession",
                60000,
                new UserIdentity(new AnonymousIdentityToken()),
                null);

            Console.WriteLine($"Connesso al server: {ServerUrl}");
            Console.WriteLine($"Session ID: {_session.SessionId}");
            Console.WriteLine();

            // Scopri i NodeId dinamicamente
            await Task.Run(() => DiscoverNodeIds());
        }

        private void DiscoverNodeIds()
        {
            Console.WriteLine("Scoperta dinamica dei NodeId...");
            
            // Prova diversi namespace index (2, 3, 4)
            var namespaceIndices = new ushort[] { 2, 3, 4 };
            var bulbIds = new[] { "PRO_001", "STD_001", "STD_002" };
            var bulbNames = new[] { "Smart Bulb Pro 001", "Smart Bulb Standard 001", "Smart Bulb Standard 002" };
            
            for (int i = 0; i < bulbIds.Length; i++)
            {
                var bulbId = bulbIds[i];
                var bulbName = bulbNames[i];
                
                Console.WriteLine($"Scoperta nodi per {bulbName}...");
                
                foreach (var nsIndex in namespaceIndices)
                {
                    try
                    {
                        // Prova a leggere State per verificare il namespace corretto
                        var testStateNodeId = new NodeId($"{bulbId}_State", (ushort)nsIndex);
                        var testValue = _session!.ReadValue(testStateNodeId);
                        
                        if (testValue != null && testValue.StatusCode == StatusCodes.Good)
                        {
                            Console.WriteLine($"  Trovato namespace {nsIndex} per {bulbName}");
                            
                            // Aggiungi tutti i nodi noti
                            _knownNodeIds[bulbName]["State"] = new NodeId($"{bulbId}_State", (ushort)nsIndex);
                            _knownNodeIds[bulbName]["Temperature"] = new NodeId($"{bulbId}_Temperature", (ushort)nsIndex);
                            _knownNodeIds[bulbName]["TurnOn"] = new NodeId($"{bulbId}_TurnOn", (ushort)nsIndex);
                            _knownNodeIds[bulbName]["TurnOff"] = new NodeId($"{bulbId}_TurnOff", (ushort)nsIndex);
                            
                            // Se è Pro, aggiungi anche Brightness
                            if (bulbId == "PRO_001")
                            {
                                _knownNodeIds[bulbName]["Brightness"] = new NodeId($"{bulbId}_Brightness", (ushort)nsIndex);
                                _knownNodeIds[bulbName]["SetBrightness"] = new NodeId($"{bulbId}_SetBrightness", (ushort)nsIndex);
                            }
                            
                            break; // Namespace trovato, passa alla prossima lampadina
                        }
                    }
                    catch
                    {
                        // Prova il prossimo namespace
                        continue;
                    }
                }
            }
            
            // Mostra cosa è stato trovato
            foreach (var bulb in _knownNodeIds)
            {
                Console.WriteLine($"{bulb.Key}: {bulb.Value.Count} nodi trovati");
            }
        }

        public void ReadBulbStatesDynamic()
        {
            Console.WriteLine("=== LETTURA DINAMICA DELLE LAMPADINE ===");
            
            foreach (var bulb in _knownNodeIds)
            {
                var bulbName = bulb.Key;
                var nodes = bulb.Value;
                
                if (nodes.Count == 0)
                {
                    Console.WriteLine($"\n{bulbName}: Nodi non trovati!");
                    continue;
                }
                
                Console.WriteLine($"\n{bulbName}:");
                Console.WriteLine(new string('-', bulbName.Length + 1));
                
                try
                {
                    // Leggi State se esiste
                    if (nodes.ContainsKey("State"))
                    {
                        var stateValue = _session!.ReadValue(nodes["State"]);
                        Console.WriteLine($"  Stato: {stateValue.Value}");
                    }
                    
                    // Leggi Temperature se esiste
                    if (nodes.ContainsKey("Temperature"))
                    {
                        var tempValue = _session!.ReadValue(nodes["Temperature"]);
                        Console.WriteLine($"  Temperatura: {tempValue.Value:F1}°C");
                    }
                    
                    // Leggi Brightness se esiste (solo Pro)
                    if (nodes.ContainsKey("Brightness"))
                    {
                        var brightnessValue = _session!.ReadValue(nodes["Brightness"]);
                        Console.WriteLine($"  Luminosità: {brightnessValue.Value}%");
                    }
                    
                    // Mostra metodi disponibili
                    var methods = nodes.Where(n => n.Key.Contains("Turn") || n.Key.Contains("Set")).ToList();
                    if (methods.Any())
                    {
                        Console.WriteLine($"  Metodi: {string.Join(", ", methods.Select(m => m.Key))}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  Errore nella lettura: {ex.Message}");
                }
            }
        }

        public async Task TestMethodsDynamic()
        {
            Console.WriteLine("\n=== TEST DINAMICO DEI METODI ===");
            
            try
            {
                // Trova la lampadina Pro (quella con Brightness)
                var proBulb = _knownNodeIds.FirstOrDefault(b => b.Value.ContainsKey("Brightness"));
                if (proBulb.Key != null)
                {
                    var proBulbName = proBulb.Key;
                    var proNodes = proBulb.Value;
                    
                    Console.WriteLine($"\n--- TEST SU {proBulbName} ---");
                    
                    // Test TurnOff
                    if (proNodes.ContainsKey("TurnOff"))
                    {
                        Console.WriteLine($"Spegnimento {proBulbName}...");
                        var bulbObjectId = new NodeId("PRO_001", (ushort)2);
                        var result = _session!.Call(bulbObjectId, proNodes["TurnOff"], new object[0]); // Array vuoto
                        Console.WriteLine($"Risultato TurnOff: Successo");
                        
                        await Task.Delay(1000);
                        
                        // Leggi nuovo stato
                        if (proNodes.ContainsKey("State"))
                        {
                            var newState = _session.ReadValue(proNodes["State"]);
                            Console.WriteLine($"Nuovo stato: {newState.Value}");
                        }
                    }
                    
                    // Test SetBrightness
                    if (proNodes.ContainsKey("SetBrightness"))
                    {
                        Console.WriteLine($"Impostazione luminosità a 30%...");
                        var bulbObjectId = new NodeId("PRO_001", (ushort)2);
                        var result = _session!.Call(bulbObjectId, proNodes["SetBrightness"], new object[] { 30 });
                        Console.WriteLine($"Risultato SetBrightness: Successo");
                        
                        await Task.Delay(1000);
                        
                        // Verifica cambiamenti
                        if (proNodes.ContainsKey("Brightness") && proNodes.ContainsKey("State"))
                        {
                            var brightness = _session.ReadValue(proNodes["Brightness"]);
                            var state = _session.ReadValue(proNodes["State"]);
                            Console.WriteLine($"Nuova luminosità: {brightness.Value}%");
                            Console.WriteLine($"Stato dopo impostazione: {state.Value}");
                        }
                    }
                }
                else
                {
                    Console.WriteLine("Nessuna lampadina Pro trovata!");
                }
                
                // Test su lampadina Standard
                var standardBulb = _knownNodeIds.FirstOrDefault(b => !b.Value.ContainsKey("Brightness") && b.Value.Count > 0);
                if (standardBulb.Key != null)
                {
                    var stdBulbName = standardBulb.Key;
                    var stdNodes = standardBulb.Value;
                    
                    Console.WriteLine($"\n--- TEST SU {stdBulbName} ---");
                    
                    if (stdNodes.ContainsKey("TurnOn"))
                    {
                        Console.WriteLine($"Accensione {stdBulbName}...");
                        var bulbObjectId = new NodeId("STD_001", (ushort)2);
                        var result = _session!.Call(bulbObjectId, stdNodes["TurnOn"], new object[0]);
                        Console.WriteLine($"Risultato TurnOn: Successo");
                        
                        await Task.Delay(1000);
                        
                        if (stdNodes.ContainsKey("State"))
                        {
                            var stdState = _session.ReadValue(stdNodes["State"]);
                            Console.WriteLine($"Stato {stdBulbName}: {stdState.Value}");
                        }
                    }
                }
                else
                {
                    Console.WriteLine("Nessuna lampadina Standard trovata!");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore durante il test dei metodi: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        public async Task MonitorChangesDynamic()
        {
            Console.WriteLine("\n=== MONITORAGGIO DINAMICO (10 secondi) ===");
            
            try
            {
                // Crea una subscription
                var subscription = new Subscription(_session!.DefaultSubscription)
                {
                    PublishingInterval = 1000,
                    LifetimeCount = 10000,
                    KeepAliveCount = 10,
                    MaxNotificationsPerPublish = 1000,
                    Priority = 0
                };

                // Aggiungi item da monitorare dinamicamente
                var monitoredItems = new List<MonitoredItem>();

                foreach (var bulb in _knownNodeIds)
                {
                    var bulbName = bulb.Key;
                    var nodes = bulb.Value;
                    
                    if (nodes.Count == 0) continue;
                    
                    // Monitora State se esiste
                    if (nodes.ContainsKey("State"))
                    {
                        var stateItem = new MonitoredItem(subscription.DefaultItem)
                        {
                            DisplayName = $"{bulbName}_State",
                            StartNodeId = nodes["State"],
                            AttributeId = Attributes.Value,
                            SamplingInterval = 1000,
                            QueueSize = 0,
                            DiscardOldest = true
                        };
                        stateItem.Notification += OnNotification;
                        monitoredItems.Add(stateItem);
                    }

                    // Monitora Temperature se esiste
                    if (nodes.ContainsKey("Temperature"))
                    {
                        var tempItem = new MonitoredItem(subscription.DefaultItem)
                        {
                            DisplayName = $"{bulbName}_Temperature",
                            StartNodeId = nodes["Temperature"],
                            AttributeId = Attributes.Value,
                            SamplingInterval = 1000,
                            QueueSize = 0,
                            DiscardOldest = true
                        };
                        tempItem.Notification += OnNotification;
                        monitoredItems.Add(tempItem);
                    }
                    
                    // Monitora Brightness se esiste
                    if (nodes.ContainsKey("Brightness"))
                    {
                        var brightnessItem = new MonitoredItem(subscription.DefaultItem)
                        {
                            DisplayName = $"{bulbName}_Brightness",
                            StartNodeId = nodes["Brightness"],
                            AttributeId = Attributes.Value,
                            SamplingInterval = 1000,
                            QueueSize = 0,
                            DiscardOldest = true
                        };
                        brightnessItem.Notification += OnNotification;
                        monitoredItems.Add(brightnessItem);
                    }
                }

                if (monitoredItems.Count == 0)
                {
                    Console.WriteLine("Nessun nodo trovato per il monitoraggio!");
                    return;
                }

                subscription.AddItems(monitoredItems);
                _session.AddSubscription(subscription);
                subscription.Create();

                Console.WriteLine($"Monitoraggio {monitoredItems.Count} nodi per 10 secondi...");

                // Monitora per 10 secondi
                await Task.Delay(10000);

                subscription.Delete(true);
                _session.RemoveSubscription(subscription);
                Console.WriteLine("Monitoraggio terminato.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore durante il monitoraggio: {ex.Message}");
            }
        }

        private void OnNotification(MonitoredItem item, MonitoredItemNotificationEventArgs e)
        {
            foreach (var value in item.DequeueValues())
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {item.DisplayName}: {value.Value}");
            }
        }

        public void ShowDiscoveredStructure()
        {
            Console.WriteLine("\n=== STRUTTURA SCOPERTA ===");
            foreach (var bulb in _knownNodeIds)
            {
                Console.WriteLine($"\nLampadina: {bulb.Key}");
                if (bulb.Value.Count == 0)
                {
                    Console.WriteLine("  Nessun nodo trovato!");
                }
                else
                {
                    foreach (var node in bulb.Value)
                    {
                        Console.WriteLine($"  {node.Key}: {node.Value}");
                    }
                }
            }
        }

        public void Disconnect()
        {
            try
            {
                Console.WriteLine("\nDisconnessione...");
                _session?.Close();
                _session?.Dispose();
                Console.WriteLine("Disconnesso dal server.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore durante la disconnessione: {ex.Message}");
            }
        }
    }
}