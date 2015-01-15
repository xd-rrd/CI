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
	public class NeedCodeReviewPolicyEnforcer : IPolicyEnforcer
	{
		public bool CheckPolicy(TeamFoundationRequestContext requestContext, CheckinNotification args, 
			PolicyConfigurationElement policyConfiguration, out string statusMessage)
		{
			statusMessage = string.Empty;
			var helper = new Helper(requestContext, args);
			bool isCodeReviewRequested = false;
			bool isCodeReviewAccepted = false;
			List<string> codeReviewOwners = new List<string>();
			var checkinItemInfos = args.NotificationInfo.WorkItemInfo;
			if (checkinItemInfos.Any())
			{
				foreach (var checkInItemInfo in checkinItemInfos)
				{
					WorkItem workIem = helper.GetWorkItem(checkInItemInfo.Id);
					//WorkItem workItem
					if (workIem.Type.Name.ToLower() == "code review request")
					{
						isCodeReviewRequested = true;

						var childWorkItems = helper.GetChildWorkItems(workIem);
						var codeReviewResponses =
							childWorkItems.Where(childWorkItem => childWorkItem.Type.Name.ToLower() == "code review response").ToList();
						if (codeReviewResponses.Count > 0)
						{
							foreach (var response in codeReviewResponses)
							{
								if (response.State.ToLower() == "closed")
								{
									isCodeReviewAccepted = true;
								}
								if (response.Fields.Contains("assigned to"))
								{
									string assignedTo = (string)response.Fields["assigned to"].Value;
									if (!string.IsNullOrEmpty(assignedTo) && !codeReviewOwners.Contains(assignedTo))
									{
										codeReviewOwners.Add(assignedTo);
									}
								}
							}
						}
						if (codeReviewResponses.Any(response => response.State.ToLower() == "closed"))
						{
							isCodeReviewAccepted = true;
						}
					}
				}
			}
			if (!isCodeReviewRequested)
			{
				statusMessage = Resources.NeedCodeReview;
				return false;
			}
			if (!isCodeReviewAccepted)
			{
				statusMessage = Resources.CodeReviewAccepted;
				return false;
			}
			var policy = policyConfiguration as NeedCodeReviewConfigurationElement;
			if (policy != null)
			{
				if (!policy.AllowSelfReview)
				{
					if (codeReviewOwners.Count > 0)
					{
						if (codeReviewOwners.Contains(args.ChangesetOwner.UniqueName) ||
						    codeReviewOwners.Contains(args.ChangesetOwner.DisplayName))
						{
							statusMessage = Resources.SelfReviewerNotAllowed;
							return false;
						}
					}
				}
				if (!string.IsNullOrEmpty(policy.AllowedReviewers))
				{
					var allowedReviewers = policy.AllowedReviewers.Split(new[] {';'}, StringSplitOptions.RemoveEmptyEntries).ToList();
					if (!allowedReviewers.Intersect(codeReviewOwners).Any())
					{
						statusMessage = Resources.CodeReviewerNotAllowed;
						return false;
					}
				}
			}
			return true;
		}
	}
}
