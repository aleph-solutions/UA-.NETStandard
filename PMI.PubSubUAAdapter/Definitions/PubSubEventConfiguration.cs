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
    public class PubSubEventConfiguration
    {
        [DataMember]
        public NodeId EventTypeId { get; set; }

        [DataMember]
        public string EventTypeName { get; set; }

        [DataMember]
        public IEnumerable<PubSubEventFieldConfiguration> Fields { get; set; }

        [DataMember]
        public IEnumerable<NodeId> IncludedNodes { get; set; }
        
         [DataMember]
        public IEnumerable<NodeId> ExcludedNodes { get; set; }

    }

    [DataContract]
    public class PubSubEventFieldConfiguration
    {
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
