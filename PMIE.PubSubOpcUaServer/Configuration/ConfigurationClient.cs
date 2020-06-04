using Microsoft.Extensions.Logging;
using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.PubSub;
using Opc.Ua.PubSub.Definitions;
using Opc.Ua.Server;
using PMIE.PubSubOpcUaServer.PubSub;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace PMIE.PubSubOpcUaServer.Configuration
{
    public class ConfigurationClient
    {
        private InternalPubSubUaClient _internalClient;
        private Opc.Ua.Client.Session _pubSubSession;
        private IServerInternal _pubSubServer;

        private List<PublishedDataSetBase> _datasets;
        private List<DataSetWriterGroup> _writerGroups;
        private List<DataSetWriterDefinition> _writers;
        private ILoggerFactory _loggerFactory;
        private ILogger<ConfigurationClient> _logger;

        public ConfigurationClient(IServerInternal pubSubServer, ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
            _logger = _loggerFactory.CreateLogger<ConfigurationClient>();
            _pubSubServer = pubSubServer;
        }

        /// <summary>
        /// Initializes the configuration client
        /// </summary>
        public void InitializeClient()
        {
            try
            {
                _internalClient = new InternalPubSubUaClient(_loggerFactory);

                //TODO: [ALEPH] move session creation into InternalPubSubUaClient
                //Select the endpoint to connect to the server
                var selectedEndpoint = CoreClientUtils.SelectEndpoint(_pubSubServer.EndpointAddresses.First().ToString(), false);
                var endpointConfiguration = EndpointConfiguration.Create(_internalClient.Configuration);
                var endpoint = new ConfiguredEndpoint(null, selectedEndpoint, endpointConfiguration);

                var session = Opc.Ua.Client.Session.Create(_internalClient.Configuration, endpoint, false, "ConfigurationClient", 60000, new UserIdentity(new AnonymousIdentityToken()), null).Result;
                _internalClient.Session = session;

                //TODO: [ALEPH] _pubSubSession is not needed, use m_clientAdaptor.Session instead
                _pubSubSession = session;

                _internalClient.BrowserNodeControl = new BrowseNodeControl(session);
            }
            catch (Exception ex)
            {
                _logger.LogError($"ConfigurationClient...Initialize...Exception: {ex}");
            }
        }

        /// <summary>
        /// Initialiazes the MQTT connection
        /// </summary>
        /// <param name="connectionName">The name of the connection</param>
        /// <param name="brokerAddress">The address of the MQTT broker</param>
        /// <param name="mqttConnection">The instance of the MQTT connection</param>
        /// <returns>The nodeId of the connection node</returns>
        public NodeId InitializeMQTTConnection(string connectionName, string brokerAddress, string publisherId, out Connection mqttConnection)
        {
            mqttConnection = new Connection()
            {
                Name = connectionName,
                Address = brokerAddress,
                TransportProfile = Constants.PUBSUB_MQTT_JSON,
                ConnectionType = "1",   //Broker Type
                NetworkInterface = "eth",
                PublisherDataType = 0, //String
                Children = new System.Collections.ObjectModel.ObservableCollection<PubSubConfiguationBase>(),
                AuthenticationProfileUri = String.Empty,
                ResourceUri = String.Empty,
                PublisherId = publisherId
            };

            var connectionRes = _internalClient.AddConnection(mqttConnection, out NodeId connectionNodeId);
            if (!String.IsNullOrEmpty(connectionRes))
            {
                _logger.LogError("ConfigurationClient InitializeMQTTConnection...Error during the connection with the broker");
                return null;
            }
            _logger.LogInformation($"Added connection. Connection NodeId: {connectionNodeId}");
            mqttConnection.ConnectionNodeId = connectionNodeId;
            return connectionNodeId;
        }

        /// <summary>
        /// Adds a writer group
        /// </summary>
        /// <param name="parent">The connection of the writer group</param>
        /// <param name="groupName">The name of the group</param>
        /// <param name="queueName">The MQTT topic related to the writer group</param>
        /// <param name="datasetWriterGroup">The WriterGroup instance</param>
        /// <returns>The NodeId of the writer group node</returns>
        public NodeId AddWriterGroup(Connection parent, string groupName, string queueName, out DataSetWriterGroup datasetWriterGroup)
        {
            Console.WriteLine($"Configuration Client...AddWriterGroup...Name: {groupName}");
            datasetWriterGroup = new DataSetWriterGroup()
            {
                Name = groupName,
                GroupName = groupName,
                ParentNode = parent,
                JsonNetworkMessageContentMask = 11, //Include DatasetMessage and Network message header,
                MaxNetworkMessageSize = 15000,
                MessageSecurityMode = 1,
                MessageSetting = 1,
                PublishingInterval = 1000,
                KeepAliveTime = 5000,
                QueueName = queueName,
                TransportSetting = 1,
                WriterGroupId = _writerGroups == null ? 1 : _writerGroups.Count + 1,
                SecurityGroupId = "0",
            };

            var resGroup = _internalClient.AddWriterGroup(datasetWriterGroup, out NodeId groupId);
            if (!String.IsNullOrEmpty(resGroup))
            {
                _logger.LogError($"ConfigurationClient AddWriterGroup...An error occured adding the group {datasetWriterGroup.Name}");
                return null;
            }
            _logger.LogInformation($"Added writer group {datasetWriterGroup.Name}. Group NodeId: {groupId}");
            datasetWriterGroup.GroupId = groupId;

            if (groupId != null)
            {
                if (_writerGroups == null) _writerGroups = new List<DataSetWriterGroup>();
                _writerGroups.Add(datasetWriterGroup);
            }

            return groupId;
        }

        /// <summary>
        /// Add a published data set
        /// </summary>
        /// <param name="itemList">The list of fields of the dataset</param>
        /// <param name="datasetName">The name of the Published Dataset</param>
        /// <param name="publishedDataSet">The output Dataset</param>
        public void AddPublishedDataSet(List<DataSetFieldConfiguration> itemList, string datasetName, out PublishedDataSetBase publishedDataSet)
        {
            _logger.LogDebug($"ConfigurationClient AddPublishedDataSet {datasetName}...");
            try
            {
                var datasetItems = new ObservableCollection<PublishedDataSetItemDefinition>();

                foreach (var item in itemList)
                {
                    datasetItems.Add(new PublishedDataSetItemDefinition(new PublishedDataSetBase())
                    {
                        Name = item.Name,
                        Attribute = item.Attribute,
                        PublishVariableNodeId = item.SourceNodeId,
                        SamplingInterval = item.SamplingInterval
                    });
                }

                publishedDataSet = _internalClient.AddPublishedDataSet(datasetName, datasetItems);

                if (publishedDataSet != null)
                {
                    if (_datasets == null) _datasets = new List<PublishedDataSetBase>();
                    _datasets.Add(publishedDataSet);
                }
                _logger.LogInformation($"ConfigurationClient AddPublishedDataSet {datasetName}...completed");
            }
            catch (Exception ex)
            {
                publishedDataSet = null;
                _logger.LogError($"ConfigurationClient AddPublishedDataSet {datasetName}...Exception: {ex}");
            }
        }

        /// <summary>
        /// Add a Published Dataset for an event 
        /// </summary>
        /// <param name="eventFields">The event's fields to be published</param>
        /// <param name="eventTypeId">The event type nodeId</param>
        /// <param name="datasetName">The name of the Dataset</param>
        /// <param name="eventNotifier">The nodeId of the object that generates the event</param>
        /// <param name="publishedDataSet">The output Dataset</param>
        public void AddPublishedDataSetEvent(List<DataSetEventFieldConfiguration> eventFields, NodeId eventTypeId, string datasetName, NodeId eventNotifier, out PublishedDataSetBase publishedDataSet)
        {
            _logger.LogDebug($"ConfigurationClient AddPublishedDataSetEvent {datasetName}...");
            try
            {
                var selectedFields = new ObservableCollection<PublishedEventSet>();
                foreach (var field in eventFields)
                {
                    selectedFields.Add(new PublishedEventSet()
                    {
                        Name = field.Name,
                        BrowsePath = field.BrowsePath,
                    });
                }

                ContentFilter whereClause = new ContentFilter();
                ContentFilterElement typeClause = whereClause.Push(FilterOperator.OfType, eventTypeId);

                publishedDataSet = _internalClient.AddPublishedDataSetEvents(datasetName, eventNotifier, eventTypeId, selectedFields, whereClause);
                _logger.LogInformation($"ConfigurationClient AddPublishedDataSetEvent {datasetName}...completed");
            }
            catch (Exception ex)
            {
                publishedDataSet = null;
                _logger.LogError($"ConfigurationClient AddPublishedDataSetEvent {datasetName}...Exception: {ex}");
            }
        }

        /// <summary>
        /// Adds a DataSetWriter
        /// </summary>
        /// <param name="parent">The Group the wirter belongs to</param>
        /// <param name="writerName">The name of the writer</param>
        /// <param name="datasetName">The name of the PublishedDataSet to be used in the writer</param>
        /// <param name="writer">The writer instances</param>
        /// <returns></returns>
        public NodeId AddWriter(DataSetWriterGroup parent, string writerName, string datasetName, uint keyframeCount, out DataSetWriterDefinition writer)
        {
            return AddWriter(parent, writerName, datasetName, parent.QueueName, keyframeCount, out writer);
        }

        /// <summary>
        /// Adds a DataSetWriter
        /// </summary>
        /// <param name="parent">The Group the wirter belongs to</param>
        /// <param name="writerName">The name of the writer</param>
        /// <param name="datasetName">The name of the PublishedDataSet to be used in the writer</param>
        /// <param name="queueName">The MQTT topic related to the writer</param>
        /// <param name="writer">The writer instances</param>
        /// <returns></returns>
        public NodeId AddWriter(DataSetWriterGroup parent, string writerName, string datasetName, string queueName, uint keyframeCount, out DataSetWriterDefinition writer)
        {
            var datasetId = CommonFunctions.GetChildId(_internalClient.Session, new NodeId(17371), datasetName);
            if (datasetId != null)
            {
                writer = new DataSetWriterDefinition()
                {
                    AuthenticationProfileUri = String.Empty,
                    DataSetContentMask = 31,
                    DataSetName = datasetName,
                    DataSetWriterName = writerName,
                    MessageSetting = 31,
                    Name = writerName,
                    ParentNode = parent,
                    PublisherDataSetId = datasetId.ToString(),
                    PublisherDataSetNodeId = datasetId,
                    QueueName = queueName,
                    ResourceUri = String.Empty,
                    TransportSetting = 1,
                    DataSetWriterId = Convert.ToUInt16(_writers == null ? 1 : _writers.Count + 1), //Unique id of the writer
                    KeyFrameCount = keyframeCount //Send a keyframe every 10 messages
                };


                var res = _internalClient.AddDataSetWriter(parent.GroupId, writer, out NodeId writerNodeId, out int revisedKeyframeCount);
                if (!String.IsNullOrEmpty(res))
                {
                    _logger.LogError($"ConfigurationClient AddWriter...An error occured adding the writer {writerName}");
                    return null;
                }
                _logger.LogInformation($"Added writer {writer.Name}. Writer NodeId: {writerNodeId}");
                writer.WriterNodeId = writerNodeId;

                if (writerNodeId != null)
                {
                    if (_writers == null) _writers = new List<DataSetWriterDefinition>();
                    _writers.Add(writer);
                }

                return writerNodeId;
            }
            else
            {
                _logger.LogError($"ConfigurationClient AddWriter...Dataset {datasetName} does not exist");

                writer = null;
                return null;
            }
        }

        /// <summary>
        /// Enables a writer group
        /// </summary>
        /// <param name="groupName">The name of the group to enable</param>
        public void EnableWriterGroup(string groupName)
        {
            var group = _writerGroups.FirstOrDefault(x => x.Name == groupName);

            if (group != null)
            {
                //search for the StatusId
                var statusId = CommonFunctions.GetChildId(_pubSubSession, group.GroupId, "Status");

                //search for the methodId
                var methodId = CommonFunctions.GetChildId(_pubSubSession, statusId, "Enable");
                _internalClient.EnablePubSubState(new MonitorNode()
                {
                    ParentNodeId = statusId,
                    EnableNodeId = methodId
                });
            }

        }

        /// <summary>
        /// Enables a writer
        /// </summary>
        /// <param name="writerName">The name of the writer to enable</param>
        public void EnableWriter(string writerName)
        {
            var writer = _writers.FirstOrDefault(x => x.Name == writerName);
            EnableWriter(writer);
        }

        /// <summary>
        /// Enables a writer
        /// </summary>
        /// <param name="writer">The definition of the writer to enable</param>
        public void EnableWriter(DataSetWriterDefinition writer)
        {
            if (writer != null)
            {
                //search for the StatusId
                var statusId = CommonFunctions.GetChildId(_pubSubSession, writer.WriterNodeId, "Status");

                //search for the methodId
                var methodId = CommonFunctions.GetChildId(_pubSubSession, statusId, "Enable");
                _internalClient.EnablePubSubState(new MonitorNode()
                {
                    ParentNodeId = statusId,
                    EnableNodeId = methodId
                });
            }
        }

        /// <summary>
        /// Enables all the writers
        /// </summary>
        public void EnableAllWriters()
        {
            foreach (var writer in _writers)
            {
                EnableWriter(writer);
            }
        }

        /// <summary>
        /// Add an extension field to the Dataset
        /// </summary>
        /// <param name="publishedDataSet">The Published Dataset to be extended</param>
        /// <param name="fieldName">The name of the field to add</param>
        /// <param name="fieldValue">The static value of the field</param>
        /// <returns>The NodeId of the node of the extended field in the address space</returns>
        public NodeId AddExtensionField(PublishedDataSetBase publishedDataSet, string fieldName, object fieldValue)
        {
            return _internalClient.AddExtensionField(publishedDataSet, fieldName, fieldValue);
        }
    }
}
