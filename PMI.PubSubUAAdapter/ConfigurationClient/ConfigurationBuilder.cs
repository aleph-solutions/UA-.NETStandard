using Newtonsoft.Json;
using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.CommonFunctions;
using Opc.Ua.Server;
using PubSubBase.Definitions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace PMI.PubSubUAAdapter.Configuration
{
    public class ConfigurationBuilder
    {
        Opc.Ua.Client.Session _browseSession;
        ConfigurationClient _configurationClient;
        Connection _mqttConnection;
        string _topicPrefix;

        public ConfigurationBuilder(Opc.Ua.Client.Session browseSession, IServerInternal pubSubServer)
        {
            _browseSession = browseSession;
            _configurationClient = new ConfigurationClient(pubSubServer);
            _configurationClient.InitializeClient();
            _configurationClient.InitializeMQTTConnection("MQTT", "40.91.255.161:1883", out _mqttConnection);
        }

        private void CertificateValidator_CertificateValidation(CertificateValidator sender, CertificateValidationEventArgs e)
        {
            e.Accept = true;
        }

        #region PubSub Configuration Methods
        public void Start()
        {
            //Browse the Objects folder
            var objFolderNodes = Browse(ObjectIds.ObjectsFolder);

            //Search for the DeviceSet folder
            var deviceSetExpNodeId = objFolderNodes.FirstOrDefault(x => x.BrowseName.Name == "DeviceSet" && x.NodeClass == NodeClass.Object)?.NodeId;

            if(deviceSetExpNodeId != null)
            {
                var deviceSetNodeId = ExpandedNodeId.ToNodeId(deviceSetExpNodeId, _browseSession.NamespaceUris);

                var browseRes = Browse(deviceSetNodeId);
                foreach (var node in browseRes.Where(x => x.NodeClass == NodeClass.Object))
                {
                    var typeId = GetTypeDefinition(node.NodeId);
                    if (typeId.Equals(ExpandedNodeId.ToNodeId(TMCPlus.ObjectTypeIds.PMI_MachineModuleType, _browseSession.NamespaceUris)))
                    {
                        ConfigureMachineModule(ExpandedNodeId.ToNodeId(node.NodeId, _browseSession.NamespaceUris), node.BrowseName.Name);
                    }
                }
            }
            else
            {
                Console.WriteLine("COnfigurationBuilder Start...DeviceSet object not found");
            }

        }


        public void ConfigureMachineModule(NodeId machineModuleId, string machineName)
        {
            //Search the NodeIds of the MachineModule sub object
            var configurationId = GetChildId(machineModuleId, "Configuration");
            var livestatusId = GetChildId(machineModuleId, "LiveStatus");
            var productionId = GetChildId(machineModuleId, "Production");
            var setupId = GetChildId(machineModuleId, "SetUp");
            var specificationId = GetChildId(machineModuleId, "Specification");

            var defectSensorsFolderId = GetChildId(machineModuleId, "DefectDetectionSensors");
            var materialBuffersFolderId = GetChildId(machineModuleId, "MaterialBuffers");
            var materialLoadingPointsFolderId = GetChildId(machineModuleId, "MaterialLoadingPoints");
            var materialOutputsFolderId = GetChildId(machineModuleId, "MaterialOutputs");
            var rejectionTrapsFolderId = GetChildId(machineModuleId, "MaterialRejectionTraps");
            var processControlLoopsFolderId = GetChildId(machineModuleId, "ProcessControlLoops");
            var processItemsFolderId = GetChildId(machineModuleId, "ProcessItems");


            //Configure the Objects
            ConfigureDefectSensors(defectSensorsFolderId, machineName);
            ConfigureMaterialStorageBuffers(materialBuffersFolderId, machineName);
            ConfigureMaterialLoadingPoints(materialLoadingPointsFolderId, machineName);
            ConfigureMaterialOutputs(materialOutputsFolderId, machineName);
            ConfigureMaterialRejectionTraps(rejectionTrapsFolderId, machineName);
            ConfigureProcessControlLoops(processControlLoopsFolderId, machineName);
            ConfigureProcessItems(processItemsFolderId, machineName);

            //Load the json configuration
            PubSubObjectConfiguration jsonConfig;
            var jsonFilename = @"AppData/Configuration.MachineModule.json";
            if (File.Exists(jsonFilename))
            {
                jsonConfig = JsonConvert.DeserializeObject<PubSubObjectConfiguration>(File.ReadAllText(jsonFilename));
            }
            else throw new FileNotFoundException($"JSON file {jsonFilename} not found.");

            //Prepare the Writer group
            _configurationClient.AddWriterGroup(_mqttConnection, $"{machineName}", $"{_topicPrefix}{machineName}", out DataSetWriterGroup writerGroup);
            _configurationClient.EnableWriterGroup(writerGroup.Name);

            //Prepare the dataset for the entire MachienModule
            var datasetItems = new Dictionary<string, NodeId>();

            #region LiveStatus
            var fields = Browse(livestatusId);
            foreach (var confField in jsonConfig.Fields.Where(x => x.FieldName.Split('.')[0] == "LiveStatus"))
            {
                var fieldNode = fields.FirstOrDefault(x => x.BrowseName.Name == confField.BrowseName);

                var fieldId = ExpandedNodeId.ToNodeId(fieldNode.NodeId, _browseSession.NamespaceUris);
                datasetItems.Add(confField.FieldName, fieldId);
            }


            #endregion

            #region Configuration
            fields = Browse(configurationId);
            foreach (var confField in jsonConfig.Fields.Where(x => x.FieldName.Split('.')[0] == "Configuration"))
            {
                var fieldNode = fields.FirstOrDefault(x => x.BrowseName.Name == confField.BrowseName);

                var fieldId = ExpandedNodeId.ToNodeId(fieldNode.NodeId, _browseSession.NamespaceUris);
                datasetItems.Add(confField.FieldName, fieldId);
            }


            #endregion

            //Add the dataset
            _configurationClient.AddPublishedDataSet(datasetItems, $"{machineName}", out PublishedDataSetBase publishedDataSet);

            //Prepare the writer
            _configurationClient.AddWriter(writerGroup, $"{machineName}", publishedDataSet.Name, $"{writerGroup.QueueName}", out DataSetWriterDefinition writer);
            _configurationClient.EnableWriter(writer.Name);
        }


        private void ConfigureDefectSensors(NodeId folderId, string machineName)
        {
            var references = Browse(folderId);

            foreach(var objItem in references.Where(x => x.NodeClass == NodeClass.Object))
            {
                var typeId = GetTypeDefinition(objItem.NodeId);
                if(typeId.Equals(ExpandedNodeId.ToNodeId(TMCPlus.ObjectTypeIds.PMI_DefectDetectionSensorType, _browseSession.NamespaceUris)))
                {
                    //Do things
                }
            }
        }

        private void ConfigureMaterialStorageBuffers(NodeId folderId, string machineName)
        {
            var references = Browse(folderId);

            foreach (var objItem in references.Where(x => x.NodeClass == NodeClass.Object))
            {
                var typeId = GetTypeDefinition(objItem.NodeId);
                if (typeId.Equals(ExpandedNodeId.ToNodeId(TMCPlus.ObjectTypeIds.PMI_MaterialStorageBufferType, _browseSession.NamespaceUris)))
                {
                    //Do things
                }
            }
        }

        private void ConfigureMaterialLoadingPoints(NodeId folderId, string machineName)
        {

           
        }

        private void ConfigureMaterialOutputs(NodeId folderId, string machineName)
        {
            var references = Browse(folderId);

            foreach (var objItem in references.Where(x => x.NodeClass == NodeClass.Object))
            {
                var typeId = GetTypeDefinition(objItem.NodeId);
                if (typeId.Equals(ExpandedNodeId.ToNodeId(TMCPlus.ObjectTypeIds.PMI_MaterialOutputType, _browseSession.NamespaceUris)))
                {
                    //Do things
                }
            }
        }

        private void ConfigureMaterialRejectionTraps(NodeId folderId, string machineName)
        {
            var references = Browse(folderId);

            foreach (var objItem in references.Where(x => x.NodeClass == NodeClass.Object))
            {
                var typeId = GetTypeDefinition(objItem.NodeId);
                if (typeId.Equals(ExpandedNodeId.ToNodeId(TMCPlus.ObjectTypeIds.PMI_MaterialRejectionTrapType, _browseSession.NamespaceUris)))
                {
                    //Do things
                }
            }
        }

        private void ConfigureProcessControlLoops(NodeId folderId, string machineName)
        {
            var references = Browse(folderId);

            foreach (var objItem in references.Where(x => x.NodeClass == NodeClass.Object))
            {
                var typeId = GetTypeDefinition(objItem.NodeId);
                if (typeId.Equals(ExpandedNodeId.ToNodeId(TMCPlus.ObjectTypeIds.PMI_ProcessControlLoopType, _browseSession.NamespaceUris)))
                {
                    //Do things
                }
            }
        }

        private void ConfigureProcessItems(NodeId folderId, string machineName)
        {
            PubSubObjectConfiguration jsonConfig;
            var jsonFilename = @"AppData/Configuration.ProcessItem.json";
            if (File.Exists(jsonFilename))
            {
                jsonConfig = JsonConvert.DeserializeObject<PubSubObjectConfiguration>(File.ReadAllText(jsonFilename));
            }
            else throw new FileNotFoundException($"JSON file {jsonFilename} not found.");

            //Prepare the Writer group
            _configurationClient.AddWriterGroup(_mqttConnection, $"{machineName}.ProcessItems", $"{_topicPrefix}{machineName}/ProcessItems", out DataSetWriterGroup writerGroup);
            _configurationClient.EnableWriterGroup(writerGroup.Name);

            var references = Browse(folderId);

            foreach (var objItem in references.Where(x => x.NodeClass == NodeClass.Object))
            {
                var typeId = GetTypeDefinition(objItem.NodeId);
                if (typeId.Equals(ExpandedNodeId.ToNodeId(TMCPlus.ObjectTypeIds.PMI_ProcessControlItemType, _browseSession.NamespaceUris)) ||
                    typeId.Equals(ExpandedNodeId.ToNodeId(TMCPlus.ObjectTypeIds.PMI_ProcessItemType, _browseSession.NamespaceUris)))
                {
                    var objectId = ExpandedNodeId.ToNodeId(objItem.NodeId, _browseSession.NamespaceUris);

                    //Prepare the Dataset
                    var datasetItems = InitializeItemList(objectId, typeId, out Dictionary<string, uint> datasetAttributes);

                    var fields = Browse(objItem.NodeId);
                    foreach (var confField in jsonConfig.Fields)
                    {
                        var fieldNode = fields.FirstOrDefault(x => x.BrowseName.Name == confField.BrowseName);

                        var fieldName = $"{objItem.BrowseName.Name}.{fieldNode.BrowseName.Name}";
                        var fieldId = ExpandedNodeId.ToNodeId(fieldNode.NodeId, _browseSession.NamespaceUris);
                        datasetItems.Add(fieldName, fieldId);
                    }

                    //Add the dataset
                    _configurationClient.AddPublishedDataSet(datasetItems, datasetAttributes, $"{machineName}.{objItem.BrowseName.Name}", out PublishedDataSetBase publishedDataSet);

                    //Prepare the writer
                    _configurationClient.AddWriter(writerGroup, $"{machineName}.ProcessItems.{objItem.BrowseName.Name}", publishedDataSet.Name, $"{writerGroup.QueueName}/{objItem.BrowseName.Name}", out DataSetWriterDefinition writer);
                    _configurationClient.EnableWriter(writer.Name);
                }
            }
        }
        #endregion


        #region Private Methods
        private Dictionary<string, NodeId> InitializeItemList(NodeId objectId, NodeId objectTypeId, out Dictionary<string, uint> attributesList)
        {
            var fieldList = new Dictionary<string, NodeId>();
            attributesList = new Dictionary<string, uint>();

            PubSubObjectConfiguration jsonConfig;
            var jsonFilename = @"AppData/Configuration.Base.json";
            if (File.Exists(jsonFilename))
            {
                jsonConfig = JsonConvert.DeserializeObject<PubSubObjectConfiguration>(File.ReadAllText(jsonFilename));
            }
            else throw new FileNotFoundException($"JSON file {jsonFilename} not found.");

            foreach(var field in jsonConfig.Fields)
            {
                var fieldId = objectId;
                if (field.BrowseName == "_type") fieldId = objectTypeId;

                fieldList.Add(field.FieldName, fieldId);
                attributesList.Add(field.FieldName, field.Attribute);
            }


            return fieldList;
        }

        private NodeId GetChildId(NodeId startNodeId, string childName)
        {
            return CommonFunctions.GetChildId(_browseSession, startNodeId, childName);
        }

        private List<ReferenceDescription> Browse(ExpandedNodeId startNodeId)
        {
            return Browse(ExpandedNodeId.ToNodeId(startNodeId, _browseSession.NamespaceUris));
        }

        private List<ReferenceDescription> Browse(NodeId startNodeId)
        {
            return CommonFunctions.Browse(_browseSession, startNodeId);
        }

        private NodeId GetTypeDefinition(ExpandedNodeId nodeId)
        {
            return CommonFunctions.GetTypeDefinition(_browseSession, ExpandedNodeId.ToNodeId(nodeId, _browseSession.NamespaceUris));
        }
        #endregion
    }

    [DataContract]
    class PubSubObjectConfiguration
    {
        [DataMember]
        public string Id { get; set; }

        [DataMember]
        public int PusblishInterval { get; set; }
        
        [DataMember]
        public IEnumerable<FieldDefinition> Fields { get; set; }
    }

    [DataContract]
    class FieldDefinition
    {
        [DataMember]
        public string FieldName { get; set; }

        [DataMember]
        public uint Attribute
        {
            get
            {
                if (_attribute != null) return (uint)_attribute;
                return Attributes.Value;
            }
            set
            { _attribute = value; }
        }
        private uint? _attribute;

        [DataMember]
        public string BrowseName 
        {
            get 
            {
                if (_browseName != null) return _browseName;
                return FieldName;
            }
            set 
            { 
                _browseName = value; 
            } 
        }
        private string _browseName;
    }
}
