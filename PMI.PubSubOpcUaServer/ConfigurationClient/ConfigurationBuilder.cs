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
            var brokerIp = Environment.GetEnvironmentVariable("BROKER_IP");
            var brokerPort = Environment.GetEnvironmentVariable("BROKER_PORT");

            //Read the environmental variables
            if (brokerIp != null && brokerPort != null)
            {
                _browseSession = browseSession;
                _configurationClient = new ConfigurationClient(pubSubServer);
                _configurationClient.InitializeClient();
                _configurationClient.InitializeMQTTConnection("MQTT", $"{brokerIp}:{brokerPort}", out _mqttConnection);
            }
            else
            {
                throw new Exception("ConfigurationBuilder error...broker IP address or port are not correctly set");
            }

            
        }

        private void CertificateValidator_CertificateValidation(CertificateValidator sender, CertificateValidationEventArgs e)
        {
            e.Accept = true;
        }

        #region PubSub Configuration Methods
        /// <summary>
        /// Starts the configuration process of the PubSub module
        /// </summary>
        public void Start()
        {
            //Initialize the encodable types in order to be correctly serialized in the json
            InitializeEncodableTypes();

            //Sample
            //SampleEvents();

            //Browse the Objects folder
            var objFolderNodes = Browse(ObjectIds.ObjectsFolder);

            //Search for the DeviceSet folder
            var deviceSetExpNodeId = objFolderNodes.FirstOrDefault(x => x.BrowseName.Name == "DeviceSet" && x.NodeClass == NodeClass.Object)?.NodeId;

            if (deviceSetExpNodeId != null)
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

        private void SampleEvents()
        {
            //var fields = InitializeEventItemList();
            //fields.Add(new DataSetEventFieldConfiguration(new QualifiedName(TMCPlus.BrowseNames.MaterialLotAttributes, 4)));

            //fields.Add(new DataSetEventFieldConfiguration(new QualifiedName(TMCPlus.BrowseNames.MaterialAttributes, 4)));

            //fields.Add(new DataSetEventFieldConfiguration(new QualifiedName(TMCPlus.BrowseNames.LoadingPointMES_ID, 4)));

            //fields.Add(new DataSetEventFieldConfiguration(new QualifiedName(TMCPlus.BrowseNames.PONumber, 4)));

            //fields.Add(new DataSetEventFieldConfiguration(new QualifiedName(TMCPlus.BrowseNames.MaterialQuantity, 3)));
            //fields.Add(new DataSetEventFieldConfiguration()
            //{
            //    Name = $"{TMCPlus.BrowseNames.MaterialQuantity}/{TMCPlus.BrowseNames.QuantityInLUoM}",
            //    BrowsePath = new QualifiedNameCollection
            //    {
            //        new QualifiedName(TMCPlus.BrowseNames.MaterialQuantity, 3),
            //        new QualifiedName(TMCPlus.BrowseNames.QuantityInLUoM, 3),
            //    }
            //});

            //_configurationClient.AddWriterGroup(_mqttConnection, "TestEventGroup", "eventsTest", out DataSetWriterGroup group);
            //_configurationClient.AddPublishedDataSetEvent(fields, new NodeId(1012, 4), "EventData", new NodeId(5331, 5), out PublishedDataSetBase dataset);
            //_configurationClient.AddPublishedDataSetEvent(fields, new NodeId(1008, 4), "EventData2", new NodeId(5331, 5), out PublishedDataSetBase dataset2);
            //_configurationClient.AddWriter(group, "EventWriter", "EventData", out DataSetWriterDefinition writer);
            //_configurationClient.AddWriter(group, "EventWriter2", "EventData2", out DataSetWriterDefinition writer2);
            //_configurationClient.EnableWriterGroup("TestEventGroup");
            //_configurationClient.EnableWriter("EventWriter");
            //_configurationClient.EnableWriter("EventWriter2");


            var configurations = GetEventsConfigurations(new NodeId(5331, 5));
            var materialConsumedConf = configurations.FirstOrDefault(x => x.EventTypeId.Equals(new NodeId(1012, 4)));
            var loadingPointIsLoaded = configurations.FirstOrDefault(x => x.EventTypeId.Equals(new NodeId(1008, 4)));
            var items = InitializeEventItemList();
            LoadEventFieldsList(items, materialConsumedConf);

            var items2 = InitializeEventItemList();
            LoadEventFieldsList(items2, loadingPointIsLoaded);

            _configurationClient.AddWriterGroup(_mqttConnection, "TestEventGroup", "eventsTest", out DataSetWriterGroup group);
            _configurationClient.EnableWriterGroup(group.Name);
            _configurationClient.AddPublishedDataSetEvent(items, new NodeId(1012, 4), "EventDataTest", new NodeId(5331, 5), out PublishedDataSetBase dataset);
            _configurationClient.AddPublishedDataSetEvent(items2, new NodeId(1008, 4), "EventDataTest2", new NodeId(5331, 5), out PublishedDataSetBase dataset2);
            _configurationClient.AddPublishedDataSetEvent(items2, new NodeId(1012, 4), "EventDataTest2", new NodeId(5335, 5), out PublishedDataSetBase dataset3);
            _configurationClient.AddWriter(group, "EventWriter", dataset.Name, out DataSetWriterDefinition writer);
            _configurationClient.AddWriter(group, "EventWriter2", dataset2.Name, out DataSetWriterDefinition writer2);
            _configurationClient.AddWriter(group, "EventWriter3", dataset3.Name, out DataSetWriterDefinition writer3);
            _configurationClient.EnableWriter(writer.Name);
            _configurationClient.EnableWriter(writer2.Name);
            _configurationClient.EnableWriter(writer3.Name);

        }

        /// <summary>
        /// Configure the PubSub items for the specified machine module
        /// </summary>
        /// <param name="machineModuleId">The NodeId of the Machine Module</param>
        /// <param name="machineName">The name of the machine module</param>
        public void ConfigureMachineModule(NodeId machineModuleId, string machineName)
        {
            //Search the nodeIds of the objects folders
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

            //Prepare the dataset for the entire MachineModule
            var typeId = GetTypeDefinition(machineModuleId);
            var datasetItems = InitializeItemList(machineModuleId, typeId);
            var objectIncluded = LoadItemList(datasetItems, "MachineModule", machineModuleId);
            if (objectIncluded)
            {
                //Add the dataset and the extensionFields
                _configurationClient.AddPublishedDataSet(datasetItems, $"{machineName}", out PublishedDataSetBase publishedDataSet);
                _configurationClient.AddExtensionField(publishedDataSet, "DataSetName", $"{_pathPrefix}/{publishedDataSet.Name.Replace('.', '/')}");

                //Prepare the writer
                _configurationClient.AddWriter(writerGroup, $"{machineName}", publishedDataSet.Name, $"{writerGroup.QueueName}", out DataSetWriterDefinition writer);
            }

            //Configure Events
            var subObjects = new List<NodeId>();
            var configurationId = GetChildId(machineModuleId, TMCGroup.TMC.BrowseNames.Configuration);
            subObjects.Add(configurationId);
            var livestatusId = GetChildId(machineModuleId, TMCGroup.TMC.BrowseNames.LiveStatus);
            subObjects.Add(livestatusId);
            var productionId = GetChildId(machineModuleId, TMCGroup.TMC.BrowseNames.Production);
            subObjects.Add(productionId);
            var setupId = GetChildId(machineModuleId, TMCGroup.TMC.BrowseNames.SetUp);
            subObjects.Add(setupId);
            var specificationId = GetChildId(machineModuleId, TMCGroup.TMC.BrowseNames.Specification);
            subObjects.Add(specificationId);

            //Add a writer group for the events
            _configurationClient.AddWriterGroup(_mqttConnection, $"{machineName}.Events", $"{_topicPrefix}{machineName}/Events", out DataSetWriterGroup writerGroupEvents);
            _configurationClient.EnableWriterGroup(writerGroupEvents.Name);

            foreach (var subObjId in subObjects)
            {
                //Get the configuration for the events of this object
                var eventsConfiguration = GetEventsConfigurations(subObjId);

                foreach (var conf in eventsConfiguration)
                {
                    //Check if the configuration of this event for this object has to be included
                    if (ObjectIncluded(conf, subObjId))
                    {
                        //Prepare the list of event fields from the configuration
                        var eventFields = InitializeEventItemList();
                        LoadEventFieldsList(eventFields, conf);

                        PublishedDataSetBase eventDataSet = new PublishedDataSetBase();
                        _configurationClient.AddPublishedDataSetEvent(eventFields, conf.EventTypeId, $"{machineName}.{conf.EventTypeName}", subObjId, out eventDataSet);

                        DataSetWriterDefinition writerEvent = new DataSetWriterDefinition();
                        _configurationClient.AddWriter(writerGroup, $"{machineName}.{conf.EventTypeName}", eventDataSet.Name, $"{writerGroup.QueueName}/{conf.EventTypeName}", out writerEvent);
                    }
                }
            }

        }

        /// <summary>
        /// Add the structured types to let the JsonEncoder serialize them
        /// </summary>
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


        /// <summary>
        /// Configure the pub sub items for the defect sensors in the folder
        /// </summary>
        /// <param name="folderId">The nodeId of the folder</param>
        /// <param name="machineName">The name of the machine module</param>
        private void ConfigureDefectSensors(NodeId folderId, string machineName)
        {
            //Prepare the Writer group
            _configurationClient.AddWriterGroup(_mqttConnection, $"{machineName}.DefectDetectionSensors", $"{_topicPrefix}{machineName}/DefectDetectionSensors", out DataSetWriterGroup writerGroup);
            _configurationClient.EnableWriterGroup(writerGroup.Name);

            var references = Browse(folderId);

            foreach (var objItem in references.Where(x => x.NodeClass == NodeClass.Object))
            {
                var typeId = GetTypeDefinition(objItem.NodeId);
                if (typeId.Equals(ExpandedNodeId.ToNodeId(TMCPlus.ObjectTypeIds.PMI_DefectDetectionSensorType, _browseSession.NamespaceUris)))
                {
                    //Do things
                }
            }
        }

        /// <summary>
        /// Configure the pub sub items for the material storage buffers in the folder
        /// </summary>
        /// <param name="folderId">The nodeId of the folder</param>
        /// <param name="machineName">The name of the machine module</param>
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
                    var datasetItems = InitializeItemList(objectId, typeId);
                    var objectIncluded = LoadItemList(datasetItems, "MaterialBuffer", objectId);

                    if (objectIncluded)
                    {                    
                        //Add the dataset
                        _configurationClient.AddPublishedDataSet(datasetItems, $"{machineName}.{objItem.BrowseName.Name}", out PublishedDataSetBase publishedDataSet);
                        _configurationClient.AddExtensionField(publishedDataSet, "DataSetName", $"{_pathPrefix}/{publishedDataSet.Name.Replace('.', '/')}");

                        //Prepare the writer
                        _configurationClient.AddWriter(writerGroup, $"{machineName}.MaterialBuffers.{objItem.BrowseName.Name}", publishedDataSet.Name, $"{writerGroup.QueueName}/{objItem.BrowseName.Name}", out DataSetWriterDefinition writer);
                    }

                    //Configure object events
                    ConfigureObjectEvents(machineName, objItem.BrowseName.Name, objectId, writerGroup);
                }
            }


        }

        /// <summary>
        /// Configure the pub sub items for the material loading points in the folder
        /// </summary>
        /// <param name="folderId">The nodeId of the folder</param>
        /// <param name="machineName">The name of the machine module</param>
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
                    var datasetItems = InitializeItemList(objectId, typeId);
                    var objectIncluded = LoadItemList(datasetItems, "MaterialLoadingPoint", objectId);

                    if (objectIncluded)
                    {
                        //Add the dataset
                        _configurationClient.AddPublishedDataSet(datasetItems, $"{machineName}.{objItem.BrowseName.Name}", out PublishedDataSetBase publishedDataSet);
                        _configurationClient.AddExtensionField(publishedDataSet, "DataSetName", $"{_pathPrefix}/{publishedDataSet.Name.Replace('.', '/')}");

                        //Prepare the writer
                        _configurationClient.AddWriter(writerGroup, $"{machineName}.MaterialLoadingPoints.{objItem.BrowseName.Name}", publishedDataSet.Name, $"{writerGroup.QueueName}/{objItem.BrowseName.Name}", out DataSetWriterDefinition writer);
                    }

                    //Configure object events
                    ConfigureObjectEvents(machineName, objItem.BrowseName.Name, objectId, writerGroup);
                }
            }
        }

        /// <summary>
        /// Configure the pub sub items for the material outputs in the folder
        /// </summary>
        /// <param name="folderId">The nodeId of the folder</param>
        /// <param name="machineName">The name of the machine module</param>
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
                    var datasetItems = InitializeItemList(objectId, typeId);
                    var objectIncluded = LoadItemList(datasetItems, "MaterialOutput", objectId);

                    if (objectIncluded)
                    {
                        //Add the dataset
                        _configurationClient.AddPublishedDataSet(datasetItems, $"{machineName}.{objItem.BrowseName.Name}", out PublishedDataSetBase publishedDataSet);
                        _configurationClient.AddExtensionField(publishedDataSet, "DataSetName", $"{_pathPrefix}/{publishedDataSet.Name.Replace('.', '/')}");

                        //Prepare the writer
                        _configurationClient.AddWriter(writerGroup, $"{machineName}.MaterialOutputs.{objItem.BrowseName.Name}", publishedDataSet.Name, $"{writerGroup.QueueName}/{objItem.BrowseName.Name}", out DataSetWriterDefinition writer);
                    }

                    //Configure object events
                    ConfigureObjectEvents(machineName, objItem.BrowseName.Name, objectId, writerGroup);
                }
            }
        }

        /// <summary>
        /// Configure the pub sub items for the material rejection traps in the folder
        /// </summary>
        /// <param name="folderId">The nodeId of the folder</param>
        /// <param name="machineName">The name of the machine module</param>
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
                    var datasetItems = InitializeItemList(objectId, typeId);
                    var objectIncluded = LoadItemList(datasetItems, "MaterialRejectionTrap", objectId);

                    if (objectIncluded)
                    {
                        //Add the dataset
                        _configurationClient.AddPublishedDataSet(datasetItems, $"{machineName}.{objItem.BrowseName.Name}", out PublishedDataSetBase publishedDataSet);
                        _configurationClient.AddExtensionField(publishedDataSet, "DataSetName", $"{_pathPrefix}/{publishedDataSet.Name.Replace('.', '/')}");

                        //Prepare the writer
                        _configurationClient.AddWriter(writerGroup, $"{machineName}.MaterialRejectionTraps.{objItem.BrowseName.Name}", publishedDataSet.Name, $"{writerGroup.QueueName}/{objItem.BrowseName.Name}", out DataSetWriterDefinition writer);
                    }

                    //Configure object events
                    ConfigureObjectEvents(machineName, objItem.BrowseName.Name, objectId, writerGroup);
                }

            }
        }

        /// <summary>
        /// Configure the pub sub items for the process control loops in the folder
        /// </summary>
        /// <param name="folderId">The nodeId of the folder</param>
        /// <param name="machineName">The name of the machine module</param>
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
                    var datasetItems = InitializeItemList(objectId, typeId);
                    var objectIncluded = LoadItemList(datasetItems, "ProcessControlLoop", objectId);

                    if (objectIncluded)
                    {
                        //Add the dataset
                        _configurationClient.AddPublishedDataSet(datasetItems, $"{machineName}.{objItem.BrowseName.Name}", out PublishedDataSetBase publishedDataSet);
                        _configurationClient.AddExtensionField(publishedDataSet, "DataSetName", $"{_pathPrefix}/{publishedDataSet.Name.Replace('.', '/')}");

                        //Prepare the writer
                        _configurationClient.AddWriter(writerGroup, $"{machineName}.ProcessControlLoops.{objItem.BrowseName.Name}", publishedDataSet.Name, $"{writerGroup.QueueName}/{objItem.BrowseName.Name}", out DataSetWriterDefinition writer);
                    }

                    //Configure object events
                    //ConfigureObjectEvents(machineName, objItem.BrowseName.Name, objectId, writerGroup);
                }
            }
        }

        /// <summary>
        /// Configure the pub sub items for the process items in the folder
        /// </summary>
        /// <param name="folderId">The nodeId of the folder</param>
        /// <param name="machineName">The name of the machine module</param>
        private void ConfigureProcessItems(NodeId folderId, string machineName)
        {
            try
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
                        var datasetItems = InitializeItemList(objectId, typeId);
                        var objectIncluded = LoadItemList(datasetItems, "ProcessItem", objectId);

                        if (objectIncluded)
                        {
                            //Add the dataset
                            _configurationClient.AddPublishedDataSet(datasetItems, $"{machineName}.{objItem.BrowseName.Name}", out PublishedDataSetBase publishedDataSet);
                            _configurationClient.AddExtensionField(publishedDataSet, "DataSetName", $"{_pathPrefix}/{publishedDataSet.Name.Replace('.', '/')}");

                            //Prepare the writer
                            _configurationClient.AddWriter(writerGroup, $"{machineName}.ProcessItems.{objItem.BrowseName.Name}", publishedDataSet.Name, $"{writerGroup.QueueName}/{objItem.BrowseName.Name}", out DataSetWriterDefinition writer);
                        }

                        //Configure object events
                        //ConfigureObjectEvents(machineName, objItem.BrowseName.Name, objectId, writerGroup);
                    }
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine($"ConfigurationBuilder ConfigureProcessItems...Error: {ex}");
            }
        }
        #endregion


        #region Private Methods

        #region Data Items
        /// <summary>
        /// Initialize the list of items of the published dataset
        /// </summary>
        /// <param name="objectId">The nodeId of the object</param>
        /// <param name="objectTypeId">The nodeId of the type definition of the object</param>
        /// <returns></returns>
        private List<DataSetFieldConfiguration> InitializeItemList(NodeId objectId, NodeId objectTypeId)
        {
            var fieldList = new List<DataSetFieldConfiguration>();

            //Load the Base configuration
            var jsonConfig = LoadJsonDataConfiguration("Base");

            foreach (var field in jsonConfig.Fields)
            {
                var fieldId = objectId;
                if (field.BrowseName == "_type") fieldId = objectTypeId;
                else if (field.BrowseName == "_this") fieldId = objectId;
                else if (field.FieldName.Split('.').Length == 2)
                {
                    var subObjectId = GetChildId(objectId, field.BrowseName);
                    if (subObjectId != null)
                    {
                        var subObjectNodes = Browse(subObjectId);
                        var node = subObjectNodes.FirstOrDefault(x => x.BrowseName.Name == field.BrowseName);
                        if (node != null)
                        {
                            fieldId = ExpandedNodeId.ToNodeId(node.NodeId, _browseSession.NamespaceUris);
                        }
                    }

                }

                fieldList.Add(new DataSetFieldConfiguration()
                {
                    Name = field.FieldName,
                    SourceNodeId = fieldId,
                    Attribute = field.Attribute,
                    SamplingInterval = field.SamplingInterval
                });
            }


            return fieldList;
        }

        /// <summary>
        /// Load the published dataset items from the configuration
        /// </summary>
        /// <param name="itemList">The item list to fill</param>
        /// <param name="configuration">The configuration of the published dataset</param>
        /// <param name="objectNodeId">The node id of the object </param>
        /// <returns></returns>
        private bool LoadItemList(List<DataSetFieldConfiguration> itemList, PubSubObjectDataConfiguration configuration, NodeId objectNodeId)
        {
            var objectIncluded = true;

            if (configuration != null)
            {
                if (configuration.ExcludedNodes != null)
                {
                    if (!configuration.ExcludedNodes.Any(x => x.Equals(objectNodeId))) objectIncluded = true;
                    else return false;
                }
                else if (configuration.IncludedNodes != null)
                {
                    if (configuration.IncludedNodes.Any(x => x.Equals(objectNodeId))) objectIncluded = true;
                    else return false;
                }

                //Load the parent type item list
                if (configuration.ParentType != null) LoadItemList(itemList, configuration.ParentType, objectNodeId);

                var subObjectReferences = new Dictionary<string, List<ReferenceDescription>>();
                var subNodes = Browse(objectNodeId);

                foreach (var field in configuration.Fields)
                {
                    if (field.Enabled)
                    {
                        var fieldName = field.FieldName;
                        NodeId fieldId = null;

                        //If the field contains a dot, the variable is a child of a child node 
                        if (field.FieldName.Split('.').Length == 2)
                        {
                            //The first part of the field name is the name of the child node
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
                            if (fieldReference != null)
                            {
                                fieldId = ExpandedNodeId.ToNodeId(fieldReference.NodeId, _browseSession.NamespaceUris);
                                itemList.Add(new DataSetFieldConfiguration(fieldName, fieldId, field));
                            }
                        }
                        else
                        {
                            var fieldReference = subNodes.FirstOrDefault(x => x.BrowseName.Name == field.BrowseName);

                            if (fieldReference != null)
                            {
                                fieldId = ExpandedNodeId.ToNodeId(fieldReference.NodeId, _browseSession.NamespaceUris);
                                itemList.Add(new DataSetFieldConfiguration(fieldName, fieldId, field));
                            }

                        }

                        //If the field is a complex variable, include also the sub properties
                        if (!String.IsNullOrEmpty(field.ComplexVariableType))
                        {
                            if (fieldId != null)
                            {
                                LoadComplexVariableItemList(itemList, field.ComplexVariableType, fieldId, fieldName);
                            }
                        }

                        if (fieldId == null) Console.WriteLine($"NodeId for the field {fieldName} of object with nodId {objectNodeId} not found");
                    }
                }
            }
            else return false;

            return objectIncluded;
        }

        /// <summary>
        /// Load in the item list all the fields of a complex variable
        /// </summary>
        /// <param name="itemList">The item list to fill</param>
        /// <param name="variableTypeName">The name of the variable type definition</param>
        /// <param name="variableNodeId">The nodeId of the variable</param>
        /// <param name="variableName">The name of the variable</param>
        private void LoadComplexVariableItemList(List<DataSetFieldConfiguration> itemList, string variableTypeName, NodeId variableNodeId, string variableName)
        {
            try
            {
                var config = LoadJsonDataConfiguration(variableTypeName);

                var subObjectReferences = new Dictionary<string, List<ReferenceDescription>>();
                var subNodes = Browse(variableNodeId);

                if (config != null)
                {
                    foreach (var field in config.Fields)
                    {
                        if (field.Enabled)
                        {
                            var fieldName = $"{variableName}.{field.FieldName}";
                            Console.WriteLine($"Field: {fieldName}");

                            if (field.FieldName.Split('.').Length == 2)
                            {
                                var subObjectName = field.FieldName.Split('.')[0];
                                var subObjectId = subNodes.FirstOrDefault(x => x.BrowseName.Name == subObjectName)?.NodeId;

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
                                itemList.Add(new DataSetFieldConfiguration(fieldName, fieldId, field));
                            }
                            else
                            {
                                var fieldExpId = subNodes.FirstOrDefault(x => x.BrowseName.Name == field.BrowseName).NodeId;
                                var fieldId = ExpandedNodeId.ToNodeId(fieldExpId, _browseSession.NamespaceUris);
                                itemList.Add(new DataSetFieldConfiguration(fieldName, fieldId, field));
                            }
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ConfigurationBuilder LoadComplexVariableItemList...Error loading configuration. Type: {variableTypeName}; Variable name: {variableName}; Exception: {ex}");
            }

        }

        private bool LoadItemList(List<DataSetFieldConfiguration> itemList, string objectTypeName, NodeId objectNodeId)
        {
            var jsonConfig = LoadJsonDataConfiguration(objectTypeName);
            return LoadItemList(itemList, jsonConfig, objectNodeId);
        }

        /// <summary>
        /// Load the published data configuration from the a json file
        /// </summary>
        /// <param name="typeName">The name of the type definition if the object</param>
        /// <returns></returns>
        private PubSubObjectDataConfiguration LoadJsonDataConfiguration(string typeName)
        {
            var jsonFilename = $@"AppData/Configuration.{typeName}.json";
            if (File.Exists(jsonFilename))
            {
                return JsonConvert.DeserializeObject<PubSubObjectDataConfiguration>(File.ReadAllText(jsonFilename));
            }
            else throw new FileNotFoundException($"JSON file {jsonFilename} not found.");
        }
        #endregion

        #region Events
        /// <summary>
        /// Initialize the event fields list 
        /// </summary>
        /// <returns></returns>
        private List<DataSetEventFieldConfiguration> InitializeEventItemList()
        {
            var fields = new List<DataSetEventFieldConfiguration>()
            {
                //new DataSetEventFieldConfiguration(BrowseNames.EventId),
                //new DataSetEventFieldConfiguration(BrowseNames.EventType),
                //new DataSetEventFieldConfiguration(BrowseNames.Message),
                //new DataSetEventFieldConfiguration(BrowseNames.SourceName),
                //new DataSetEventFieldConfiguration(BrowseNames.SourceNode),
                //new DataSetEventFieldConfiguration(BrowseNames.Severity),
                //new DataSetEventFieldConfiguration(BrowseNames.Time)
            };
            return fields;
        }

        /// <summary>
        /// Load the event field list from the configuration
        /// </summary>
        /// <param name="fieldsList">The event field list to fill</param>
        /// <param name="configuration">The configuration of the event type</param>
        private void LoadEventFieldsList(List<DataSetEventFieldConfiguration> fieldsList, PubSubEventConfiguration configuration)
        {
            foreach (var field in configuration.Fields)
            {
                if (field.Enabled)
                {
                    fieldsList.Add(new DataSetEventFieldConfiguration
                    {
                        Name = field.AliasName,
                        BrowsePath = field.BrowsePath
                    });
                }
            }
        }

        /// <summary>
        /// Retrive the configuration for the event of the specified object
        /// </summary>
        /// <param name="objectNodeId">The object nodeId</param>
        /// <returns></returns>
        private List<PubSubEventConfiguration> GetEventsConfigurations(NodeId objectNodeId)
        {
            var ret = new List<PubSubEventConfiguration>();

            try
            {
                var eventsList = CommonFunctions.GetGeneratedEvent(_browseSession, objectNodeId);
                foreach (var eventRef in eventsList)
                {
                    var conf = GetEventConfiguration(eventRef);
                    ret.Add(conf);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ConfigurationBulder GetEventsConfigurations...Exception thrown: {ex}");
            }

            return ret;
        }


        /// <summary>
        /// Get the pubsub configuration for an event
        /// </summary>
        /// <param name="eventTypeReference">The reference description of the event node</param>
        /// <returns></returns>
        private PubSubEventConfiguration GetEventConfiguration(ReferenceDescription eventTypeReference)
        {
            //Try to get the configuration from file
            var jsonConfig = LoadJsonEventConfiguration(eventTypeReference.BrowseName.Name);
            if (jsonConfig != null) return jsonConfig;

            var eventTypeId = ExpandedNodeId.ToNodeId(eventTypeReference.NodeId, _browseSession.NamespaceUris);
            var ret = new PubSubEventConfiguration();

            ret.EventTypeId = eventTypeId;
            ret.EventTypeName = eventTypeReference.BrowseName.Name;
            ret.Fields = new List<PubSubEventFieldConfiguration>();
            var children = Browse(eventTypeId, (uint)NodeClass.Variable);
            var retFieldList = ret.Fields as List<PubSubEventFieldConfiguration>;
            foreach (var child in children)
            {
                var fieldConf = new PubSubEventFieldConfiguration();
                fieldConf.AliasName = child.BrowseName.Name;
                fieldConf.BrowsePath = new QualifiedNameCollection { child.BrowseName };
                retFieldList.Add(fieldConf);
                GetEventsSubFields(retFieldList, ExpandedNodeId.ToNodeId(child.NodeId, _browseSession.NamespaceUris), fieldConf.BrowsePath);
            }
            GetEventSuperTypeFields(retFieldList, eventTypeId);

            try
            {
                File.WriteAllText($@"AppData/Configuration.{eventTypeReference.BrowseName.Name}.json", JsonConvert.SerializeObject(ret));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ConfigurationBuilder GetEventConfiguration...Unable to write the configuration file. Excpetion: {ex}");
            }

            return ret;

        }

        /// <summary>
        /// Retrieve the nested event fields
        /// </summary>
        /// <param name="fieldsList">The event field list to fill</param>
        /// <param name="parentFieldId">The nodeId of the parent of the field</param>
        /// <param name="browsePath">The browse path of the field</param>
        private void GetEventsSubFields(List<PubSubEventFieldConfiguration> fieldsList, NodeId parentFieldId, QualifiedNameCollection browsePath)
        {
            var childrenVar = Browse(parentFieldId, (uint)NodeClass.Variable);
            foreach (var child in childrenVar)
            {
                var fieldConf = new PubSubEventFieldConfiguration();
                fieldConf.BrowsePath = new QualifiedNameCollection(browsePath);
                fieldConf.BrowsePath.Add(child.BrowseName);
                fieldsList.Add(fieldConf);
                GetEventsSubFields(fieldsList, ExpandedNodeId.ToNodeId(child.NodeId, _browseSession.NamespaceUris), fieldConf.BrowsePath);
            }
        }

        /// <summary>
        /// Retrieve the event fields of the super type of the event
        /// </summary>
        /// <param name="fieldsList">The event field list to fill</param>
        /// <param name="subTypeId">The child event type</param>
        private void GetEventSuperTypeFields(List<PubSubEventFieldConfiguration> fieldsList, NodeId subTypeId)
        {
            var superTypeId = CommonFunctions.GetSuperTypeId(_browseSession, subTypeId);
            if (superTypeId != null)
            {
                var children = Browse(superTypeId, (uint)NodeClass.Variable);
                foreach (var child in children)
                {
                    var fieldConf = new PubSubEventFieldConfiguration();
                    fieldConf.AliasName = child.BrowseName.Name;
                    fieldConf.BrowsePath = new QualifiedNameCollection { child.BrowseName };
                    fieldsList.Add(fieldConf);
                    GetEventsSubFields(fieldsList, ExpandedNodeId.ToNodeId(child.NodeId, _browseSession.NamespaceUris), fieldConf.BrowsePath);
                }

                if (!superTypeId.Equals(ObjectTypeIds.BaseEventType)) GetEventSuperTypeFields(fieldsList, superTypeId);
            }
        }

        /// <summary>
        /// Load the event pubsub configuration
        /// </summary>
        /// <param name="typeName">The name of the event type</param>
        /// <returns></returns>
        private PubSubEventConfiguration LoadJsonEventConfiguration(string typeName)
        {
            var jsonFilename = $@"AppData/Configuration.{typeName}.json";
            if (File.Exists(jsonFilename))
            {
                return JsonConvert.DeserializeObject<PubSubEventConfiguration>(File.ReadAllText(jsonFilename));
            }
            else return null;
        }

        /// <summary>
        /// Check if the specified object shall be configured in the pub sub
        /// </summary>
        /// <param name="configuration">The configuration for the event type</param>
        /// <param name="objectId">The nodeId of the object that generates the event</param>
        /// <returns></returns>
        private bool ObjectIncluded(PubSubEventConfiguration configuration, NodeId objectId)
        {
            if(configuration.ExcludedNodes != null)
            {
                if (!configuration.ExcludedNodes.Any(x => x.Equals(objectId))) return true;
                return false;
            }
            else if(configuration.IncludedNodes != null)
            {
                if (configuration.IncludedNodes.Any(x => x.Equals(objectId))) return true;
                return false;
            }
            return true;
        }

        /// <summary>
        /// Configure the pub sub items for an event generated by an object
        /// </summary>
        /// <param name="machineName">The name of the machine module</param>
        /// <param name="objectName">The name of the object that generates the event</param>
        /// <param name="objectId">The nodeId of the object</param>
        /// <param name="writerGroup">The writer group in which add the new writer</param>
        public void ConfigureObjectEvents(string machineName, string objectName, NodeId objectId, DataSetWriterGroup writerGroup)
        {
            var eventsConfiguration = GetEventsConfigurations(objectId);

            foreach (var conf in eventsConfiguration)
            {
                if (ObjectIncluded(conf, objectId))
                {
                    var eventFields = InitializeEventItemList();
                    LoadEventFieldsList(eventFields, conf);

                    PublishedDataSetBase eventDataSet = new PublishedDataSetBase();
                    _configurationClient.AddPublishedDataSetEvent(eventFields, conf.EventTypeId, $"{machineName}.{objectName}.{conf.EventTypeName}", objectId, out eventDataSet);

                    DataSetWriterDefinition writerEvent = new DataSetWriterDefinition();
                    _configurationClient.AddWriter(writerGroup, $"{writerGroup.GroupName}.{objectName}.{conf.EventTypeName}", eventDataSet.Name, $"{writerGroup.QueueName}/{objectName}/{conf.EventTypeName}", out writerEvent);
                }
            }

        }

        /// <summary>
        /// Configure the pub sub items for an event generated by an object
        /// </summary>
        /// <param name="machineName">The name of the machine module</param>
        /// <param name="objectName">The name of the object that generates the event</param>
        /// <param name="objectTypeName">The name of the object type</param>
        /// <param name="objectId">The nodeId of the object</param>
        public void ConfigureObjectEvents(string machineName, string objectName, string objectTypeName, NodeId objectId)
        {
            _configurationClient.AddWriterGroup(_mqttConnection, $"{machineName}.{objectTypeName}s.Events", $"{_topicPrefix}{machineName}/{objectTypeName}s/Events", out DataSetWriterGroup writerGroupEvents);
            _configurationClient.EnableWriterGroup(writerGroupEvents.Name);
            ConfigureObjectEvents(machineName, objectName, objectId, writerGroupEvents);
        }
        #endregion

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

        private List<ReferenceDescription> Browse(NodeId startNodeId, uint nodeClassMask)
        {
            return CommonFunctions.Browse(_browseSession, startNodeId, nodeClassMask);
        }

        private List<ReferenceDescription> Browse(ExpandedNodeId startNodeId, uint nodeClassMask)
        {
            return CommonFunctions.Browse(_browseSession, startNodeId, nodeClassMask);
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
}
