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
    /// Defines Data Set writer group information.
    /// </summary>
    public class DataSetWriterGroup : PubSubConfiguationBase
    {
        #region Private Fields

        private string m_groupName;
        private string m_queueName = string.Empty;
        private string m_encodingMimeType = string.Empty;
        private double m_publishingInterval;
        private int m_publishingOffset;
        private double m_keepAliveTime;
        private byte m_priority;
        private string m_securityGroupId;
        private uint m_maxNetworkMessageSize = 1500;
        private int m_writerGroupId;
        private NodeId m_groupId;
        private int m_messageSecurityMode = 0;
        private byte m_messageRepeatCount;
        private double m_messsageRepeatDelay;
        private string m_resourceUri;
        private string m_authenticationProfileUri;
        private int m_requestedDeliveryGuarantee = 0;
        private int m_transportSetting;
        private int m_samplingOffset;
        private int m_messgaeSetting;
        private int m_dataSetOrdering = 0;
        private uint m_groupVersion;
        private int m_networkMessageContentMask;
        private int m_jsonNetworkMessageContentMask;
        private string m_HeaderLayoutUri;
        #endregion

        #region Public Properties
        public string HeaderLayoutUri
        {
            get
            {
                return m_HeaderLayoutUri;
            }
            set
            {
                m_HeaderLayoutUri = value;
            }
        }
        /// <summary>
        /// defines network message content mask
        /// </summary>
        public int UadpNetworkMessageContentMask
        {
            get { return m_networkMessageContentMask; }
            set
            {
                m_networkMessageContentMask = value;
            }
        }

        /// <summary>
        /// defines network message content mask
        /// </summary>
        public int JsonNetworkMessageContentMask
        {
            get { return m_jsonNetworkMessageContentMask; }
            set
            {
                m_jsonNetworkMessageContentMask = value;
            }
        }

        public uint GroupVersion
        {
            get
            {
                return m_groupVersion;
            }
            set
            {
                m_groupVersion = value;
            }
        }

        public int DataSetOrdering
        {
            get { return m_dataSetOrdering; }
            set { m_dataSetOrdering = value; }
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

        public byte MessageRepeatCount
        {
            get { return m_messageRepeatCount; }
            set { m_messageRepeatCount = value; }
        }

        public double MessageRepeatDelay
        {
            get { return m_messsageRepeatDelay; }
            set { m_messsageRepeatDelay = value; }
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
        /// defines Group Name
        /// </summary>
        public string GroupName
        {
            get
            {
                return m_groupName;
            }
            set
            {
                Name = m_groupName = value;
            }
        }
        /// <summary>
        /// Defines Queue Name
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
        /// defines Encoding Mime Type
        /// </summary>
        public string EncodingMimeType
        {
            get
            {
                return m_encodingMimeType;
            }
            set
            {
                m_encodingMimeType = value;
            }
        }
        /// <summary>
        /// Defines publishing interval 
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
        /// defines publishing offset
        /// </summary>
        public int PublishingOffset
        {
            get
            {
                return m_publishingOffset;
            }
            set
            {
                m_publishingOffset = value;
            }
        }

        public int SamplingOffset
        {
            get { return m_samplingOffset; }
            set
            {
                m_samplingOffset = value;
            }
        }

        /// <summary>
        /// defines keepAliveTime
        /// </summary>
        public double KeepAliveTime
        {
            get
            {
                return m_keepAliveTime;
            }
            set
            {
                m_keepAliveTime = value;
            }
        }
        /// <summary>
        /// defines priority of the target group
        /// </summary>
        public byte Priority
        {
            get
            {
                return m_priority;
            }
            set
            {
                m_priority = value;
            }
        }
        /// <summary>
        /// defines security group ID for target group
        /// </summary>
        public string SecurityGroupId
        {
            get
            {
                return m_securityGroupId;
            }
            set
            {
                m_securityGroupId = value;
            }
        }
        /// <summary>
        /// defines maximum network message size node 
        /// </summary>
        public uint MaxNetworkMessageSize
        {
            get
            {
                return m_maxNetworkMessageSize;
            }
            set
            {
                m_maxNetworkMessageSize = value;
            }
        }
        /// <summary>
        /// defines writer group ID
        /// </summary>
        public int WriterGroupId
        {
            get
            {
                return m_writerGroupId;
            }
            set
            {
                m_writerGroupId = value;
            }
        }
        /// <summary>
        /// defines group ID
        /// </summary>
        public NodeId GroupId
        {
            get
            {
                return m_groupId;

            }
            set
            {
                m_groupId = value;
            }
        }
        /// <summary>
        /// Defines Message Security Mode
        /// </summary>
        public int MessageSecurityMode
        {
            get { return m_messageSecurityMode; }
            set
            {
                m_messageSecurityMode = value;
            }
        }

        #endregion
    }
}
