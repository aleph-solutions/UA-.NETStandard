using Opc.Ua;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace PMIE.PubSubOpcUaServer.Configuration
{
    [DataContract]
    public class PubSubEventConfiguration
    {
        /// <summary>
        /// The nodeId of the event type
        /// </summary>
        [DataMember]
        public NodeId EventTypeId { get; set; }

        /// <summary>
        /// The name of the eventype
        /// </summary>
        [DataMember]
        public string EventTypeName { get; set; }

        /// <summary>
        /// The list of fields
        /// </summary>
        [DataMember]
        public IEnumerable<PubSubEventFieldConfiguration> Fields { get; set; }

        /// <summary>
        /// List of NodeIDs of the objects that generates this event that shall be included in the pub sub configuration
        /// </summary>
        [DataMember]
        public IEnumerable<NodeId> IncludedNodes { get; set; }

        /// <summary>
        /// List of NodeIDs of the objects that generates this event that shall not be included in the pub sub configuration
        /// </summary>
        [DataMember]
        public IEnumerable<NodeId> ExcludedNodes { get; set; }

    }

    [DataContract]
    public class PubSubEventFieldConfiguration
    {
        /// <summary>
        /// The alias name of the event field
        /// </summary>
        [DataMember]
        public string AliasName 
        { 
            get 
            {
                if (_aliasName != null) return _aliasName;
                else if(_browsePath != null)
                {
                    var ret = String.Empty;
                    foreach(var pathItem in _browsePath)
                    {
                        ret += $"{pathItem.Name}/";
                    }
                    return ret.Remove(ret.Length - 1);
                }
                return String.Empty;
            }
            set
            {
                _aliasName = value;
            }
        }
        private string _aliasName;

        /// <summary>
        /// The browsepath of the event field as string
        /// </summary>
        [DataMember]
        public string BrowsePathString { 
            get 
            { 
                if (_browsePathString != null) return _browsePathString;
                else if(_browsePath != null)
                {
                    var ret = "";
                    foreach(var pathItem in _browsePath)
                    {
                        ret += $"{pathItem.ToString()}/";
                    }
                    return ret.Remove(ret.Length - 1);
                }
                return String.Empty;
            }
            set
            {
                _browsePathString = value;
            }
        }
        private string _browsePathString;

        /// <summary>
        /// The browsepath of the event field
        /// </summary>
        public QualifiedNameCollection BrowsePath
        {
            get
            {
                if (_browsePath != null) return _browsePath;
                var ret = new QualifiedNameCollection();
                var splittedPath = BrowsePathString.Split('/');
                foreach(var pathItem in splittedPath)
                {
                    var splittedItem = pathItem.Split(':');
                    if(splittedItem.Length == 1)
                    {
                        ret.Add(splittedItem[0]);
                    }
                    else if(splittedItem.Length == 2)
                    {
                        ret.Add(new QualifiedName(splittedItem[1], Convert.ToUInt16(splittedItem[0])));
                    }
                }
                return ret;

            }
            set
            {
                _browsePath = value;
            }
        }
        private QualifiedNameCollection _browsePath;

        /// <summary>
        /// Define if the field shall be included in the dataset
        /// </summary>
        [DataMember]
        public bool Enabled
        {
            get
            {
                return _enabled;
            }
            set
            {
                _enabled = value;
            }
        }

        private bool _enabled = true;
    }
}
