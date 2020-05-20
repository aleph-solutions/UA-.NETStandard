using System.Collections.Generic;
using Opc.Ua.Client;

namespace Opc.Ua.PubSub.Publisher
{
    public interface IUAPublisherDataSource
    {
        void AddWriterGroup(WriterGroupState writerGroupState, ref List<MonitoredItem> lstMonitoredItems);
        void RemoveGroup(BaseInstanceState GroupState);
        bool Initialize(PubSubConnectionState pubSubConnectionState, IDataSource dataSource);
        void StopPublishing();
        void AddDataSetWriter(DataSetWriterState dataSetWriterState);
        void RemoveDataSetWriter(DataSetWriterState dataSetWriterState);

    }

}

