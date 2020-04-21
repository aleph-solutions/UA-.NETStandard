using Opc.Ua;
using Opc.Ua.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Opc.Ua.CommonFunctions
{
    public static class CommonFunctions
    {
        public static NodeId GetChildId(Session session, NodeId parentNode, string childrenName)
        {
            var browseDescrColl = new BrowseDescriptionCollection()
            {
                new BrowseDescription()
                {
                    NodeId = parentNode,
                    BrowseDirection = BrowseDirection.Forward,
                    ReferenceTypeId = ReferenceTypeIds.Aggregates,
                    IncludeSubtypes = true,
                    NodeClassMask = (uint)(NodeClass.Object | NodeClass.Variable | NodeClass.Method),
                    ResultMask = (uint)BrowseResultMask.All
                }
            };


            var browseResp = session.Browse(new RequestHeader(), null, 1000, browseDescrColl, out BrowseResultCollection results, out DiagnosticInfoCollection diagnosticInfos);
            
            if(results != null && results.First().References.Count > 0)
            {
                var child = results[0].References.FirstOrDefault(x => x.BrowseName.Name == childrenName);
                if(child != null) return ExpandedNodeId.ToNodeId(child.NodeId, session.NamespaceUris);
            }

            return null;
        }

        public static List<ReferenceDescription> Browse(Session session, NodeId nodeId)
        {
            var browseDescrColl = new BrowseDescriptionCollection()
            {
                new BrowseDescription()
                {
                    NodeId = nodeId,
                    BrowseDirection = BrowseDirection.Forward,
                    ReferenceTypeId = ReferenceTypeIds.Aggregates,
                    IncludeSubtypes = true,
                    NodeClassMask = (uint)(NodeClass.Object | NodeClass.Variable | NodeClass.Method),
                    ResultMask = (uint)BrowseResultMask.All
                }
            };

            var browseResp = session.Browse(new RequestHeader(), null, 1000, browseDescrColl, out BrowseResultCollection results, out DiagnosticInfoCollection diagnosticInfos);
            return results[0].References;
        }

        public static NodeId GetTypeDefinition(Session session, NodeId nodeId) 
        {
            var browseDescrColl = new BrowseDescriptionCollection()
            {
                new BrowseDescription()
                {
                    NodeId = nodeId,
                    BrowseDirection = BrowseDirection.Forward,
                    ReferenceTypeId = ReferenceTypeIds.HasTypeDefinition,
                    IncludeSubtypes = true,
                    NodeClassMask = (uint)(NodeClass.ObjectType | NodeClass.VariableType),
                    ResultMask = (uint)BrowseResultMask.All
                }
            };

            var browseResp = session.Browse(new RequestHeader(), null, 1000, browseDescrColl, out BrowseResultCollection results, out DiagnosticInfoCollection diagnosticInfos);
            return ExpandedNodeId.ToNodeId(results[0].References.First()?.NodeId, session.NamespaceUris);
        }
    }
}
