using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Opc.Ua.Client;
using Opc.Ua;

namespace Opc.Ua.Extensions
{
    public class PubSubEventMonitoredItem : PubSubMonitoredItem
    {
        public NodeId EventType
        {
            get; set;
        }
    }
}
