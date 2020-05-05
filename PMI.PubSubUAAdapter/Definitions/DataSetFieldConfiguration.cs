using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Opc.Ua;

namespace PMI.PubSubUAAdapter.Configuration
{
    public class DataSetFieldConfiguration
    {
        public DataSetFieldConfiguration()
        {

        }

        public DataSetFieldConfiguration(string name, NodeId fieldId, FieldFileDefinition jsonDefinition)
        {
            Name = name;
            SourceNodeId = fieldId;
            Attribute = jsonDefinition.Attribute;
            SamplingInterval = jsonDefinition.SamplingInterval;
        }

        public string Name { get; set; }

        public NodeId SourceNodeId { get; set; }
        public uint Attribute { get { return _attribute; } set { _attribute = value; } }
        private uint _attribute = Attributes.Value;

        public int SamplingInterval { get { return _samplingInterval; } set { _samplingInterval = value; } }
        private int _samplingInterval = -1;
    }
}
