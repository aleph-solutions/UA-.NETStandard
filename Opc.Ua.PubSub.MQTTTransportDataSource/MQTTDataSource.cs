using System;
using M2Mqtt;
using M2Mqtt.Messages;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using Microsoft.Extensions.Logging;
using System.Threading;

namespace Opc.Ua.PubSub
{
    public class MQTTDataSource : BaseDataSource
    {
        MqttClient client;
        string m_Format = "json";

        string[] Topics = new string[1] { "Test" };
        ILoggerFactory _loggerFactory;
        ILogger<MQTTDataSource> _logger;

        public MQTTDataSource(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger<MQTTDataSource>();
        }
        #region Private Methods

        private void Client_MqttMsgSubscribed(object sender, MqttMsgSubscribedEventArgs e)
        {

        }

        private void Client_MqttMsgPublished(object sender, MqttMsgPublishedEventArgs e)
        {

        }

        #endregion

        #region Public Methods

        public bool Initialize(string format, string address)
        {
            _logger.LogDebug($"MQTTTransportDataSource...Initialize...");
            try
            {
                m_Format = format;

                string[] addressarray = address.Split(':');

                string Address = addressarray[0].Replace("/", string.Empty);
                if (Address.ToLower() == "localhost")
                {
                    Address = "127.0.0.1";
                }
                // string BrokerHostName = "test.mosquitto.org";
                System.Net.IPAddress IPAddress;
                bool isvalidIP = System.Net.IPAddress.TryParse(Address, out IPAddress);
                if (isvalidIP)
                {
                    Address = IPAddress.ToString();
                }

                var port = 1883;
                if (addressarray.Length > 1)
                {
                    port = Convert.ToInt32(addressarray[1]);
                }

                _logger.LogDebug($"MQTTDataSource...Initialize...Broker address: {Address} Port: {port}");

                var brokerAuthStr = Environment.GetEnvironmentVariable("BROKER_AUTH");
                _logger.LogDebug($"MQTTDataSource...Initialize...Broker authentication: {brokerAuthStr} ");

                var brokerAuth = BrokerSecurity.NoSecurity;
                if (brokerAuthStr != null)
                {
                    var index = Convert.ToInt32(brokerAuthStr);
                    brokerAuth = (BrokerSecurity)index;
                }

                var enableTlsVar = Environment.GetEnvironmentVariable("BROKER_ENABLE_TLS");
                var enableTls = false;
                if (!String.IsNullOrEmpty(enableTlsVar))
                {
                    try
                    {
                        enableTls = Convert.ToBoolean(enableTlsVar);
                    }
                    catch
                    {
                        //Unable to convert the string to boolean
                        _logger.LogWarning("MQTTDataSource...Initialize...BROKER_ENABLE_TLS env var has not a valid boolean value");
                    }
                }

                if (brokerAuth == BrokerSecurity.Certificate)
                {
                    _logger.LogDebug("MQTTDataSource Initialize...Certificate Security");
                    //var clientCertificatePath = Environment.GetEnvironmentVariable("MQTT_CLIENT_CERT");
                    var caCertificatePath = Environment.GetEnvironmentVariable("MQTT_CLIENT_CA_CERT");

                    //Console.WriteLine($"MQTTDataSource Initialize...Certificate Security...ClientCert path: {clientCertificatePath} ClientCACert path: {clientCACertificatePath}");
                    //var clientCert = new X509Certificate2(clientCertificatePath);
                    var caCert = new X509Certificate(caCertificatePath);

                    client = new MqttClient(Address, port, true, null, caCert, MqttSslProtocols.TLSv1_2, Client_RemoteCertificateValidationCallback);
                }
                else if (enableTls)
                {
                    _logger.LogDebug("MQTTDataSource Initialize...TLS enabled");
                    client = new MqttClient(Address, port, false, null, null, MqttSslProtocols.TLSv1_2, Client_RemoteCertificateValidationCallback);
                }
                else client = new MqttClient(Address, port, false, null, null, MqttSslProtocols.None);



                client.MqttMsgPublishReceived += Client_MqttMsgPublishReceived;
                client.MqttMsgPublished += Client_MqttMsgPublished;
                client.MqttMsgSubscribed += Client_MqttMsgSubscribed;
                _logger.LogInformation($"MQTTTransportDataSource...Initialize...Connecting");

                var connected = false;
                do
                {
                    try
                    {
                        if (brokerAuth == BrokerSecurity.UserPassword)
                        {
                            var username = Environment.GetEnvironmentVariable("BROKER_USERNAME");
                            var cryptedPassword = Environment.GetEnvironmentVariable("BROKER_PASSWORD");

                            _logger.LogDebug($"MQTTDataSource...Initialize...Broker user: {username} cryptedPassword: {cryptedPassword}");

                            if (username != null && cryptedPassword != null)
                            {
                                var password = AesOperation.DecryptString($"{Address}_{username}", cryptedPassword);
                                client.Connect(Guid.NewGuid().ToString(), username, password);
                            }
                            else
                            {
                                _logger.LogWarning($"MQTTDataSource Initialize...Username and/or Password are null");
                                return false;
                            }
                        }
                        else
                        {
                            client.Connect(Guid.NewGuid().ToString());
                        }
                    }
                    catch(Exception ex)
                    {
                        _logger.LogError($"MQTTTransportDataSource...Error connecting to the broker. Exception: {ex}");
                    }

                    if (!client.IsConnected)
                    {
                        _logger.LogWarning("MQTTTransportDataSource...MQTT CLIENT IS NOT CONNECTED! Retry in 5s");
                        Thread.Sleep(5000);
                        //return false;
                    }
                    else
                    {
                        _logger.LogInformation($"MQTTTransportDataSource...Initialize...Connected");
                        connected = true;
                        //return true;
                    }
                } while (!connected);
                

                
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error connecting to the broker: {ex}");
                return false;
            }
        }

        public void Client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
            //string ReceivedMessage = Encoding.UTF8.GetString(e.Message);
            OnDataReceived(e.Message);
        }

        public override bool SendData(byte[] data, Dictionary<string, object> settings)
        {
            try
            {
                string topic = Convert.ToString(settings["topic"]);
                if (String.IsNullOrEmpty(topic))
                {
                    topic = "test";
                }


                //var dataStr = Encoding.UTF8.GetString(data);
                client.Publish(topic, data, MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE, true);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"MQTTTransportDataSource...SendData..Error: {ex}");
                return false;
            }

        }

        public override bool ReceiveData(string queueName)
        {
            Topics[0] = queueName;
            client.Subscribe(Topics, new byte[] { MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE });
            return true;
        }

        bool Client_RemoteCertificateValidationCallback(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            // logic for validation here
            return true;
        }

        #endregion
    }

    public enum BrokerSecurity { NoSecurity, UserPassword, Certificate }
}
