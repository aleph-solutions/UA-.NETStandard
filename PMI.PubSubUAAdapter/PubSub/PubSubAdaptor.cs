using AMQPTransportDataSource;
using DataSource;
using MQTTTransportDataSource;
using Opc.Ua.Client;
using Opc.Ua.Publisher;
using Opc.Ua.Subscriber;
using PMI.PubSubUAAdapter.Configuration;
using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using UDPTransportDataSource;

namespace Opc.Ua.Sample.PubSub
{
    public class ApplicationStartSettings
    {
        public string EndpointUrl = String.Empty;

        public int Timeout = System.Threading.Timeout.Infinite;
        public bool AutoAccept = true;
    }
    public class PubSubAdaptor
    { 
        Dictionary<NodeId, PublishSubscribeMap> DicUAPublisherSubscriber;
        Dictionary<NodeId, Subscription> Dic_Subscription;
        private ApplicationConfiguration m_configuration;
        private ServiceMessageContext m_messageContext;
        private ConfiguredEndpointCollection m_configuredEndpointCollection;
        private List<MonitoredItem> LstMonitoredItems = new List<MonitoredItem>();
        private Session m_session;
        X509Certificate2 m_Servercertificate;

        public ApplicationConfiguration Configuration
        {
            get
            {
                return m_configuration;
            }
        }

        public Session Session {
            get
            {
                return m_session;
            }
        }

        public PubSubAdaptor(X509Certificate2 certificate)
        {
            DicUAPublisherSubscriber = new Dictionary<NodeId, PublishSubscribeMap>();
            Dic_Subscription = new Dictionary<NodeId, Subscription>();
            m_Servercertificate = certificate;

        }
        public async Task Start(ApplicationStartSettings settings)
        {
            m_configuration = await CreateApplicationConfiguration(settings);
            var selectedEndpoint = CoreClientUtils.SelectEndpoint(settings.EndpointUrl, false, settings.Timeout);
            var endpointConfiguration = EndpointConfiguration.Create(m_configuration);
            var endpoint = new ConfiguredEndpoint(null, selectedEndpoint, endpointConfiguration);

            m_configuration.CertificateValidator.CertificateValidation += CertificateValidator_CertificateValidation;

            m_session = await Session.Create(m_configuration, endpoint, false, "OPC UA Sample Publisher " + new Random().Next(), 60000, new UserIdentity(new AnonymousIdentityToken()), null);


        }
        private async Task<ApplicationConfiguration> CreateApplicationConfiguration(ApplicationStartSettings settings)
        {
            CertificateIdentifier applicationCertificate = new CertificateIdentifier
            {
                StoreType = "Directory",
                StorePath = "../../../../../pki/own",
                SubjectName = "CN=" + "PMI.PubSubAdaptor Client" + ",DC=" + Environment.MachineName
            };

            Utils.SetTraceOutput(Utils.TraceOutput.DebugAndFile);

            var config = new ApplicationConfiguration()
            {
                ApplicationName = "PMI.PubSubAdaptor Client",
                ApplicationType = ApplicationType.Client,
                ApplicationUri = "urn:" + Utils.GetHostName() + ":PMI:PubSubAdaptor",
                SecurityConfiguration = new SecurityConfiguration
                {
                    ApplicationCertificate = applicationCertificate,
                    TrustedPeerCertificates = new CertificateTrustList
                    {
                        StoreType = "Directory",
                        StorePath = "../../../../../pki/trusted"
                    },
                    TrustedIssuerCertificates = new CertificateTrustList
                    {
                        StoreType = "Directory",
                        StorePath = "../../../../../pki/issuers"
                    },
                    RejectedCertificateStore = new CertificateTrustList
                    {
                        StoreType = "Directory",
                        StorePath = "../../../../../pki/rejected"
                    },
                    NonceLength = 32,
                    AutoAcceptUntrustedCertificates = settings.AutoAccept
                },
                TransportConfigurations = new TransportConfigurationCollection(),
                TransportQuotas = new TransportQuotas { OperationTimeout = 15000 },
                ClientConfiguration = new ClientConfiguration { DefaultSessionTimeout = 60000 }
            };

            await config.Validate(ApplicationType.Client);

            bool haveAppCertificate = config.SecurityConfiguration.ApplicationCertificate.Certificate != null;

            if (haveAppCertificate)
            {
                config.ApplicationUri = Utils.GetApplicationUriFromCertificate(config.SecurityConfiguration.ApplicationCertificate.Certificate);

                if (config.SecurityConfiguration.AutoAcceptUntrustedCertificates)
                {
                    config.CertificateValidator.CertificateValidation += new CertificateValidationEventHandler(CertificateValidator_CertificateValidation);
                }
            }
            else
            {
                //Log("WARN: missing application certificate, using unsecure connection.");
            }

            Utils.SetTraceMask(Utils.TraceMasks.None);

            return config;
        }
		 public void AddDataSetReader(DataSetReaderState dataSetReaderState, Opc.Ua.Core.SubscriberDelegate subscriberDelegate)
        {
            PublishSubscribeMap _PublishSubscribeMap = DicUAPublisherSubscriber[(dataSetReaderState.Parent as BaseInstanceState).Parent.NodeId];
            _PublishSubscribeMap.Subscriber.AddDataSetReader(dataSetReaderState, subscriberDelegate);
        }
        private void CertificateValidator_CertificateValidation(CertificateValidator validator, CertificateValidationEventArgs e)
        {
            // Log("Received Certificate: {0} {1}", e.Certificate.Subject, e.Error.StatusCode);
            e.Accept = true;
        }

        private Subscription CreateSubscription(string name)
        {
            Subscription subscription  = new Subscription();

            subscription.PublishingInterval = 100;
            subscription.PublishingEnabled = true;
            subscription.KeepAliveCount = 100;
            subscription.LifetimeCount = 1000;
            subscription.MaxNotificationsPerPublish = 10000;

            m_session.AddSubscription(subscription);
            subscription.Create();
            return subscription;

        }
        
        public void AddDataSetWriter(DataSetWriterState dataSetWriterState)
        {
            PublishSubscribeMap _PublishSubscribeMap = DicUAPublisherSubscriber[(dataSetWriterState.Parent as BaseInstanceState).Parent.NodeId];
            _PublishSubscribeMap.Publisher.AddDataSetWriter(dataSetWriterState);
        }
        public void RemoveDataSetReader(DataSetReaderState dataSetReaderState)
        {
            PublishSubscribeMap _PublishSubscribeMap = DicUAPublisherSubscriber[(dataSetReaderState.Parent as BaseInstanceState).Parent.NodeId];
            _PublishSubscribeMap.Subscriber.RemoveDataSetReader(dataSetReaderState);
        }
        public void RemoveDataSetWriter(DataSetWriterState dataSetWriterState)
        {
            PublishSubscribeMap _PublishSubscribeMap = DicUAPublisherSubscriber[(dataSetWriterState.Parent as BaseInstanceState).Parent.NodeId];
            _PublishSubscribeMap.Publisher.RemoveDataSetWriter(dataSetWriterState);
        }
        public void AddWriterGroup(WriterGroupState writerGroupState)
        {
            PublishSubscribeMap _PublishSubscribeMap = DicUAPublisherSubscriber[writerGroupState.Parent.NodeId];
            _PublishSubscribeMap.Publisher.AddWriterGroup(writerGroupState,ref LstMonitoredItems);
        }
        public void RemoveGroup(BaseInstanceState  groupState)
        {
            PublishSubscribeMap _PublishSubscribeMap = DicUAPublisherSubscriber[groupState.Parent.NodeId];
            _PublishSubscribeMap.Publisher.RemoveGroup(groupState);
            List<BaseInstanceState> LstChildren = new List<BaseInstanceState>();
            groupState.GetChildren(null, LstChildren);
            foreach(BaseInstanceState instancestate in LstChildren)
            {
                if(instancestate is DataSetReaderState)
                {
                    _PublishSubscribeMap.Subscriber.RemoveDataSetReader(instancestate as DataSetReaderState);
                }
            }
        }
        
        public void RemoveConnection(PubSubConnectionState pubSubConnectionState)
        {
            PublishSubscribeMap _PublishSubscribeMap = DicUAPublisherSubscriber[pubSubConnectionState.NodeId];
            _PublishSubscribeMap.Publisher.RemoveConnection(pubSubConnectionState);
            DicUAPublisherSubscriber.Remove(pubSubConnectionState.NodeId);
        }
        public void AddConnection(PubSubConnectionState pubSubConnectionState)
        {
            Console.WriteLine($"PubSubServer...Add Connection...");
            PublishSubscribeMap _PublishSubscribeMap = new PublishSubscribeMap(m_Servercertificate);

            DicUAPublisherSubscriber[pubSubConnectionState.NodeId] = _PublishSubscribeMap;
            IDataSource dataSource = null; 
            if(pubSubConnectionState.TransportSettings is DatagramConnectionTransportState)
            {
                UDPDataSource udpdataSource = new UDPDataSource();
                string address = (pubSubConnectionState.Address as NetworkAddressUrlState).Url.Value;
                
                bool isInitalized= udpdataSource.Initialize("Uadp", address);  
                if(isInitalized)
                {
                    pubSubConnectionState.Status.State.Value = PubSubState.Operational;
                }
                else
                {
                    pubSubConnectionState.Status.State.Value = PubSubState.Error;
                }
                dataSource = udpdataSource;
            }
            else if(pubSubConnectionState.TransportSettings is BrokerConnectionTransportState)
            {
                Console.WriteLine($"PubSubServer...Add Connection...Broker transport type");

                string address = (pubSubConnectionState.Address as NetworkAddressUrlState).Url.Value;
                if (Convert.ToString(pubSubConnectionState.TransportProfileUri.Value).ToLower().Contains("mqtt"))
                {
                    Console.WriteLine($"PubSubServer...Add Connection...MQTT transport");

                    MQTTDataSource mQTTtDataSource = new MQTTDataSource();
                    Console.WriteLine($"PubSubServer...Add Connection...MQTT transport...TransportProfileUri: {pubSubConnectionState.TransportProfileUri.Value}");
                    string format = Convert.ToString(pubSubConnectionState.TransportProfileUri.Value).ToLower().Contains("uadp") ? "uadp" : "json";
                    Console.WriteLine($"PubSubServer...Add Connection...MQTT transport...Format: {format}");
                    
                    Console.WriteLine($"PubSubServer...Add Connection...MQTT transport...mQTTtDataSource: {mQTTtDataSource}");
                    bool isInitalized = false;
                    try
                    {
                        isInitalized = mQTTtDataSource.Initialize(format, address);
                    }
                    catch (Exception ex) 
                    {
                        Console.WriteLine($"PubSubServer...Add Connection...MQTT transport...Initialiazing data source...Exception: {ex}");
                    }

                    if (isInitalized)
                    {
                        Console.WriteLine($"PubSubServer...Add Connection...MQTT broker operational");
                        pubSubConnectionState.Status.State.Value = PubSubState.Operational;
                    }
                    else
                    {
                        Console.WriteLine($"PubSubServer...Add Connection...MQTT broker error");
                        pubSubConnectionState.Status.State.Value = PubSubState.Error;
                    }
                    dataSource = mQTTtDataSource;
                }
                else if (Convert.ToString(pubSubConnectionState.TransportProfileUri.Value).ToLower().Contains("amqp"))
                {
                    AMQPDataSource amqpDataSource = new AMQPDataSource();
                    string format = Convert.ToString(pubSubConnectionState.TransportProfileUri.Value).ToLower().Contains("uadp") ? "uadp" : "json";
                    bool isInitalized = amqpDataSource.Initialize(format, address).Result;
                    if (isInitalized)
                    {
                        pubSubConnectionState.Status.State.Value = PubSubState.Operational;
                    }
                    else
                    {
                        pubSubConnectionState.Status.State.Value = PubSubState.Error;
                    }
                    dataSource = amqpDataSource;
                }
            }
            _PublishSubscribeMap.Publisher.AddConnection(pubSubConnectionState, dataSource);
            _PublishSubscribeMap.Subscriber.AddConnection(pubSubConnectionState, dataSource);

        }

        public void AddPublishedDataItems(PublishedDataItemsState publishedDataItemsState)
        {
          Subscription subscription=  CreateSubscription("PublishedDataItems_"+ publishedDataItemsState.DisplayName.Text);
            Dic_Subscription[publishedDataItemsState.NodeId] = subscription;
            PublishedVariableDataType[] PublishedVariableDataTypearray = publishedDataItemsState.PublishedData.Value as PublishedVariableDataType[];
            foreach (var ii in PublishedVariableDataTypearray)
            {
                if (NodeId.IsNull(ii.PublishedVariable))
                {
                    continue;
                }

                var monitoredItem = new MonitoredItem()
                {
                    StartNodeId = ii.PublishedVariable,
                    AttributeId = (ii.AttributeId == 0) ? Attributes.Value : ii.AttributeId,
                    MonitoringMode = MonitoringMode.Reporting,
                    SamplingInterval = (int)(ii.SamplingIntervalHint),
                    DiscardOldest = true,
                    QueueSize = 0,
                    Handle = ii
                };

                if (ii.DeadbandType != (uint)DeadbandType.None)
                {
                    monitoredItem.Filter = new DataChangeFilter
                    {
                        DeadbandType = ii.DeadbandType,
                        DeadbandValue = ii.DeadbandValue
                    };
                }
                subscription.AddItem(monitoredItem);
                LstMonitoredItems.Add(monitoredItem);
            }
            subscription.ApplyChanges();
        }
        public void RemovePublishedDataItems(PublishedDataItemsState publishedDataItemsState)
        {
            Subscription subscription= Dic_Subscription[publishedDataItemsState.NodeId];
            m_session.RemoveSubscription(subscription);
        }
        public void CreateTargetVariables(NodeId ConnectionStateId, NodeId readerStateNodeId, FieldTargetDataType[] fieldTargetDataTypes)
        {
            PublishSubscribeMap _PublishSubscribeMap = DicUAPublisherSubscriber[ConnectionStateId];
            _PublishSubscribeMap.Subscriber.CreateTargetVariables(ConnectionStateId, readerStateNodeId, fieldTargetDataTypes);
        }
        public void  RemoveFieldTargetDataType(NodeId ConnectionStateId, NodeId readerStateNodeId)
        {
            PublishSubscribeMap _PublishSubscribeMap = DicUAPublisherSubscriber[ConnectionStateId];
            _PublishSubscribeMap.Subscriber.RemoveFieldTargetDataType(readerStateNodeId);
        }
    }

    public class PublishSubscribeMap
    {
        public IUAPublisher Publisher = null;
        public IUASubscriber Subscriber = null;
      
        // public IUAPublisher IUAPublisher { get; set; }

        public PublishSubscribeMap(X509Certificate2 servercertificate)
        {
            Publisher = new UAPublisher(servercertificate);
            Subscriber = new UASubscriber(servercertificate);
        }

    }
}
