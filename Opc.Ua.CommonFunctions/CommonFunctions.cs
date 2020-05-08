﻿using Opc.Ua;
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

        public static List<ReferenceDescription> Browse(Session session, NodeId nodeId, uint nodeClassMask)
        {
            var browseDescrColl = new BrowseDescriptionCollection()
            {
                new BrowseDescription()
                {
                    NodeId = nodeId,
                    BrowseDirection = BrowseDirection.Forward,
                    ReferenceTypeId = ReferenceTypeIds.Aggregates,
                    IncludeSubtypes = true,
                    NodeClassMask = nodeClassMask,
                    ResultMask = (uint)BrowseResultMask.All
                },
                 new BrowseDescription()
                {
                    NodeId = nodeId,
                    BrowseDirection = BrowseDirection.Forward,
                    ReferenceTypeId = ReferenceTypeIds.Organizes,
                    IncludeSubtypes = true,
                    NodeClassMask = nodeClassMask,
                    ResultMask = (uint)BrowseResultMask.All
                }
            };

            var browseResp = session.Browse(new RequestHeader(), null, 1000, browseDescrColl, out BrowseResultCollection results, out DiagnosticInfoCollection diagnosticInfos);
            var res = results[0].References;
            var orgRes = results[1].References.Where(x => !res.Exists(y => y.BrowseName.Name == x.BrowseName.Name));

            res.AddRange(orgRes);

            return res;
        }

        public static List<ReferenceDescription> Browse(Session session, ExpandedNodeId nodeId, uint nodeClassMask)
        {
            return Browse(session, ExpandedNodeId.ToNodeId(nodeId, session.NamespaceUris), nodeClassMask);
        }

        public static List<ReferenceDescription> Browse(Session session, ExpandedNodeId nodeId)
        {
            return Browse(session, nodeId, (uint)(NodeClass.Object | NodeClass.Variable | NodeClass.Method));
        }

        public static List<ReferenceDescription> Browse(Session session, NodeId nodeId)
        {
            return Browse(session, nodeId, (uint)(NodeClass.Object | NodeClass.Variable | NodeClass.Method));
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

        /// <summary>
        /// Search for the events generated by the object
        /// </summary>
        /// <param name="session">The session used to browse the server</param>
        /// <param name="nodeId">The nodeId of the object</param>
        /// <returns></returns>
        public static List<ReferenceDescription> GetGeneratedEvent(Session session, NodeId nodeId)
        {
            var typeNodeId = GetTypeDefinition(session, nodeId);

            var browseDescrColl = new BrowseDescriptionCollection()
            {
                new BrowseDescription()
                {
                    NodeId = typeNodeId,
                    BrowseDirection = BrowseDirection.Forward,
                    ReferenceTypeId = ReferenceTypeIds.GeneratesEvent,
                    IncludeSubtypes = true,
                    NodeClassMask = (uint)(NodeClass.ObjectType),
                    ResultMask = (uint)BrowseResultMask.All
                }
            };

            var browseResp = session.Browse(new RequestHeader(), null, 1000, browseDescrColl, out BrowseResultCollection results, out DiagnosticInfoCollection diagnosticInfos);
            return results[0].References;
        }

        /// <summary>
        /// Get the supertype of an object type
        /// </summary>
        /// <param name="session">The session to browse the server</param>
        /// <param name="typeNodeId">The nodeId id of the object type</param>
        /// <returns></returns>
        public static NodeId GetSuperTypeId(Session session, NodeId typeNodeId)
        {
            var browseDescrColl = new BrowseDescriptionCollection()
            {
                new BrowseDescription()
                {
                    NodeId = typeNodeId,
                    BrowseDirection = BrowseDirection.Inverse,
                    ReferenceTypeId = ReferenceTypeIds.HasSubtype,
                    IncludeSubtypes = false,
                    NodeClassMask = (uint)(NodeClass.ObjectType),
                    ResultMask = (uint)BrowseResultMask.All
                }
            };
            var browseResp = session.Browse(new RequestHeader(), null, 1000, browseDescrColl, out BrowseResultCollection results, out DiagnosticInfoCollection diagnosticInfos);
            return ExpandedNodeId.ToNodeId(results[0].References[0].NodeId, session.NamespaceUris);
        }

        #region Events
        /// <summary>
        /// Finds the type of the event for the notification.
        /// </summary>
        /// <param name="monitoredItem">The monitored item.</param>
        /// <param name="notification">The notification.</param>
        /// <returns>The NodeId of the EventType.</returns>
        public static NodeId FindEventType(MonitoredItem monitoredItem, EventFieldList notification)
        {
            EventFilter filter = monitoredItem.Status.Filter as EventFilter;

            if (filter != null)
            {
                for (int ii = 0; ii < filter.SelectClauses.Count; ii++)
                {
                    SimpleAttributeOperand clause = filter.SelectClauses[ii];

                    if (clause.BrowsePath.Count == 1 && clause.BrowsePath[0] == BrowseNames.EventType)
                    {
                        return notification.EventFields[ii].Value as NodeId;
                    }
                }
            }

            return null;
        }

        public static DateTime FindEventTime(MonitoredItem monitoredItem, EventFieldList notification)
        {
            EventFilter filter = monitoredItem.Status.Filter as EventFilter;

            if (filter != null)
            {
                for (int ii = 0; ii < filter.SelectClauses.Count; ii++)
                {
                    SimpleAttributeOperand clause = filter.SelectClauses[ii];

                    if (clause.BrowsePath.Count == 1 && clause.BrowsePath[0] == BrowseNames.Time)
                    {
                        return (DateTime)notification.EventFields[ii].Value;
                    }
                }
            }

            return new DateTime();
        }


        #endregion
    }
}
