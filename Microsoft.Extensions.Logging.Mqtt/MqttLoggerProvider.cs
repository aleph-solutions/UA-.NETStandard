using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Extensions.Logging.Mqtt
{
    [ProviderAlias("Mqtt")]
    public class MqttLoggerProvider : ILoggerProvider
    {
        private readonly Func<string, LogLevel, bool> _filter;

        public MqttLoggerProvider()
        {
            _filter = null;
        }

        public ILogger CreateLogger(string name)
        {
            return new MqttLogger(name, _filter);
        }

        public void Dispose()
        {

        }
    }
}
