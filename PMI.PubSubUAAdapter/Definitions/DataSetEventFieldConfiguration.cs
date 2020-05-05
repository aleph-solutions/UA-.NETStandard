using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Opc.Ua;

namespace PMI.PubSubUAAdapter.Configuration
{
    public class DataSetEventFieldConfiguration
    {
        public string Name { get; set; }

        public QualifiedNameCollection BrowsePath { get; set; }

        public DataSetEventFieldConfiguration() { }

        public DataSetEventFieldConfiguration(string browseName)
        {
            BrowsePath = new QualifiedNameCollection { browseName };
            Name = browseName;
        }

        public DataSetEventFieldConfiguration(QualifiedName browseName)
        {
            BrowsePath = new QualifiedNameCollection { browseName };
            Name = browseName.Name;
        }
    }
}
