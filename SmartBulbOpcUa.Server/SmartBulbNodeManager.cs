// SmartBulbOpcUa.Server/SmartBulbNodeManager.cs
using Opc.Ua;
using Opc.Ua.Server;
using SmartBulbOpcUa.Models;
using System;
using System.Collections.Generic;
using System.Threading;

namespace SmartBulbOpcUa.Server
{
    public class SmartBulbNodeManager : CustomNodeManager2
    {
        private readonly List<SmartBulbBase> _bulbs;
        private readonly Dictionary<NodeId, SmartBulbBase> _bulbNodes;
        private Timer? _updateTimer;

        public SmartBulbNodeManager(IServerInternal server, ApplicationConfiguration configuration)
            : base(server, configuration, Namespaces.SmartBulb)
        {
            _bulbs = new List<SmartBulbBase>();
            _bulbNodes = new Dictionary<NodeId, SmartBulbBase>();
            
            SystemContext.NodeIdFactory = this;
        }

        protected override NodeStateCollection LoadPredefinedNodes(ISystemContext context)
        {
            var predefinedNodes = new NodeStateCollection();
            
            // Crea l'Address Space
            CreateAddressSpace(predefinedNodes);
            
            return predefinedNodes;
        }

        private void CreateAddressSpace(NodeStateCollection predefinedNodes)
        {
            // Crea il folder root per i dispositivi
            var devicesFolder = CreateFolder(null!, "Devices", "Devices");
            predefinedNodes.Add(devicesFolder);

            // Inizializza le lampadine
            InitializeBulbs();

            // Crea i nodi per ogni lampadina
            foreach (var bulb in _bulbs)
            {
                CreateBulbNodes(devicesFolder, bulb, predefinedNodes);
            }

            // Avvia il timer per aggiornare i dati
            _updateTimer = new Timer(UpdateBulbData, null, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2));
        }

        private void InitializeBulbs()
        {
            // 1 Pro + 2 Standard come richiesto
            _bulbs.Add(new SmartBulbPro("PRO_001", "Smart Bulb Pro 001"));
            _bulbs.Add(new SmartBulbBase("STD_001", "Smart Bulb Standard 001"));
            _bulbs.Add(new SmartBulbBase("STD_002", "Smart Bulb Standard 002"));

            // Accendi una lampadina per test
            _bulbs[0].TurnOn();
            if (_bulbs[0] is SmartBulbPro proBulb)
            {
                proBulb.SetBrightness(75);
            }
        }

        private void CreateBulbNodes(NodeState parent, SmartBulbBase bulb, NodeStateCollection predefinedNodes)
        {
            // Folder per la singola lampadina
            var bulbFolder = CreateFolder(parent, bulb.Id, bulb.Name);
            predefinedNodes.Add(bulbFolder);

            // Variabile State
            var stateVar = CreateVariable(bulbFolder, $"{bulb.Id}_State", "State", 
                DataTypeIds.String, ValueRanks.Scalar);
            stateVar.Value = bulb.State.ToString();
            predefinedNodes.Add(stateVar);

            // Variabile Temperature
            var tempVar = CreateVariable(bulbFolder, $"{bulb.Id}_Temperature", "Temperature", 
                DataTypeIds.Double, ValueRanks.Scalar);
            tempVar.Value = bulb.Temperature;
            predefinedNodes.Add(tempVar);

            // Metodi TurnOn/TurnOff
            var turnOnMethod = CreateMethod(bulbFolder, $"{bulb.Id}_TurnOn", "TurnOn");
            turnOnMethod.OnCallMethod = (context, method, inputArguments, outputArguments) =>
            {
                bulb.TurnOn();
                return ServiceResult.Good;
            };
            predefinedNodes.Add(turnOnMethod);

            var turnOffMethod = CreateMethod(bulbFolder, $"{bulb.Id}_TurnOff", "TurnOff");
            turnOffMethod.OnCallMethod = (context, method, inputArguments, outputArguments) =>
            {
                bulb.TurnOff();
                return ServiceResult.Good;
            };
            predefinedNodes.Add(turnOffMethod);

            // Se è Pro, aggiungi Brightness
            if (bulb is SmartBulbPro proBulb)
            {
                var brightnessVar = CreateVariable(bulbFolder, $"{bulb.Id}_Brightness", "Brightness", 
                    DataTypeIds.Int32, ValueRanks.Scalar);
                brightnessVar.Value = proBulb.Brightness;
                predefinedNodes.Add(brightnessVar);

                // Metodo SetBrightness
                var setBrightnessMethod = CreateMethod(bulbFolder, $"{bulb.Id}_SetBrightness", "SetBrightness");
                setBrightnessMethod.InputArguments = new PropertyState<Argument[]>(setBrightnessMethod)
                {
                    NodeId = new NodeId($"{bulb.Id}_SetBrightness_InputArgs", NamespaceIndex),
                    BrowseName = BrowseNames.InputArguments,
                    DisplayName = new LocalizedText("en", BrowseNames.InputArguments),
                    TypeDefinitionId = VariableTypeIds.PropertyType,
                    ReferenceTypeId = ReferenceTypeIds.HasProperty,
                    DataType = DataTypeIds.Argument,
                    ValueRank = ValueRanks.OneDimension,
                    Value = new Argument[]
                    {
                        new Argument { Name = "Level", Description = "Brightness level (0-100)", 
                                     DataType = DataTypeIds.Int32, ValueRank = ValueRanks.Scalar }
                    }
                };

                setBrightnessMethod.OnCallMethod = (context, method, inputArguments, outputArguments) =>
                {
                    if (inputArguments?.Count > 0 && inputArguments[0] is Variant variant && variant.Value is int level)
                    {
                        try
                        {
                            proBulb.SetBrightness(level);
                            return ServiceResult.Good;
                        }
                        catch (ArgumentOutOfRangeException)
                        {
                            return StatusCodes.BadOutOfRange;
                        }
                    }
                    return StatusCodes.BadInvalidArgument;
                };
                predefinedNodes.Add(setBrightnessMethod);
            }

            // Memorizza i nodi per gli aggiornamenti
            _bulbNodes[stateVar.NodeId] = bulb;
            _bulbNodes[tempVar.NodeId] = bulb;
            if (bulb is SmartBulbPro)
            {
                var brightnessNodeId = new NodeId($"{bulb.Id}_Brightness", NamespaceIndex);
                _bulbNodes[brightnessNodeId] = bulb;
            }
        }

        private void UpdateBulbData(object? state)
        {
            try
            {
                lock (Lock)
                {
                    foreach (var bulb in _bulbs)
                    {
                        bulb.UpdateTemperature();
                        
                        // Aggiorna i nodi OPC UA
                        UpdateNodeValue($"{bulb.Id}_State", bulb.State.ToString());
                        UpdateNodeValue($"{bulb.Id}_Temperature", bulb.Temperature);
                        
                        if (bulb is SmartBulbPro proBulb)
                        {
                            UpdateNodeValue($"{bulb.Id}_Brightness", proBulb.Brightness);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Utils.Trace("Error updating bulb data: {0}", ex.Message);
            }
        }

        private void UpdateNodeValue(string nodeId, object value)
        {
            var node = FindPredefinedNode(new NodeId(nodeId, NamespaceIndex), typeof(BaseDataVariableState));
            if (node is BaseDataVariableState variable)
            {
                variable.Value = value;
                variable.Timestamp = DateTime.UtcNow;
                variable.ClearChangeMasks(SystemContext, false);
            }
        }

        // Metodi di utilità per creare nodi
        private FolderState CreateFolder(NodeState parent, string path, string name)
        {
            var folder = new FolderState(parent)
            {
                SymbolicName = name,
                ReferenceTypeId = ReferenceTypeIds.Organizes,
                TypeDefinitionId = ObjectTypeIds.FolderType,
                NodeId = new NodeId(path, NamespaceIndex),
                BrowseName = new QualifiedName(name, NamespaceIndex),
                DisplayName = new LocalizedText("en", name),
                WriteMask = AttributeWriteMask.None,
                UserWriteMask = AttributeWriteMask.None,
                EventNotifier = EventNotifiers.None
            };

            parent?.AddChild(folder);
            return folder;
        }

        private BaseDataVariableState CreateVariable(NodeState parent, string path, string name, 
            NodeId dataType, int valueRank)
        {
            var variable = new BaseDataVariableState(parent)
            {
                SymbolicName = name,
                ReferenceTypeId = ReferenceTypeIds.Organizes,
                TypeDefinitionId = VariableTypeIds.BaseDataVariableType,
                NodeId = new NodeId(path, NamespaceIndex),
                BrowseName = new QualifiedName(name, NamespaceIndex),
                DisplayName = new LocalizedText("en", name),
                WriteMask = AttributeWriteMask.DisplayName | AttributeWriteMask.Description,
                UserWriteMask = AttributeWriteMask.DisplayName | AttributeWriteMask.Description,
                DataType = dataType,
                ValueRank = valueRank,
                AccessLevel = AccessLevels.CurrentReadOrWrite,
                UserAccessLevel = AccessLevels.CurrentReadOrWrite,
                Historizing = false
            };

            parent?.AddChild(variable);
            return variable;
        }

        private MethodState CreateMethod(NodeState parent, string path, string name)
        {
            var method = new MethodState(parent)
            {
                SymbolicName = name,
                ReferenceTypeId = ReferenceTypeIds.HasComponent,
                NodeId = new NodeId(path, NamespaceIndex),
                BrowseName = new QualifiedName(name, NamespaceIndex),
                DisplayName = new LocalizedText("en", name),
                WriteMask = AttributeWriteMask.None,
                UserWriteMask = AttributeWriteMask.None,
                Executable = true,
                UserExecutable = true
            };

            parent?.AddChild(method);
            return method;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _updateTimer?.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    public static class Namespaces
    {
        public const string SmartBulb = "http://smartbulb.opcua.example/";
    }
}