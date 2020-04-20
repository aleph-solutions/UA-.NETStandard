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

namespace Opc.Ua.PubSub.Sample.ConfigurationClient
{

    
    public class ConfigurationClient
    {
        private ClientAdaptor.OPCUAClientAdaptor m_clientAdaptor;
        private IServerInternal _server;

        public ConfigurationClient(IServerInternal server)
        {
            _server = server;
        }

        public void InitializeClient()
        {
            try
            {
                m_clientAdaptor = new ClientAdaptor.OPCUAClientAdaptor();

                var selectedEndpoint = CoreClientUtils.SelectEndpoint(_server.EndpointAddresses.First().ToString(), false);
                var endpointConfiguration = EndpointConfiguration.Create(m_clientAdaptor.Configuration);
                var endpoint = new ConfiguredEndpoint(null, selectedEndpoint, endpointConfiguration);

                var session = Client.Session.Create(m_clientAdaptor.Configuration, endpoint, false, "ConfigurationClient", 60000, new UserIdentity(new AnonymousIdentityToken()), null).Result;
                m_clientAdaptor.Session = session;

                if(session != null)
                {
                    Start();
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine($"ConfigurationClient...Initialize...Exception: {ex}");
            }
        }

        public void Start()
        {
            //Initialiaze the MQTT connection
            var connection = InitializeMQTTConnection();


            //Add a sample group
            var groupId = AddSampleGroup(connection, out DataSetWriterGroup group);

            //Add dataset
            var items = new Dictionary<string, NodeId>();
            var datasetName = "dataset1";
            items.Add("BufferLength.AnalogMeasurement", new NodeId(7226, 5));
            AddDataSet(items, datasetName);

            //Add writer
            var writerId = AddWriter(group, datasetName, "test-conf");
        }

        public Connection InitializeMQTTConnection()
        {
            var mqttConnection = new Connection() 
            {
                Name = "mqtt",
                Address = "40.91.255.161:1883", 
                TransportProfile = ClientAdaptor.Constants.PUBSUB_MQTT_JSON,
                ConnectionType = "1",
                NetworkInterface = "eth",
                PublisherDataType = 0, //String
                Children =  new System.Collections.ObjectModel.ObservableCollection<PubSubConfiguationBase>(),
                AuthenticationProfileUri = String.Empty,
                ResourceUri = String.Empty,
                PublisherId = "1"
            };

            var connectionRes = m_clientAdaptor.AddConnection(mqttConnection, out NodeId connectionNodeId);
            Console.WriteLine($"Added connection. Result: {connectionRes}, Connection NodeId: {connectionNodeId}");
            mqttConnection.ConnectionNodeId = connectionNodeId;
            return mqttConnection;
        }

        public NodeId AddSampleGroup(Connection parent, out DataSetWriterGroup datasetWriterGroup)
        {
            datasetWriterGroup = new DataSetWriterGroup()
            {
                Name = "group1",
                GroupName = "group1",
                ParentNode = parent,
                JsonNetworkMessageContentMask = 3, //Include DatasetMessage and Network message header,
                MaxNetworkMessageSize = 1500,
                MessageSecurityMode = 1,
                MessageSetting = 1,
                PublishingInterval = 1000,
                QueueName = "conf-test",
                TransportSetting = 1,
                WriterGroupId = 1,
                SecurityGroupId = "0"
            };

            var resGroup = m_clientAdaptor.AddWriterGroup(datasetWriterGroup, out NodeId groupId);
            Console.WriteLine($"Added writer group {datasetWriterGroup.Name}. Result: {resGroup}, Group NodeId: {groupId}");
            datasetWriterGroup.GroupId = groupId;
            return groupId;
        }

        public void AddDataSet(Dictionary<string, NodeId> itemList, string datasetName)
        {
            var datasetItems = new ObservableCollection<PublishedDataSetItemDefinition>();

            foreach(var item in itemList)
            {
                var datasetBase = new PublishedDataSetBase()
                {
                    Name = item.Key
                };

                datasetItems.Add(new PublishedDataSetItemDefinition(datasetBase)
                {
                    Attribute = Attributes.Value,
                    PublishVariableNodeId = item.Value
                });
            }

            var pubDataSet = m_clientAdaptor.AddPublishedDataSet(datasetName, datasetItems);

        }




        public NodeId AddWriter(DataSetWriterGroup parent, string datasetName, string queueName)
        {
            var datasetId = CommonFunctions.CommonFunctions.GetChildrenId(m_clientAdaptor.Session, new NodeId(17371), datasetName);
            
            var definition = new DataSetWriterDefinition() 
            {
                AuthenticationProfileUri = String.Empty,
                DataSetContentMask = 31,
                DataSetName = datasetName,
                DataSetWriterName = "writer1",
                MessageSetting =1 ,
                Name = "writer1",
                ParentNode = parent,
                PublisherDataSetId = datasetId.ToString(),
                PublisherDataSetNodeId = datasetId,
                QueueName = queueName,
                ResourceUri = String.Empty,
                TransportSetting = 1
            };


            var res = m_clientAdaptor.AddDataSetWriter(parent.GroupId, definition, out NodeId writerNodeId, out int keyframeCount);
            Console.WriteLine($"Added writer {definition.Name}. Result: {res}, Writer NodeId: {writerNodeId}");

            return writerNodeId;
        }
    }
}
