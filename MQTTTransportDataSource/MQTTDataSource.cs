using DataSource;
using System;
using M2Mqtt;
using M2Mqtt.Messages;
using System.Text;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

namespace MQTTTransportDataSource
{
    public class MQTTDataSource : BaseDataSource
    {
        MqttClient client;
        string m_Format = "json";
        
        string[] Topics = new string[1] { "Test" };

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
            Console.WriteLine($"MQTTTransportDataSource...Initialize...");
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
                if(addressarray.Length > 1)
                {
                    port = Convert.ToInt32(addressarray[1]);
                }

                var brokerSecurityStr = Environment.GetEnvironmentVariable("BROKER_SECURITY");
                var brokerSecurity = BrokerSecurity.NoSecurity;
                if(brokerSecurityStr != null)
                {
                    var index = Convert.ToInt32(brokerSecurityStr);
                    brokerSecurity = (BrokerSecurity)index;
                }

                if(brokerSecurity == BrokerSecurity.Certificate)
                {
                    Console.WriteLine("MQTTDataSource Initialize...Certificate Security");
                    var clientCertificatePath = Environment.GetEnvironmentVariable("MQTT_CLIENT_CERT");
                    var clientCACertificatePath = Environment.GetEnvironmentVariable("MQTT_CLIENT_CA_CERT");

                    Console.WriteLine($"MQTTDataSource Initialize...Certificate Security...ClientCert path: {clientCertificatePath} ClientCACert path: {clientCACertificatePath}");
                    var clientCert = new X509Certificate2(clientCertificatePath);
                    var clientCACert = new X509Certificate2(clientCACertificatePath);

                    client = new MqttClient(Address, port, true, clientCert, clientCACert, MqttSslProtocols.TLSv1_2);
                }
                else client = new MqttClient(Address, port, false, null, null, MqttSslProtocols.None);



                client.MqttMsgPublishReceived += Client_MqttMsgPublishReceived;
                client.MqttMsgPublished += Client_MqttMsgPublished;
                client.MqttMsgSubscribed += Client_MqttMsgSubscribed;
                Console.WriteLine($"MQTTTransportDataSource...Initialize...Connecting");

                if(brokerSecurity == BrokerSecurity.UserPassword)
                {
                    var username = Environment.GetEnvironmentVariable("BROKER_USERNAME");
                    var password = Environment.GetEnvironmentVariable("BROKER_PASSWORD");

                    if (username != null && password != null)
                    {
                        client.Connect(Guid.NewGuid().ToString(), username, password);
                    }
                    else
                    {
                        Console.WriteLine($"MQTTDataSource Initialize...Username ({username}) or Password ({password}) are null");
                    }
                }
                else
                {
                    client.Connect(Guid.NewGuid().ToString());
                }

                Console.WriteLine($"MQTTTransportDataSource...Initialize...Connected");
                return true;
            }
            catch (Exception ex)
            {
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
            catch(Exception ex)
            {
                return false;
            }
           
        }

        public override bool ReceiveData(string queueName)
        {
            Topics[0] = queueName;
            client.Subscribe(Topics, new byte[] { MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE });
            return true;
        }

        #endregion
    }

    public enum BrokerSecurity { NoSecurity, UserPassword, Certificate}
}
