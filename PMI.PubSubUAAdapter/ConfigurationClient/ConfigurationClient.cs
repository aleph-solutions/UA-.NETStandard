using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.CommonFunctions;
using Opc.Ua.Server;
using PubSubBase.Definitions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMI.PubSubUAAdapter.Configuration
{
    public class ConfigurationClient
    {
        private ClientAdaptor.OPCUAClientAdaptor m_clientAdaptor;
        private Opc.Ua.Client.Session _pubSubSession;
        private IServerInternal _pubSubServer;

        private Connection _mqttConnection;
        private List<PublishedDataSetBase> _datasets;
        private List<DataSetWriterGroup> _writerGroups;
        private List<DataSetWriterDefinition> _writers;

        public ConfigurationClient(IServerInternal pubSubServer)
        {
            _pubSubServer = pubSubServer;
        }

        public void InitializeClient()
        {
            try
            {
                m_clientAdaptor = new ClientAdaptor.OPCUAClientAdaptor();

                var selectedEndpoint = CoreClientUtils.SelectEndpoint(_pubSubServer.EndpointAddresses.First().ToString(), false);
                var endpointConfiguration = EndpointConfiguration.Create(m_clientAdaptor.Configuration);
                var endpoint = new ConfiguredEndpoint(null, selectedEndpoint, endpointConfiguration);

                var session = Opc.Ua.Client.Session.Create(m_clientAdaptor.Configuration, endpoint, false, "ConfigurationClient", 60000, new UserIdentity(new AnonymousIdentityToken()), null).Result;
                m_clientAdaptor.Session = session;
                _pubSubSession = session;

                m_clientAdaptor.BrowserNodeControl = new ClientAdaptor.BrowseNodeControl(session);

                //if (session != null)
                //{
                //    StartSample();
                //}
            }
            catch(Exception ex)
            {
                Console.WriteLine($"ConfigurationClient...Initialize...Exception: {ex}");
            }
        }

        public void StartSample()
        {
            //Initialiaze the MQTT connection
            var connectionId = InitializeMQTTConnection("mqtt", "40.91.255.161:1883", out _mqttConnection);

            //Add a sample group
            var groupId = AddWriterGroup(_mqttConnection, "group1", "test-configurator", out DataSetWriterGroup group);
            EnableWriterGroup(group.Name);

            //Add dataset
            var items = new Dictionary<string, NodeId>();
            var datasetName = "dataset1";
            items.Add("BufferLength.AnalogMeasurement", new NodeId(7226, 5));
            AddPublishedDataSet(items, datasetName, out PublishedDataSetBase dataset);

            //Add writer
            var writerId = AddWriter(group, "writer1", datasetName, out DataSetWriterDefinition writer);
            EnableWriter(writer.Name);
        }

        /// <summary>
        /// Initialiazes the MQTT connection
        /// </summary>
        /// <param name="connectionName">The name of the connection</param>
        /// <param name="brokerAddress">The address of the MQTT broker</param>
        /// <param name="mqttConnection">The instance of the MQTT connection</param>
        /// <returns>The nodeId of the connection node</returns>
        public NodeId InitializeMQTTConnection(string connectionName, string brokerAddress, out Connection mqttConnection)
        {
            mqttConnection = new Connection() 
            {
                Name = connectionName,
                Address = brokerAddress, 
                TransportProfile = ClientAdaptor.Constants.PUBSUB_MQTT_JSON,
                ConnectionType = "1",   //Broker Type
                NetworkInterface = "eth",
                PublisherDataType = 0, //String
                Children =  new System.Collections.ObjectModel.ObservableCollection<PubSubConfiguationBase>(),
                AuthenticationProfileUri = String.Empty,
                ResourceUri = String.Empty,
                PublisherId = "Bay40"
            };

            var connectionRes = m_clientAdaptor.AddConnection(mqttConnection, out NodeId connectionNodeId);
            Console.WriteLine($"Added connection. Result: {connectionRes}, Connection NodeId: {connectionNodeId}");
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
                MaxNetworkMessageSize = 1500,
                MessageSecurityMode = 1,
                MessageSetting = 1,
                PublishingInterval = 1000,
                QueueName = queueName,
                TransportSetting = 1,
                WriterGroupId = _writerGroups == null ? 1 : _writerGroups.Count + 1,
                SecurityGroupId = "0",
            };

            var resGroup = m_clientAdaptor.AddWriterGroup(datasetWriterGroup, out NodeId groupId);
            Console.WriteLine($"Added writer group {datasetWriterGroup.Name}. Result: {resGroup}, Group NodeId: {groupId}");
            datasetWriterGroup.GroupId = groupId;

            if(groupId != null)
            {
                if (_writerGroups == null) _writerGroups = new List<DataSetWriterGroup>();
                _writerGroups.Add(datasetWriterGroup);
            }

            return groupId;
        }

        /// <summary>
        /// Adds a PublishedDataSet
        /// </summary>
        /// <param name="itemList">The items of the dataset</param>
        /// <param name="datasetName">The name of the DataSet</param>
        /// <param name="publishedDataSet">The publishedDataSet instance</param>
        public void AddPublishedDataSet(Dictionary<string, NodeId> itemList, string datasetName, out PublishedDataSetBase publishedDataSet)
        {
            AddPublishedDataSet(itemList, new Dictionary<string, uint>(), datasetName, out publishedDataSet);
        }

        public void AddPublishedDataSet(Dictionary<string, NodeId> itemList, Dictionary<string, uint> attributesList, string datasetName, out PublishedDataSetBase publishedDataSet)
        {
            Console.WriteLine($"ConfigurationClient AddPublishedDataSet {datasetName}...");
            try
            {
                var datasetItems = new ObservableCollection<PublishedDataSetItemDefinition>();

                foreach (var item in itemList)
                {
                    var attributeId = Attributes.Value;
                    if(attributesList.Any(x => x.Key == item.Key))
                    {
                        attributeId = attributesList[item.Key];
                    }

                    datasetItems.Add(new PublishedDataSetItemDefinition(new PublishedDataSetBase())
                    {
                        Name = item.Key,
                        Attribute = attributeId,
                        PublishVariableNodeId = item.Value,
                    });
                }

                publishedDataSet = m_clientAdaptor.AddPublishedDataSet(datasetName, datasetItems);

                if (publishedDataSet != null)
                {
                    if (_datasets == null) _datasets = new List<PublishedDataSetBase>();
                    _datasets.Add(publishedDataSet);
                }
                Console.WriteLine($"ConfigurationClient AddPublishedDataSet {datasetName}...completed");

                //m_clientAdaptor.AddExtensionField(publishedDataSet, "Test", 123);
            }
            catch (Exception ex)
            {
                publishedDataSet = null;
                Console.WriteLine($"ConfigurationClient AddPublishedDataSet {datasetName}...Exception: {ex}");
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
        public NodeId AddWriter(DataSetWriterGroup parent, string writerName, string datasetName, out DataSetWriterDefinition writer)
        {
            return AddWriter(parent, writerName, datasetName, parent.QueueName, out writer);
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
        public NodeId AddWriter(DataSetWriterGroup parent, string writerName, string datasetName, string queueName, out DataSetWriterDefinition writer)
        {
            var datasetId = CommonFunctions.GetChildId(m_clientAdaptor.Session, new NodeId(17371), datasetName);
            if(datasetId != null)
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
                    DataSetWriterId = Convert.ToUInt16(_writers == null ? 1 : _writers.Count + 1),
                };


                var res = m_clientAdaptor.AddDataSetWriter(parent.GroupId, writer, out NodeId writerNodeId, out int keyframeCount);
                Console.WriteLine($"Added writer {writer.Name}. Result: {res}, Writer NodeId: {writerNodeId}");
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
                Console.WriteLine($"ConfigurationClient AddWriter...Dataset {datasetName} does not exist");

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

            if(group != null)
            {
                //search for the StatusId
                var statusId = CommonFunctions.GetChildId(_pubSubSession, group.GroupId, "Status");

                //search for the methodId
                var methodId = CommonFunctions.GetChildId(_pubSubSession, statusId, "Enable");
                m_clientAdaptor.EnablePubSubState(new MonitorNode() {
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

            if (writer != null)
            {
                //search for the StatusId
                var statusId = CommonFunctions.GetChildId(_pubSubSession, writer.WriterNodeId, "Status");

                //search for the methodId
                var methodId = CommonFunctions.GetChildId(_pubSubSession, statusId, "Enable");
                m_clientAdaptor.EnablePubSubState(new MonitorNode()
                {
                    ParentNodeId = statusId,
                    EnableNodeId = methodId
                });
            }

        }

        public NodeId AddExtensionField(PublishedDataSetBase publishedDataSet, string fieldName, object fieldValue)
        {
            return m_clientAdaptor.AddExtensionField(publishedDataSet, fieldName, fieldValue);
        }
    }
}
