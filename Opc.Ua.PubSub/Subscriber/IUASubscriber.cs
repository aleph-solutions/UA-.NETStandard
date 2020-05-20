namespace Opc.Ua.PubSub.Subscriber
{
    public interface IUASubscriber
    {
        void AddConnection(PubSubConnectionState pubSubConnectionState, IDataSource dataSource);

        void CreateTargetVariables(NodeId connectionStateNodeId, NodeId readerStateNodeId, FieldTargetDataType[] fieldTargetDataTypes);
        void RemoveFieldTargetDataType(NodeId readerStateNodeId);
        void RemoveConnection(PubSubConnectionState pubSubConnectionState);
        void AddDataSetReader(DataSetReaderState dataSetReaderState, Opc.Ua.Core.SubscriberDelegate subscriberDelegate);
        void RemoveDataSetReader(DataSetReaderState dataSetReaderState);
    }
}
