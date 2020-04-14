//using Opc.Ua.Client;
//using PubSubBase.Definitions;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace Opc.Ua.PubSub.Sample.ConfigurationClient
//{

//    #region Public Fields
//    /// <summary>
//    /// A client to configure the PubSub server
//    /// </summary>
//    class ConfigurationClient
//    {

//        /// <summary>
//        /// assigning session
//        /// </summary>
//        public Session Session
//        {
//            get;
//            set;
//        }
//        #endregion

//        /// <summary>
//        /// Method to add new UADP Connection
//        /// </summary>
//        /// <param name="connection">UADP Connection information</param>
//        /// <param name="connectionId">NodeId of current connection</param>
//        /// <returns>the error message</returns>
//        public string AddConnection(Connection connection, out NodeId connectionId)
//        {
//            string errorMessage = string.Empty;
//            connectionId = null;
//            PubSubConnectionDataType PubSubConnectionDataType = new PubSubConnectionDataType();

//            PubSubConnectionDataType.Name = connection.Name;
//            PubSubConnectionDataType.PublisherId = new Variant(connection.PublisherId);
//            PubSubConnectionDataType.TransportProfileUri = connection.TransportProfile;

//            NetworkAddressUrlDataType _NetworkAddressDataType = new NetworkAddressUrlDataType();
//            _NetworkAddressDataType.NetworkInterface = connection.NetworkInterface;
//            _NetworkAddressDataType.Url = connection.Address;

//            ExtensionObject _AddressExtensionObject = new ExtensionObject();
//            _AddressExtensionObject.Body = _NetworkAddressDataType;
//            PubSubConnectionDataType.Address = _AddressExtensionObject;

//            if (connection.ConnectionType == "0")
//            {
//                NetworkAddressUrlDataType DatagramNetworkAddressDataType = new NetworkAddressUrlDataType();
//                DatagramNetworkAddressDataType.NetworkInterface = connection.DiscoveryNetworkInterface;
//                DatagramNetworkAddressDataType.Url = connection.DiscoveryAddress;

//                ExtensionObject _AddressUrlExtensionObject = new ExtensionObject();
//                _AddressUrlExtensionObject.Body = DatagramNetworkAddressDataType;

//                DatagramConnectionTransportDataType _DatagramConnectionTransportDataType = new DatagramConnectionTransportDataType();
//                _DatagramConnectionTransportDataType.DiscoveryAddress = _AddressUrlExtensionObject;

//                ExtensionObject _ExtensionObject = new ExtensionObject();
//                _ExtensionObject.Body = _DatagramConnectionTransportDataType;
//                PubSubConnectionDataType.TransportSettings = _ExtensionObject;
//            }
//            else
//            {
//                BrokerConnectionTransportDataType _BrokerConnectionTransportDataType = new BrokerConnectionTransportDataType();
//                _BrokerConnectionTransportDataType.AuthenticationProfileUri = connection.AuthenticationProfileUri;
//                _BrokerConnectionTransportDataType.ResourceUri = connection.ResourceUri;

//                ExtensionObject _ExtensionObject = new ExtensionObject();
//                _ExtensionObject.Body = _BrokerConnectionTransportDataType;
//                PubSubConnectionDataType.TransportSettings = _ExtensionObject;
//            }
//            try
//            {
//                IList<object> lstResponse = Session.Call(Constants.PublishSubscribeObjectId,
//                    Constants.AddConnectionMethodId, new object[] { PubSubConnectionDataType });

//                connectionId = lstResponse[0] as NodeId;
//            }
//            catch (Exception e)
//            {
//                errorMessage = e.Message;
//            }
//            return errorMessage;
//        }
//    }
//}
