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

        /// <summary>
        /// The name of the parent type
        /// </summary>
        [DataMember]
        public string ParentType { get; set; }

        /// <summary>
        /// The fields list of the dataset
        /// </summary>
        [DataMember]
        public IEnumerable<PubSubDataFieldConfiguration> Fields { get; set; }

        /// <summary>
        /// The nodes of this type that shall be insluded in the pubsub configuration
        /// </summary>
        [DataMember]
        public IEnumerable<NodeId> IncludedNodes { get; set; }

        /// <summary>
        /// The nodes of this type that shall not be insluded in the pubsub configuration
        /// </summary>
        [DataMember]
        public IEnumerable<NodeId> ExcludedNodes { get; set; }
    }

    [DataContract]
    public class PubSubDataFieldConfiguration
    {
        /// <summary>
        /// The alias name of the field
        /// </summary>
        [DataMember]
        public string FieldName { get; set; }

        /// <summary>
        /// The attribute of the node to monitor
        /// </summary>
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

        /// <summary>
        /// The browseName of the node 
        /// </summary>
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

        /// <summary>
        /// If not null, is the name of the complex datatype that this configuration represents
        /// </summary>
        [DataMember]
        public string ComplexVariableType { get; set; }

        /// <summary>
        /// Define if the field shsall be included in the configuration of the dataset
        /// </summary>
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

        /// <summary>
        /// The sampling interval for this field
        /// </summary>
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

        [DataMember]
        public bool Optional
        {
            get
            {
                return _optional;
            }
            set
            {
                _optional = value;
            }
        }

        private bool _optional = false;
    }
}
