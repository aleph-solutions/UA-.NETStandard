using Opc.Ua;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace PMI.PubSubUAAdapter.Configuration
{
    [DataContract]
    public class PubSubObjectDataConfiguration
    {
        [DataMember]
        public string Id { get; set; }

        [DataMember]
        public int PublishInterval { get; set; }

        [DataMember]
        public string ParentType { get; set; }

        [DataMember]
        public IEnumerable<PubSubDatFieldConfiguration> Fields { get; set; }

        [DataMember]
        public IEnumerable<NodeId> IncludedNodes { get; set; }

        [DataMember]
        public IEnumerable<NodeId> ExcludedNodes { get; set; }
    }

    [DataContract]
    public class PubSubDatFieldConfiguration
    {
        [DataMember]
        public string FieldName { get; set; }

        [DataMember]
        public uint Attribute
        {
            get
            {
                if (_attribute != null) return (uint)_attribute;
                return Attributes.Value;
            }
            set
            { _attribute = value; }
        }
        private uint? _attribute;

        [DataMember]
        public string BrowseName
        {
            get
            {
                if (_browseName != null) return _browseName;
                return FieldName;
            }
            set
            {
                _browseName = value;
            }
        }
        private string _browseName;

        [DataMember]
        public string ComplexVariableType { get; set; }

        [DataMember]
        public bool Enabled
        {
            get
            {
                if (_enabled != null) return (bool)_enabled;
                return true;
            }
            set
            {
                _enabled = value;
            }
        }
        private bool? _enabled;

        [DataMember]
        public int SamplingInterval
        {
            get
            {
                return _samplingInterval;
            }
            set
            {
                _samplingInterval = value;
            }
        }
        private int _samplingInterval = -1;
    }
}
