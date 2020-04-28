using DataSource;
using System;
using M2Mqtt;
using M2Mqtt.Messages;
using System.Text;
using System.Collections.Generic;

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
                Console.WriteLine($"MQTTTransportDataSource...Initialize...Address: {Address} IPAddress: {IPAddress}");
                if (isvalidIP)
                {
                    client = new MqttClient(IPAddress);
                }
                else
                {
                    client = new MqttClient(Address);
                }

                client.MqttMsgPublishReceived += Client_MqttMsgPublishReceived;
                client.MqttMsgPublished += Client_MqttMsgPublished;
                client.MqttMsgSubscribed += Client_MqttMsgSubscribed;
                Console.WriteLine($"MQTTTransportDataSource...Initialize...Connecting");
                client.Connect(Guid.NewGuid().ToString(), "aleph", "A13phQ");
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
}
