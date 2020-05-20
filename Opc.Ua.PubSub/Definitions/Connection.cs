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
using System.Collections.ObjectModel;

namespace Opc.Ua.PubSub.Definitions
{
    /// <summary>
    /// Defines the connection information 
    /// </summary>
    public class Connection : PubSubConfiguationBase
    {
        #region Private Fields
        private string m_address = String.Empty;
        private object m_publisherId;
        int m_publisherDataType = 0;
        private string m_connectionType;
        private string m_transportProfile;
        private NodeId m_connectionNodeId;
        private string m_networkInterface = string.Empty;
        private string m_resourceUri;
        private string m_authenticationProfileUri;

        private string m_discoveryNetworkInterface;
        private string m_discoveryAddress;
        #endregion

        #region Public Propertiess 
        /// <summary>
        /// Defines the coonnection address 
        /// </summary>
        public string Address
        {
            get
            {
                return m_address;
            }
            set
            {
                m_address = value;
            }
        }

        /// <summary>
        /// Defines the coonnection address 
        /// </summary>
        public string DiscoveryAddress
        {
            get
            {
                return m_discoveryAddress;
            }
            set
            {
                m_discoveryAddress = value;
            }
        }

        /// <summary>
        /// Defines the coonnection address 
        /// </summary>
        public string DiscoveryNetworkInterface
        {
            get
            {
                return m_discoveryNetworkInterface;
            }
            set
            {
                m_discoveryNetworkInterface = value;
            }
        }

        /// <summary>
        /// defines the connection publisher ID
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
        /// defines the publisher data type 
        /// </summary>
        public int PublisherDataType
        {
            get
            {
                return m_publisherDataType;
            }
            set
            {
                m_publisherDataType = value;
            }
        }
        /// <summary>
        /// defines the connection type of the current connection
        /// </summary>
        public string ConnectionType
        {
            get
            {
                return m_connectionType;
            }
            set
            {
                m_connectionType = value;
            }
        }

        /// <summary>
        /// defines the connection type of the current connection
        /// </summary>
        public string TransportProfile
        {
            get
            {
                return m_transportProfile;
            }
            set
            {
                m_transportProfile = value;
            }
        }

        /// <summary>
        /// defines the connection ID
        /// </summary>
        public NodeId ConnectionNodeId
        {
            get
            {
                return m_connectionNodeId;
            }
            set
            {
                m_connectionNodeId = value;
            }
        }

        public string  NetworkInterface
        {
            get
            {
                return m_networkInterface;
            }
            set
            {
                m_networkInterface = value;
            }
        }

        public string ResourceUri
        {
            get
            {
                return m_resourceUri;
            }
            set
            {
                m_resourceUri = value;
            }
        }

        public string AuthenticationProfileUri
        {
            get
            {
                return m_authenticationProfileUri;
            }
            set
            {
                m_authenticationProfileUri = value;
            }
        }
        #endregion
    }

    /// <summary>
    /// Base class of configuration
    /// </summary>
    public class PubSubConfiguationBase
    {
        #region Private Fields
        private string m_name;
        private ObservableCollection<PubSubConfiguationBase> m_children = new ObservableCollection<PubSubConfiguationBase>();

        #endregion

        #region Public Properties
        /// <summary>
        /// Defines the Name of the configuration
        /// </summary>
        public string Name
        {
            get
            {
                return m_name;
            }
            set
            {
                m_name = value;
            }
        }
        /// <summary>
        /// collection of childrens of the target configuration
        /// </summary>
        public ObservableCollection<PubSubConfiguationBase> Children
        {
            get { return m_children; }
            set
            {
                m_children = value;
            }
        }
        /// <summary>
        /// Parent node of the target node
        /// </summary>
        public PubSubConfiguationBase ParentNode { get; set; }
        #endregion
    }

}
