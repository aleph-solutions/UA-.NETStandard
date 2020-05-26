using Opc.Ua;

namespace PMIE.PubSubOpcUaServer.Configuration
{
    public class DataSetEventFieldConfiguration
    {
        /// <summary>
        /// The alias name of the field in the dataset
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The browsepath of the event field
        /// </summary>
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
