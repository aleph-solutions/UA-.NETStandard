using Opc.Ua.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Opc.Ua.Extensions
{
    public class PubSubMonitoredItem : MonitoredItem
    {
        public DateTime LastPublishTimestamp
        {
            get; set;
        }
    }
}
