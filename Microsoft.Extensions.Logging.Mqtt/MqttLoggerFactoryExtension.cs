using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.Logging.Mqtt
{
    public static class MqttLoggerFactoryExtension
    {
        public static ILoggingBuilder AddMqtt(this ILoggingBuilder builder)
        {
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, MqttLoggerProvider>());

            return builder;
        }
    }
}
