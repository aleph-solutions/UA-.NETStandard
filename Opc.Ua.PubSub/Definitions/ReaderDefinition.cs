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
using System;
using System.Windows;

namespace Opc.Ua.PubSub.Definitions
{
    /// <summary>
    /// definition of Data set reader
    /// </summary>
    public class DataSetReaderDefinition : PubSubConfiguationBase
    {
        #region Private Fields

        private string m_dataSetReaderName;
        private object m_publisherId;
        private ushort m_dataSetWriterId;
        private DataSetMetaDataType m_dataSetMetaDataType = new DataSetMetaDataType();
        private double m_messageReceiveTimeOut;
        private int m_dataSetContentMask;
        private int m_networkMessageContentMask;
        private double m_publishingInterval;
        private NodeId m_dataSetReaderNodeId;

        private string m_resourceUri;
        private string m_authenticationProfileUri;
        private int m_requestedDeliveryGuarantee;
        private int m_transportSetting;
        private int m_messgaeSetting;
        private double m_groupVersion;
        private string m_queueName = string.Empty;
        private string m_securityGroupId = "0";
        private int m_writerGroupId = 0;
        private int m_messageSecurityMode = 1;
        private double m_processingOffset = 0;
        private double m_receiveOffset = 0;
        private uint m_NetworkMessageNumber = 0;
        private uint m_dataSetOffset;
        private string m_metadataQueueName;
        private uint m_KeyFrameCount;
        private string m_HeaderLayoutUri;
        private int m_UadpNetworkMessageContentMask;
        private int m_UadpDataSetMessageContentMask;
        private int m_JsonNetworkMessageContentMask;
        private int m_JsonDataSetMessageContentMask;

        #endregion

        #region Public Properties
        public uint KeyFrameCount
        {
            get { return m_KeyFrameCount; }
            set { m_KeyFrameCount = value; }
        }
        public string HeaderLayoutUri
        {
            get { return m_HeaderLayoutUri; }
            set { m_HeaderLayoutUri = value; }
        }
        public uint NetworkMessageNumber
        {
            get { return m_NetworkMessageNumber; }
            set { m_NetworkMessageNumber = value; }
        }

        public double Receiveoffset
        {
            get { return m_receiveOffset; }
            set { m_receiveOffset = value; }
        }

        public double ProcessingOffset
        {
            get { return m_processingOffset; }
            set { m_processingOffset = value; }
        }

        public int UadpNetworkMessageContentMask
        {
            get { return m_UadpNetworkMessageContentMask; }
            set { m_UadpNetworkMessageContentMask = value; }
        }

        public int UadpDataSetMessageContentMask
        {
            get { return m_UadpDataSetMessageContentMask; }
            set { m_UadpDataSetMessageContentMask = value; }
        }

        public int JsonNetworkMessageContentMask
        {
            get { return m_JsonNetworkMessageContentMask; }
            set { m_JsonNetworkMessageContentMask = value; }
        }

        public int JsonDataSetMessageContentMask
        {
            get { return m_JsonDataSetMessageContentMask; }
            set { m_JsonDataSetMessageContentMask = value; }
        }

        public uint DataSetOffset
        {
            get { return m_dataSetOffset; }
            set { m_dataSetOffset = value; }
        }

        public double GroupVersion
        {
            get { return m_groupVersion; }
            set { m_groupVersion = value; }
        }
        /// <summary>
        /// defines data set reader name 
        /// </summary>
        public string DataSetReaderName
        {
            get
            {
                return m_dataSetReaderName;
            }
            set
            {
                Name = m_dataSetReaderName = value;
            }
        }

        public string MetadataQueueName
        {
            get { return m_metadataQueueName; }
            set { m_metadataQueueName = value; }
        }

        /// <summary>
        /// defines pulisher ID
        /// </summary>
        public object PublisherId
        {
            get
            {
                return m_publisherId;
            }
            set
            {
                m_publisherId = value;
            }
        }

        /// <summary>
        /// defines data set writer id
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
        /// defines data set metadata type
        /// </summary>
        public DataSetMetaDataType DataSetMetaDataType
        {
            get
            {
                return m_dataSetMetaDataType;
            }
            set
            {
                m_dataSetMetaDataType = value;
            }
        }

        /// <summary>
        /// defines message received timeout of Data set reader
        /// </summary>
        public double MessageReceiveTimeOut
        {
            get
            {
                return m_messageReceiveTimeOut;
            }
            set
            {
                m_messageReceiveTimeOut = value;
            }
        }

        /// <summary>
        /// defines data set content mask
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
        /// defines network message content mask
        /// </summary>
        public int NetworkMessageContentMask
        {
            get { return m_networkMessageContentMask; }
            set
            {
                m_networkMessageContentMask = value;
            }
        }

        /// <summary>
        /// defines publishing interval for data set reader
        /// </summary>
        public double PublishingInterval
        {
            get
            {
                return m_publishingInterval;
            }
            set
            {
                m_publishingInterval = value;
            }
        }

        /// <summary>
        /// defines data set reader node ID
        /// </summary>
        public NodeId DataSetReaderNodeId
        {
            get
            {
                return m_dataSetReaderNodeId;
            }
            set
            {
                m_dataSetReaderNodeId = value;
            }
        }

        public int TransportSetting
        {
            get { return m_transportSetting; }
            set { m_transportSetting = value; }
        }

        public int MessageSetting
        {
            get { return m_messgaeSetting; }
            set { m_messgaeSetting = value; }
        }

        /// <summary>
        /// defines security group ID
        /// </summary>
        public string SecurityGroupId
        {
            get { return m_securityGroupId; }
            set
            {
                m_securityGroupId = value;
            }
        }
        /// <summary>
        /// defines quee name 
        /// </summary>
        public string QueueName
        {
            get { return m_queueName; }
            set
            {
                m_queueName = value;
            }
        }
        /// <summary>
        /// defines writer group ID
        /// </summary>
        public int WriterGroupId
        {
            get { return m_writerGroupId; }
            set
            {
                m_writerGroupId = value;
            }
        }

        /// <summary>
        /// defines message security mode
        /// </summary>
        public int MessageSecurityMode
        {
            get { return m_messageSecurityMode; }
            set
            {
                m_messageSecurityMode = value;
            }
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


        #endregion
    }
}
