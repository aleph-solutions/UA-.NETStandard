using MQTTnet;
using MQTTnet.Client.Options;
using MQTTnet.Extensions.ManagedClient;
using System;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Extensions.Logging.Mqtt
{
    public class MqttLogger : ILogger
    {
        public const string ENV_VAR_MQTT_HOST = "MQTT_HOST_LOGS";
        public const string ENV_VAR_MQTT_PORT = "MQTT_PORT_LOGS";
        public const string ENV_VAR_MQTT_SECURE = "MQTT_SECURE_LOGS";
        public const string ENV_VAR_MQTT_USER = "MQTT_USER_LOGS";
        public const string ENV_VAR_MQTT_PWD = "MQTT_PWD_LOGS";
        public const string ENV_VAR_MQTT_TOPIC_LOGS = "MQTT_TOPIC_LOGS";
        
        private readonly Func<string, LogLevel, bool> _filter;
        private readonly string _name;

        private IManagedMqttClient _mqttClient;

        public string MqttBrokerEndpoint { get; set; } = "";//"192.168.3.11";
        public int MqttBrokerPort { get; set; } = 1883;
        public bool MqttBrokerSecure { get; set; } = true;
        public string MqttBrokerUser { get; set; } = "";//"192.168.3.11";
        public string MqttBrokerPassword { get; set; } = "";//"192.168.3.11";
        public string MqttTopicLogs { get; set; } = "PubSub/Logs";

        public MqttLogger(string name, Func<string, LogLevel, bool> filter)
        {
            _name = string.IsNullOrEmpty(name) ? nameof(MqttLogger) : name;

            if (!String.IsNullOrEmpty(Environment.GetEnvironmentVariable(ENV_VAR_MQTT_HOST)))
                MqttBrokerEndpoint = Environment.GetEnvironmentVariable(ENV_VAR_MQTT_HOST);
            else if(!String.IsNullOrEmpty(Environment.GetEnvironmentVariable("BROKER_IP")))
            {
                MqttBrokerEndpoint = Environment.GetEnvironmentVariable("BROKER_IP");
            }

            if (!String.IsNullOrEmpty(Environment.GetEnvironmentVariable(ENV_VAR_MQTT_PORT)))
                MqttBrokerPort = Convert.ToUInt16(Environment.GetEnvironmentVariable(ENV_VAR_MQTT_PORT));
            else if (!String.IsNullOrEmpty(Environment.GetEnvironmentVariable("BROKER_PORT")))
            {
                MqttBrokerPort = Convert.ToUInt16(Environment.GetEnvironmentVariable("BROKER_PORT"));
            }

            if (!String.IsNullOrEmpty(Environment.GetEnvironmentVariable(ENV_VAR_MQTT_SECURE)))
                MqttBrokerSecure = Convert.ToBoolean(Environment.GetEnvironmentVariable(ENV_VAR_MQTT_SECURE));
            else if (!String.IsNullOrEmpty(Environment.GetEnvironmentVariable("BROKER_ENABLE_TLS")))
            {
                MqttBrokerSecure = Convert.ToBoolean(Environment.GetEnvironmentVariable("BROKER_ENABLE_TLS"));
            }

            if (!String.IsNullOrEmpty(Environment.GetEnvironmentVariable(ENV_VAR_MQTT_USER)))
                MqttBrokerUser = Environment.GetEnvironmentVariable(ENV_VAR_MQTT_USER);
            else if (!String.IsNullOrEmpty(Environment.GetEnvironmentVariable("BROKER_USERNAME")))
            {
                MqttBrokerUser = Environment.GetEnvironmentVariable("BROKER_USERNAME");
            }

            if (!String.IsNullOrEmpty(Environment.GetEnvironmentVariable(ENV_VAR_MQTT_PWD)))
                MqttBrokerPassword = Environment.GetEnvironmentVariable(ENV_VAR_MQTT_PWD);
            else if (!String.IsNullOrEmpty(Environment.GetEnvironmentVariable("BROKER_PASSWORD")))
            {
                var cryptedPassword = Environment.GetEnvironmentVariable("BROKER_PASSWORD");
                MqttBrokerPassword = AesOperation.DecryptString($"{MqttBrokerEndpoint}_{MqttBrokerUser}", cryptedPassword);
            }

            if (!String.IsNullOrEmpty(Environment.GetEnvironmentVariable(ENV_VAR_MQTT_TOPIC_LOGS)))
                MqttTopicLogs = Environment.GetEnvironmentVariable(ENV_VAR_MQTT_TOPIC_LOGS);
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return NoopDisposable.Instance;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel != LogLevel.None
                && (_filter == null || _filter(_name, logLevel));
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            if (formatter == null)
            {
                throw new ArgumentNullException(nameof(formatter));
            }

            var message = formatter(state, exception);

            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            message = $"[{_name}] {message}";

            if (exception != null)
            {
                message += Environment.NewLine + Environment.NewLine + exception.ToString();
            }

            if (!string.IsNullOrEmpty(MqttBrokerEndpoint) && !string.IsNullOrEmpty(MqttTopicLogs))
            {
                if (_mqttClient == null) _mqttClient = GetMqttClient();
                _mqttClient.PublishAsync(new MqttApplicationMessageBuilder()
                            .WithTopic(MqttTopicLogs)
                            .WithContentType("application/json")
                            .WithPayload(Newtonsoft.Json.JsonConvert.SerializeObject(message))
                            .Build());
            }
        }

        private IManagedMqttClient GetMqttClient()
        {
            string clientId = Guid.NewGuid().ToString();
            var messageBuilder = new MqttClientOptionsBuilder()
              .WithClientId(clientId)
              .WithTcpServer(MqttBrokerEndpoint, MqttBrokerPort)
              //.WithProtocolVersion(MqttProtocolVersion.V500)
              .WithCleanSession();

            //If the broker uses certificates don't authenticate using username/password
            if (!MqttBrokerSecure) messageBuilder.WithCredentials(MqttBrokerUser, MqttBrokerPassword);

            var options = messageBuilder.Build();
            if (MqttBrokerSecure)
            {
                //Retrive CA certificate
                //var caCert = new X509Certificate(Settings.MqttBrokerCertificatePath);

                options = messageBuilder
                .WithTls(new MqttClientOptionsBuilderTlsParameters()
                {
                    //Certificates = new List<byte[]> { caCert.GetRawCertData() },
                    UseTls = true,
                    CertificateValidationCallback = (X509Certificate x, X509Chain y, SslPolicyErrors z, IMqttClientOptions o) =>
                    {
                        return true;
                    },
                })
                .Build();
            }

            var managedOptions = new ManagedMqttClientOptionsBuilder()
              .WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
              .WithClientOptions(options)
              .Build();
            var mqttClient = new MqttFactory().CreateManagedMqttClient();
            mqttClient.StartAsync(managedOptions).Wait();
            return mqttClient;
        }

        private class NoopDisposable : IDisposable
        {
            public static NoopDisposable Instance = new NoopDisposable();

            public void Dispose()
            {
            }
        }
    }
}
