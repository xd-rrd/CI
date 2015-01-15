using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.TeamFoundation.Framework.Server;
using Microsoft.TeamFoundation.VersionControl.Server;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using Rrd.Tfs.Plugins.Config;
using Rrd.Tfs.Plugins.Properties;

namespace Rrd.Tfs.Plugins.Policies
{
	public class WorkItemAssociationPolicyEnforcer : IPolicyEnforcer
	{
		public bool CheckPolicy(TeamFoundationRequestContext requestContext, CheckinNotification args,
			PolicyConfigurationElement policyConfiguration, out string statusMessage)
		{
			statusMessage = string.Empty;
			var helper = new Helper(requestContext, args);
			var checkinItemInfos = args.NotificationInfo.WorkItemInfo;
			List<string> associatedWorkItemTypes = new List<string>();
			int associatedWorkItemCount = 0;
			if (checkinItemInfos.Any())
			{
				foreach (var checkInItemInfo in checkinItemInfos)
				{
					associatedWorkItemCount ++;
					WorkItem workItem = helper.GetWorkItem(checkInItemInfo.Id);
					string workItemType = workItem.Type.Name;
					if (!associatedWorkItemTypes.Contains(workItemType))
					{
						associatedWorkItemTypes.Add(workItemType);
					}
				}
			}
			if (associatedWorkItemCount > 0)
			{
				var policy = policyConfiguration as AssociateWithWorkItemConfigurationElement;
				if (policy != null)
				{
					if (!string.IsNullOrEmpty(policy.AllowedWorkItemTypes))
					{
						var allowedWorkItemTypes =
							policy.AllowedWorkItemTypes.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries).ToList();
						if (!allowedWorkItemTypes.Intersect(associatedWorkItemTypes).Any())
						{
							statusMessage = string.Format(Resources.AssociatedWorkItemTypeIncorrect, policy.AllowedWorkItemTypes);
							return false;
						}
					}
				}
				return true;
			}
			else
			{
				statusMessage = Resources.NeedToAssociateWithWorkItem;
				return false;
			}
		}
	}
}
