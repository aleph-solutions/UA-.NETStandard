using Opc.Ua;

namespace PMIE.PubSubOpcUaServer.Configuration
{
    public class DataSetFieldConfiguration
    {
        public DataSetFieldConfiguration()
        {

        }

        public DataSetFieldConfiguration(string name, NodeId fieldId, PubSubDataFieldConfiguration jsonDefinition)
        {
            Name = name;
            SourceNodeId = fieldId;
            Attribute = jsonDefinition.Attribute;
            SamplingInterval = jsonDefinition.SamplingInterval;
        }

        /// <summary>
        /// The alias name of the field in the dataset
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The nodeId of the data source for this field
        /// </summary>
        public NodeId SourceNodeId { get; set; }

        /// <summary>
        /// The monitored attribute of the node [Default = Value]
        /// </summary>
        public uint Attribute { get { return _attribute; } set { _attribute = value; } }
        private uint _attribute = Attributes.Value;

        /// <summary>
        /// The sampling interval for the monitored item
        /// </summary>
        public int SamplingInterval { get { return _samplingInterval; } set { _samplingInterval = value; } }
        private int _samplingInterval = -1;
    }
}
