using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.CommonFunctions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMI.PubSubUAAdapter.ConfigurationClient
{
    public class ConfiguratonBuilder
    {
        Session _browseSession;

        public ConfiguratonBuilder(ApplicationConfiguration configuration)
        {
            var selectedEndpoint = CoreClientUtils.SelectEndpoint("opc.tcp://localhost:48030", false);
            var endpointConfiguration = EndpointConfiguration.Create(configuration);
            var endpoint = new ConfiguredEndpoint(null, selectedEndpoint, endpointConfiguration);

            _browseSession = Session.Create(configuration, endpoint, false, "PubSub Configurator Builder", 60000, new UserIdentity(new AnonymousIdentityToken()), null).Result;
        }

        public void ConfigureMachineModule(NodeId machineModuleId, string machineName)
        {
            //Search the NodeIds of the MachineModule sub object
            var configurationId = GetChildId(machineModuleId, "Configuration");
            var livestatusId = GetChildId(machineModuleId, "LiveStatus");
            var productionId = GetChildId(machineModuleId, "Production");
            var setupId = GetChildId(machineModuleId, "SetUp");
            var specificationId = GetChildId(machineModuleId, "Specification");
            var defectSensorsFolderId = GetChildId(machineModuleId, "DefectDetectionSensors");
            var materialBuffersFolderId = GetChildId(machineModuleId, "MaterialBuffers");
            var materialLoadingPointsFolderId = GetChildId(machineModuleId, "MaterialLoadingPoints");
            var materialOutputsFolderId = GetChildId(machineModuleId, "MaterialOutputs");
            var rejectionTrapsFolderId = GetChildId(machineModuleId, "MaterialRejectionTraps");
            var processControlLoopsFolderId = GetChildId(machineModuleId, "ProcessControlLoops");
            var processItemsFolderId = GetChildId(machineModuleId, "ProcessItems");
        
        }

        public void ConfigureDefectSensors(NodeId folderId, string machineName)
        {
            var references = Browse(folderId);

            foreach(var objItem in references.Where(x => x.NodeClass == NodeClass.Object))
            {
                var typeId = GetTypeDefinition(objItem.NodeId);
                if(typeId.Equals(ExpandedNodeId.ToNodeId(TMCPlus.ObjectTypeIds.PMI_DefectDetectionSensorType, _browseSession.NamespaceUris)))
                {
                    //Do things
                }
            }
        }

        private NodeId GetChildId(NodeId startNodeId, string childName)
        {
            return CommonFunctions.GetChildId(_browseSession, startNodeId, childName);
        }

        private List<ReferenceDescription> Browse(NodeId startNodeId)
        {
            return CommonFunctions.Browse(_browseSession, startNodeId);
        }

        private NodeId GetTypeDefinition(ExpandedNodeId nodeId)
        {
            return CommonFunctions.GetTypeDefinition(_browseSession, ExpandedNodeId.ToNodeId(nodeId, _browseSession.NamespaceUris));
        }
    }
}
