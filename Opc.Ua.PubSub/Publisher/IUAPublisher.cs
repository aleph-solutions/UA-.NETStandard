using Opc.Ua.Client;
using System.Collections.Generic;

namespace Opc.Ua.PubSub.Publisher
{
    public interface IUAPublisher
    {
        void AddConnection(PubSubConnectionState pubSubConnectionState, IDataSource dataSource);
        void AddWriterGroup(WriterGroupState writerGroupState,ref List<MonitoredItem> lstMonitoredItems);
        void RemoveGroup(BaseInstanceState groupState);
        void RemoveConnection(PubSubConnectionState pubSubConnectionState);
        void AddDataSetWriter(DataSetWriterState dataSetWriterState);

        void RemoveDataSetWriter(DataSetWriterState dataSetWriterState);
    }
}
