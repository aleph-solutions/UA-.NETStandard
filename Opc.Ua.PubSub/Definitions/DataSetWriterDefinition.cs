/* Copyright (c) 1996-2017, OPC Foundation. All rights reserved.

   The source code in this file is covered under a dual-license scenario:
     - RCL: for OPC Foundation members in good-standing
     - GPL V2: everybody else

   RCL license terms accompanied with this source code. See http://opcfoundation.org/License/RCL/1.00/

   GNU General Public License as published by the Free Software Foundation;
   version 2 of the License are accompanied with this source code. See http://opcfoundation.org/License/GPLv2

   This source code is distributed in the hope that it will be useful,
   but WITHOUT ANY WARRANTY; without even the implied warranty of
   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
*/

using Opc.Ua;

namespace Opc.Ua.PubSub.Definitions
{
    /// <summary>
    /// Defines DataSet Writer for publisher 
    /// </summary>
    public class DataSetWriterDefinition : PubSubConfiguationBase
    {
        #region Private Fields
        private string m_dataSetWriterName;
        private NodeId m_publisherDataSetNodeId = new NodeId("0", 1);
        private string m_publisherDataSetId;
        private uint m_keyFrameCount;

        private ushort m_dataSetWriterId;
        private string m_queueName;
        private string m_metadataQueueName;
        private int m_metadataUpdateTime;
        private int m_maxMessageSize;
        private NodeId m_writerNodeId;
        private int m_revisedKeyFrameCount;
        private int m_revisedMaxMessageSize;
        private int m_dataSetContentMask;
        private int m_uadpdataSetMessageContentMask;
        private int m_jsondataSetMessageContentMask;
        private string m_resourceUri;
        private string m_authenticationProfileUri;
        private int m_requestedDeliveryGuarantee;
        private ushort m_configuredSize;
        private int m_transportSetting = 0;
        private int m_messageSetting;
        private string m_datasetName;
        private ushort m_dataSetOffset;
        private ushort m_networkMessageNumber;
        #endregion

        #region Public Properties

        public string DataSetName
        {
            get
            {
                return m_datasetName;
            }
            set
            {
                m_datasetName = value;
            }
        }

        public ushort ConfiguredSize
        {
            get { return m_configuredSize; }
            set { m_configuredSize = value; }
        }

        public ushort DataSetOffset
        {
            get { return m_dataSetOffset; }
            set { m_dataSetOffset = value; }
        }

        public ushort NetworkMessageNumber
        {
            get { return m_networkMessageNumber; }
            set { m_networkMessageNumber = value; }
        }

        public string ResourceUri
        {
            get { return m_resourceUri; }
            set { m_resourceUri = value; }
        }

        public string AuthenticationProfileUri
        {
            get { return m_authenticationProfileUri; }
            set { m_authenticationProfileUri = value; }
        }

        public int RequestedDeliveryGuarantee
        {
            get { return m_requestedDeliveryGuarantee; }
            set { m_requestedDeliveryGuarantee = value; }
        }

        /// <summary>
        /// defines name of DataSetWriter Name
        /// </summary>
        public string DataSetWriterName
        {
            get
            {
                return m_dataSetWriterName;
            }
            set
            {
                Name = m_dataSetWriterName = value;
            }
        }

        /// <summary>
        /// Defines Pulisher DataSet Node ID
        /// </summary>
        public NodeId PublisherDataSetNodeId
        {
            get
            {
                return m_publisherDataSetNodeId;
            }
            set
            {
                m_publisherDataSetNodeId = value;
            }
        }

        /// <summary>
        /// Defines Publisher DataSet ID
        /// </summary>
        public string PublisherDataSetId
        {
            get
            {
                return m_publisherDataSetId;
            }
            set
            {
                m_publisherDataSetId = value;
            }
        }

        /// <summary>
        /// Defines the KeyFrame Count of DataSet Writer
        /// </summary>
        public uint KeyFrameCount
        {
            get
            {
                return m_keyFrameCount;
            }
            set
            {
                m_keyFrameCount = value;
            }
        }



        /// <summary>
        /// Defines the DataSet Writer ID
        /// </summary>
        public ushort DataSetWriterId
        {
            get
            {
                return m_dataSetWriterId;
            }
            set
            {
                m_dataSetWriterId = value;
            }
        }

        /// <summary>
        /// Defines the  Data Writer Queue Name
        /// </summary>
        public string QueueName
        {
            get
            {
                return m_queueName;
            }
            set
            {
                m_queueName = value;
            }
        }
        /// <summary>
        /// Defines the MetaDataQueue Name
        /// </summary>
        public string MetadataQueueName
        {
            get
            {
                return m_metadataQueueName;
            }
            set
            {
                m_metadataQueueName = value;
            }
        }

        /// <summary>
        /// Defines the MetaData update time 
        /// </summary>
        public int MetadataUpdataTime
        {
            get
            {
                return m_metadataUpdateTime;
            }
            set
            {
                m_metadataUpdateTime = value;
            }
        }

        /// <summary>
        /// defines the max size of the messgae queue
        /// </summary>
        public int MaxMessageSize
        {
            get
            {
                return m_maxMessageSize;
            }
            set
            {
                m_maxMessageSize = value;
            }
        }

        /// <summary>
        /// Defines the Writer Node ID
        /// </summary>
        public NodeId WriterNodeId
        {
            get
            {
                return m_writerNodeId;

            }
            set
            {
                m_writerNodeId = value;
            }
        }

        /// <summary>
        /// Defines the revised key frame count
        /// </summary>
        public int RevisedKeyFrameCount
        {
            get
            {
                return m_revisedKeyFrameCount;

            }
            set
            {
                m_revisedKeyFrameCount = value;
            }
        }
        /// <summary>
        /// Defines Revised maximum message size code
        /// </summary>
        public int RevisedMaxMessageSize
        {
            get
            {
                return m_revisedMaxMessageSize;

            }
            set
            {
                m_revisedMaxMessageSize = value;
            }
        }
        /// <summary>
        /// defines the DataSet Content Mask
        /// </summary>
        public int DataSetContentMask
        {
            get { return m_dataSetContentMask; }
            set
            {
                m_dataSetContentMask = value;
            }
        }

        /// <summary>
        /// defines the DataSet Message Content Mask
        /// </summary>
        public int UadpDataSetMessageContentMask
        {
            get { return m_uadpdataSetMessageContentMask; }
            set
            {
                m_uadpdataSetMessageContentMask = value;
            }
        }

        /// <summary>
        /// defines the DataSet Message Content Mask
        /// </summary>
        public int JsonDataSetMessageContentMask
        {
            get { return m_jsondataSetMessageContentMask; }
            set
            {
                m_jsondataSetMessageContentMask = value;
            }
        }

        /// <summary>
        /// defines visibility for context menu
        /// </summary>
        public int TransportSetting
        {
            get { return m_transportSetting; }
            set
            {
                m_transportSetting = value;
            }
        }

        /// <summary>
        /// defines visibility for context menu
        /// </summary>
        public int MessageSetting
        {
            get { return m_messageSetting; }
            set
            {
                m_messageSetting = value;
            }
        }

        #endregion

    }
}
