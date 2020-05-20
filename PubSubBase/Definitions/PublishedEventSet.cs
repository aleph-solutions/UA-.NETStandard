using System;
using Opc.Ua;

namespace PubSubBase.Definitions
{
    public class PublishedEventSet : PublishedDataSetBase
    {
        #region Private Fields
        QualifiedNameCollection _browsePath;
        #endregion

        public QualifiedNameCollection BrowsePath
        {
            get
            {
                return _browsePath;
            }
            set
            {
                _browsePath = value;
            }
        }

        public NodeId PublishedDataSetNodeId { get; set; }

        ConfigurationVersionDataType m_configurationVersionDataType = new ConfigurationVersionDataType();
        /// <summary>
        /// defines data type of the target definition
        /// </summary>
        public ConfigurationVersionDataType ConfigurationVersionDataType
        {
            get
            {
                return m_configurationVersionDataType;
            }
            set
            {
                m_configurationVersionDataType = value;
                OnPropertyChanged("ConfigurationVersionDataType");
            }
        }
        public PublishedEventSet()
        {
        }
    }
}
