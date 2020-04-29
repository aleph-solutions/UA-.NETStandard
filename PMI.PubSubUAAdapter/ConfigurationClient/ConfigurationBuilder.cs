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
        string _pathPrefix = "DeviceSet";

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
            //Initialize the encodable types in order to be correctly serialized in the json
            InitializeEncodableTypes();

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
                Console.WriteLine("ConfigurationBuilder Start...DeviceSet object not found");
            }
            _configurationClient.EnableAllWriters();
        }


        public void ConfigureMachineModule(NodeId machineModuleId, string machineName)
        {
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

            //Prepare the Writer group
            _configurationClient.AddWriterGroup(_mqttConnection, $"{machineName}", $"{_topicPrefix}{machineName}", out DataSetWriterGroup writerGroup);
            _configurationClient.EnableWriterGroup(writerGroup.Name);
          
            //Prepare the dataset for the entire MachienModule
            var typeId = GetTypeDefinition(machineModuleId);
            var datasetItems = InitializeItemList(machineModuleId, typeId, out Dictionary<string, uint> datasetAttributes);
            LoadItemList(datasetItems, "MachineModule", machineModuleId);


            //Add the dataset and the extensionFields
            _configurationClient.AddPublishedDataSet(datasetItems, $"{machineName}", out PublishedDataSetBase publishedDataSet);
            _configurationClient.AddExtensionField(publishedDataSet, "DataSetName", $"{_pathPrefix}/{publishedDataSet.Name.Replace('.', '/')}");

            //Prepare the writer
            _configurationClient.AddWriter(writerGroup, $"{machineName}", publishedDataSet.Name, $"{writerGroup.QueueName}", out DataSetWriterDefinition writer);
            //_configurationClient.EnableWriter(writer.Name);

        }

        private void InitializeEncodableTypes()
        {
            EncodeableFactory.GlobalFactory.AddEncodeableType(typeof(TMCGroup.TMC.DataSetListType));
            EncodeableFactory.GlobalFactory.AddEncodeableType(typeof(TMCGroup.TMC.DataDescriptionType));
            EncodeableFactory.GlobalFactory.AddEncodeableType(typeof(TMCGroup.TMC.RootCauseGroupType));
            EncodeableFactory.GlobalFactory.AddEncodeableType(typeof(TMCGroup.TMC.RootCauseMessageType));
            EncodeableFactory.GlobalFactory.AddEncodeableType(typeof(TMCGroup.TMC.MessageType));
            EncodeableFactory.GlobalFactory.AddEncodeableType(typeof(TMCGroup.TMC.POType));
            EncodeableFactory.GlobalFactory.AddEncodeableType(typeof(TMCPlus.PMI_BoMType));
            EncodeableFactory.GlobalFactory.AddEncodeableType(typeof(TMCPlus.PMI_BoMEntryType));
            EncodeableFactory.GlobalFactory.AddEncodeableType(typeof(TMCGroup.TMC.DataSetType));
            EncodeableFactory.GlobalFactory.AddEncodeableType(typeof(TMCGroup.TMC.DataSetEntryType));
            EncodeableFactory.GlobalFactory.AddEncodeableType(typeof(TMCGroup.TMC.LoadUnloadPointType));
            EncodeableFactory.GlobalFactory.AddEncodeableType(typeof(TMCGroup.TMC.MaterialStorageBufferDataType));
            EncodeableFactory.GlobalFactory.AddEncodeableType(typeof(TMCGroup.TMC.MaterialType));
            EncodeableFactory.GlobalFactory.AddEncodeableType(typeof(TMCPlus.PMI_MaterialAttributesType));
            EncodeableFactory.GlobalFactory.AddEncodeableType(typeof(TMCPlus.PMI_MaterialLotAttributesType));
            EncodeableFactory.GlobalFactory.AddEncodeableType(typeof(TMCPlus.PMI_MaterialQualityType));
            EncodeableFactory.GlobalFactory.AddEncodeableType(typeof(TMCPlus.PMI_MaterialStateEnumeration));
        }


        private void ConfigureDefectSensors(NodeId folderId, string machineName)
        {
            //Prepare the Writer group
            _configurationClient.AddWriterGroup(_mqttConnection, $"{machineName}.DefectDetectionSensors", $"{_topicPrefix}{machineName}/DefectDetectionSensors", out DataSetWriterGroup writerGroup);
            _configurationClient.EnableWriterGroup(writerGroup.Name);

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
            //Prepare the Writer group
            _configurationClient.AddWriterGroup(_mqttConnection, $"{machineName}.MaterialBuffers", $"{_topicPrefix}{machineName}/MaterialBuffers", out DataSetWriterGroup writerGroup);
            _configurationClient.EnableWriterGroup(writerGroup.Name);

            var references = Browse(folderId);

            foreach (var objItem in references.Where(x => x.NodeClass == NodeClass.Object))
            {
                var typeId = GetTypeDefinition(objItem.NodeId);
                if (typeId.Equals(ExpandedNodeId.ToNodeId(TMCPlus.ObjectTypeIds.PMI_MaterialStorageBufferType, _browseSession.NamespaceUris)))
                {
                    var objectId = ExpandedNodeId.ToNodeId(objItem.NodeId, _browseSession.NamespaceUris);

                    //Prepare the Dataset
                    var datasetItems = InitializeItemList(objectId, typeId, out Dictionary<string, uint> datasetAttributes);
                    LoadItemList(datasetItems, "MaterialBuffer", objectId);

                    //Add the dataset
                    _configurationClient.AddPublishedDataSet(datasetItems, datasetAttributes, $"{machineName}.{objItem.BrowseName.Name}", out PublishedDataSetBase publishedDataSet);
                    _configurationClient.AddExtensionField(publishedDataSet, "DataSetName", $"{_pathPrefix}/{publishedDataSet.Name.Replace('.', '/')}");

                    //Prepare the writer
                    _configurationClient.AddWriter(writerGroup, $"{machineName}.MaterialBuffers.{objItem.BrowseName.Name}", publishedDataSet.Name, $"{writerGroup.QueueName}/{objItem.BrowseName.Name}", out DataSetWriterDefinition writer);
                    //_configurationClient.EnableWriter(writer.Name);
                }
            }
        }

        private void ConfigureMaterialLoadingPoints(NodeId folderId, string machineName)
        {

            //Prepare the Writer group
            _configurationClient.AddWriterGroup(_mqttConnection, $"{machineName}.MaterialLoadingPoints", $"{_topicPrefix}{machineName}/MaterialLoadingPoints", out DataSetWriterGroup writerGroup);
            _configurationClient.EnableWriterGroup(writerGroup.Name);

            var references = Browse(folderId);

            foreach (var objItem in references.Where(x => x.NodeClass == NodeClass.Object))
            {
                var typeId = GetTypeDefinition(objItem.NodeId);
                if (typeId.Equals(ExpandedNodeId.ToNodeId(TMCPlus.ObjectTypeIds.PMI_MaterialLoadingPointType, _browseSession.NamespaceUris)))
                {
                    var objectId = ExpandedNodeId.ToNodeId(objItem.NodeId, _browseSession.NamespaceUris);

                    //Prepare the Dataset
                    var datasetItems = InitializeItemList(objectId, typeId, out Dictionary<string, uint> datasetAttributes);
                    LoadItemList(datasetItems, "MaterialLoadingPoint", objectId);

                    //Add the dataset
                    _configurationClient.AddPublishedDataSet(datasetItems, datasetAttributes, $"{machineName}.{objItem.BrowseName.Name}", out PublishedDataSetBase publishedDataSet);
                    _configurationClient.AddExtensionField(publishedDataSet, "DataSetName", $"{_pathPrefix}/{publishedDataSet.Name.Replace('.', '/')}");

                    //Prepare the writer
                    _configurationClient.AddWriter(writerGroup, $"{machineName}.MaterialLoadingPoints.{objItem.BrowseName.Name}", publishedDataSet.Name, $"{writerGroup.QueueName}/{objItem.BrowseName.Name}", out DataSetWriterDefinition writer);
                    //_configurationClient.EnableWriter(writer.Name);
                }
            }
        }

        private void ConfigureMaterialOutputs(NodeId folderId, string machineName)
        {
            //Prepare the Writer group
            _configurationClient.AddWriterGroup(_mqttConnection, $"{machineName}.MaterialOutputs", $"{_topicPrefix}{machineName}/MaterialOutputs", out DataSetWriterGroup writerGroup);
            _configurationClient.EnableWriterGroup(writerGroup.Name);
            
            var references = Browse(folderId);

            foreach (var objItem in references.Where(x => x.NodeClass == NodeClass.Object))
            {
                var typeId = GetTypeDefinition(objItem.NodeId);
                if (typeId.Equals(ExpandedNodeId.ToNodeId(TMCPlus.ObjectTypeIds.PMI_MaterialOutputType, _browseSession.NamespaceUris)))
                {
                    var objectId = ExpandedNodeId.ToNodeId(objItem.NodeId, _browseSession.NamespaceUris);

                    //Prepare the Dataset
                    var datasetItems = InitializeItemList(objectId, typeId, out Dictionary<string, uint> datasetAttributes);
                    LoadItemList(datasetItems, "MaterialOutput", objectId);

                    //Add the dataset
                    _configurationClient.AddPublishedDataSet(datasetItems, datasetAttributes, $"{machineName}.{objItem.BrowseName.Name}", out PublishedDataSetBase publishedDataSet);
                    _configurationClient.AddExtensionField(publishedDataSet, "DataSetName", $"{_pathPrefix}/{publishedDataSet.Name.Replace('.', '/')}");

                    //Prepare the writer
                    _configurationClient.AddWriter(writerGroup, $"{machineName}.MaterialOutputs.{objItem.BrowseName.Name}", publishedDataSet.Name, $"{writerGroup.QueueName}/{objItem.BrowseName.Name}", out DataSetWriterDefinition writer);
                    //_configurationClient.EnableWriter(writer.Name);
                }
            }
        }

        private void ConfigureMaterialRejectionTraps(NodeId folderId, string machineName)
        {
            //Prepare the Writer group
            _configurationClient.AddWriterGroup(_mqttConnection, $"{machineName}.MaterialRejectionTraps", $"{_topicPrefix}{machineName}/MaterialRejectionTraps", out DataSetWriterGroup writerGroup);
            _configurationClient.EnableWriterGroup(writerGroup.Name);

            var references = Browse(folderId);

            foreach (var objItem in references.Where(x => x.NodeClass == NodeClass.Object))
            {
                var typeId = GetTypeDefinition(objItem.NodeId);
                if (typeId.Equals(ExpandedNodeId.ToNodeId(TMCPlus.ObjectTypeIds.PMI_MaterialRejectionTrapType, _browseSession.NamespaceUris)))
                {
                    var objectId = ExpandedNodeId.ToNodeId(objItem.NodeId, _browseSession.NamespaceUris);

                    //Prepare the Dataset
                    var datasetItems = InitializeItemList(objectId, typeId, out Dictionary<string, uint> datasetAttributes);
                    LoadItemList(datasetItems, "MaterialRejectionTrap", objectId);

                    //Add the dataset
                    _configurationClient.AddPublishedDataSet(datasetItems, datasetAttributes, $"{machineName}.{objItem.BrowseName.Name}", out PublishedDataSetBase publishedDataSet);
                    _configurationClient.AddExtensionField(publishedDataSet, "DataSetName", $"{_pathPrefix}/{publishedDataSet.Name.Replace('.', '/')}");

                    //Prepare the writer
                    _configurationClient.AddWriter(writerGroup, $"{machineName}.MaterialRejectionTraps.{objItem.BrowseName.Name}", publishedDataSet.Name, $"{writerGroup.QueueName}/{objItem.BrowseName.Name}", out DataSetWriterDefinition writer);
                    //_configurationClient.EnableWriter(writer.Name);
                }
            }
        }

        private void ConfigureProcessControlLoops(NodeId folderId, string machineName)
        {
            //Prepare the Writer group
            _configurationClient.AddWriterGroup(_mqttConnection, $"{machineName}.ProcessControlLoops", $"{_topicPrefix}{machineName}/ProcessControlLoops", out DataSetWriterGroup writerGroup);
            _configurationClient.EnableWriterGroup(writerGroup.Name);

            var references = Browse(folderId);

            foreach (var objItem in references.Where(x => x.NodeClass == NodeClass.Object))
            {
                Console.WriteLine($"Configuring processControlLoop {objItem.BrowseName.Name}");
                var typeId = GetTypeDefinition(objItem.NodeId);
                if (typeId.Equals(ExpandedNodeId.ToNodeId(TMCPlus.ObjectTypeIds.PMI_ProcessControlLoopType, _browseSession.NamespaceUris)))
                {
                    var objectId = ExpandedNodeId.ToNodeId(objItem.NodeId, _browseSession.NamespaceUris);

                    //Prepare the Dataset
                    var datasetItems = InitializeItemList(objectId, typeId, out Dictionary<string, uint> datasetAttributes);
                    LoadItemList(datasetItems, "ProcessControlLoop", objectId);

                    //Add the dataset
                    _configurationClient.AddPublishedDataSet(datasetItems, datasetAttributes, $"{machineName}.{objItem.BrowseName.Name}", out PublishedDataSetBase publishedDataSet);
                    _configurationClient.AddExtensionField(publishedDataSet, "DataSetName", $"{_pathPrefix}/{publishedDataSet.Name.Replace('.', '/')}");

                    //Prepare the writer
                    _configurationClient.AddWriter(writerGroup, $"{machineName}.ProcessControlLoops.{objItem.BrowseName.Name}", publishedDataSet.Name, $"{writerGroup.QueueName}/{objItem.BrowseName.Name}", out DataSetWriterDefinition writer);
                    //_configurationClient.EnableWriter(writer.Name);
                }
            }
        }

        private void ConfigureProcessItems(NodeId folderId, string machineName)
        { 
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
                    LoadItemList(datasetItems, "ProcessItem", objectId);

                    //Add the dataset
                    _configurationClient.AddPublishedDataSet(datasetItems, datasetAttributes, $"{machineName}.{objItem.BrowseName.Name}", out PublishedDataSetBase publishedDataSet);
                    _configurationClient.AddExtensionField(publishedDataSet, "DataSetName", $"{_pathPrefix}/{publishedDataSet.Name.Replace('.', '/')}");

                    //Prepare the writer
                    _configurationClient.AddWriter(writerGroup, $"{machineName}.ProcessItems.{objItem.BrowseName.Name}", publishedDataSet.Name, $"{writerGroup.QueueName}/{objItem.BrowseName.Name}", out DataSetWriterDefinition writer);
                    //////_configurationClient.EnableWriter(writer.Name);
                }
            }
        }
        #endregion


        #region Private Methods
        private Dictionary<string, NodeId> InitializeItemList(NodeId objectId, NodeId objectTypeId, out Dictionary<string, uint> attributesList)
        {
            var fieldList = new Dictionary<string, NodeId>();
            attributesList = new Dictionary<string, uint>();

            var jsonConfig = LoadJsonConfiguration("Base");

            foreach (var field in jsonConfig.Fields)
            {
                var fieldId = objectId;
                if (field.BrowseName == "_type") fieldId = objectTypeId;
                else if(field.BrowseName == "_this") fieldId = objectId;
                else if(field.FieldName.Split('.').Length == 2)
                {
                    var subObjectId = GetChildId(objectId, field.BrowseName);
                    if(subObjectId != null)
                    {
                        var subObjectNodes = Browse(subObjectId);
                        var node = subObjectNodes.FirstOrDefault(x => x.BrowseName.Name == field.BrowseName);
                        if(node != null)
                        {
                            fieldId = ExpandedNodeId.ToNodeId(node.NodeId, _browseSession.NamespaceUris);
                        }
                    }

                }

                fieldList.Add(field.FieldName, fieldId);
                attributesList.Add(field.FieldName, field.Attribute);
            }


            return fieldList;
        }

        private void LoadItemList(Dictionary<string, NodeId> itemList, PubSubObjectConfiguration config, NodeId objectNodeId)
        {
            //Load the parent type item list
            if (config.ParentType != null) LoadItemList(itemList, config.ParentType, objectNodeId);
            
            var subObjectReferences = new Dictionary<string, List<ReferenceDescription>>();
            var subNodes = Browse(objectNodeId);

            if(config != null)
            {
                foreach(var field in config.Fields)
                {
                    var fieldName = field.FieldName;
                    NodeId fieldId = null;
                    if (field.FieldName.Split('.').Length == 2)
                    {
                        var subObjectName = field.FieldName.Split('.')[0];
                        var subObjectId = subNodes.FirstOrDefault(x => x.BrowseName.Name == subObjectName).NodeId;

                        List<ReferenceDescription> references;
                        if (!subObjectReferences.ContainsKey(subObjectName))
                        {
                            references = Browse(subObjectId);
                            subObjectReferences.Add(subObjectName, references);
                        }
                        else
                        {
                            references = subObjectReferences[subObjectName];
                        }

                        var fieldReference = references.FirstOrDefault(x => x.BrowseName.Name == field.BrowseName);
                        if(fieldReference != null)
                        {
                            fieldId = ExpandedNodeId.ToNodeId(fieldReference.NodeId, _browseSession.NamespaceUris);
                            itemList.Add(fieldName, fieldId);
                        }
                    }
                    else
                    {
                        var fieldReference = subNodes.FirstOrDefault(x => x.BrowseName.Name == field.BrowseName);

                        if(fieldReference != null)
                        {
                            fieldId = ExpandedNodeId.ToNodeId(fieldReference.NodeId, _browseSession.NamespaceUris);
                            itemList.Add(fieldName, fieldId);
                        }
                       
                    }

                    //If the field is a complex variable, include also the sub properties
                    if (!String.IsNullOrEmpty(field.ComplexVariableType))
                    {
                        if(fieldId != null)
                        {
                            LoadComplexVariableItemList(itemList, field.ComplexVariableType, fieldId, fieldName);
                        }
                    }

                    if (fieldId == null) Console.WriteLine($"NodeId for the field {fieldName} of object with nodId {objectNodeId} not found");
                }
            }
        }

        private void LoadComplexVariableItemList(Dictionary<string, NodeId> itemList, string variableTypeName, NodeId variableNodeId, string variableName)
        {
            try
            {
                var config = LoadJsonConfiguration(variableTypeName);

                var subObjectReferences = new Dictionary<string, List<ReferenceDescription>>();
                var subNodes = Browse(variableNodeId);

                if (config != null)
                {
                    foreach (var field in config.Fields)
                    {
                        var fieldName = $"{variableName}.{field.FieldName}";
                        Console.WriteLine($"Field: {fieldName}");

                        if (field.FieldName.Split('.').Length == 2)
                        {
                            var subObjectName = field.FieldName.Split('.')[0];
                            var subObjectId = subNodes.FirstOrDefault(x => x.BrowseName.Name == subObjectName).NodeId;

                            List<ReferenceDescription> references;
                            if (!subObjectReferences.ContainsKey(subObjectName))
                            {
                                references = Browse(subObjectId);
                                subObjectReferences.Add(subObjectName, references);
                            }
                            else
                            {
                                references = subObjectReferences[subObjectName];
                            }

                            var fieldReference = references.FirstOrDefault(x => x.BrowseName.Name == field.BrowseName);

                            var fieldId = ExpandedNodeId.ToNodeId(fieldReference.NodeId, _browseSession.NamespaceUris);
                            itemList.Add(fieldName, fieldId);
                        }
                        else
                        {
                            var fieldExpId = subNodes.FirstOrDefault(x => x.BrowseName.Name == field.BrowseName).NodeId;
                            var fieldId = ExpandedNodeId.ToNodeId(fieldExpId, _browseSession.NamespaceUris);
                            itemList.Add(fieldName, fieldId);
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine($"ConfigurationBuilder LoadComplexVariableItemList...Error loading configuration. Type: {variableTypeName}; Variable name: {variableName}; Exception: {ex}");
            }

        }

        private void LoadItemList(Dictionary<string, NodeId> itemList, string objectTypeName, NodeId objectNodeId)
        {
            var jsonConfig = LoadJsonConfiguration(objectTypeName);
            LoadItemList(itemList, jsonConfig, objectNodeId);
        }

        private PubSubObjectConfiguration LoadJsonConfiguration(string typeName)
        {
            var jsonFilename = $@"AppData/Configuration.{typeName}.json";
            if (File.Exists(jsonFilename))
            {
                return JsonConvert.DeserializeObject<PubSubObjectConfiguration>(File.ReadAllText(jsonFilename));
            }
            else throw new FileNotFoundException($"JSON file {jsonFilename} not found.");
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

        private NodeId GetTypeDefinition(NodeId nodeId)
        {
            return CommonFunctions.GetTypeDefinition(_browseSession, nodeId);
        }

        private NodeId GetTypeDefinition(ExpandedNodeId nodeId)
        {
            return GetTypeDefinition(ExpandedNodeId.ToNodeId(nodeId, _browseSession.NamespaceUris));
        }
        #endregion
    }

    [DataContract]
    class PubSubObjectConfiguration
    {
        [DataMember]
        public string Id { get; set; }

        [DataMember]
        public int PublishInterval { get; set; }

        [DataMember]
        public string ParentType { get; set; }
        
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

        [DataMember]
        public string ComplexVariableType { get; set; }
    }


}
