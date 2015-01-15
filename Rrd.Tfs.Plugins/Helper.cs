using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Framework.Common;
using Microsoft.TeamFoundation.Framework.Server;
using Microsoft.TeamFoundation.Server;
using Microsoft.TeamFoundation.Server.Core;
using Microsoft.TeamFoundation.VersionControl.Server;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using Microsoft.VisualStudio.Services.Identity;

namespace Rrd.Tfs.Plugins
{
	public class Helper
	{
		private readonly TeamFoundationRequestContext _requestContext;
		private readonly CheckinNotification _notification;
		public TfsTeamProjectCollection tfsTeamProjectCollection;

		public TfsTeamProjectCollection ProjectCollection
		{
			get { return tfsTeamProjectCollection ?? (tfsTeamProjectCollection = GetTeamProjectCollection()); }
		}

		public Helper(TeamFoundationRequestContext requestContext, CheckinNotification notification)
		{
			this._requestContext = requestContext;
			this._notification = notification;
		}

		#region WorkItemStore

		public NodeInfo GetIterationInfo(WorkItem workItem)
		{
			//workItem.Project.Name
			Node iterationNode = workItem.Project.FindNodeInSubTree(workItem.IterationId);
			var commonStructureService = (ICommonStructureService)GetTeamProjectCollection().GetService(typeof(ICommonStructureService));
			return commonStructureService.GetNode(iterationNode.Uri.ToString());
		}

		public WorkItem GetWorkItem(int workItemId)
		{
			var workItemStore = GetTeamProjectCollection().GetService<WorkItemStore>();
			return workItemStore.GetWorkItem(workItemId);
		}

		public WorkItem GetParentWorkItem(WorkItem workItem)
		{
			var parentLink = workItem.WorkItemLinks.OfType<WorkItemLink>().FirstOrDefault(x => x.LinkTypeEnd.Name == "Parent");
			if (parentLink != null)
			{
				var workItemStore = GetTeamProjectCollection().GetService<WorkItemStore>();
				return workItemStore.GetWorkItem(parentLink.TargetId);
			}
			return null;
		}

		public List<WorkItem> GetChildWorkItems(WorkItem workItem, string childWorkItemType = null)
		{
			var workItemStore = GetTeamProjectCollection().GetService<WorkItemStore>();
			var childLinks = workItem.WorkItemLinks.OfType<WorkItemLink>().Where(x => x.LinkTypeEnd.Name == "Child").ToList();
			return childLinks.Select(childLink => workItemStore.GetWorkItem(childLink.TargetId))
				.Where(childWorkItem => childWorkItemType == null || childWorkItem.Type.Name == childWorkItemType)
				.ToList();
		}

		public int GetWorkItemQueryResultCount(string wiq)
		{
			var workItemStore = GetTeamProjectCollection().GetService<WorkItemStore>();
			var query = new Query(workItemStore, ResolveWiqMacros(wiq));
			return query.RunCountQuery();
		}

		#endregion

		#region Version Control

		public IEnumerable<PendingChange> GetPendingChanges()
		{
			var vcs = _requestContext.GetService<TeamFoundationVersionControlService>();
			var submittedItems = _notification.GetSubmittedItems(_requestContext).Select(a => new ItemSpec(a, RecursionType.None)).ToArray();
			return vcs.QueryPendingChangesForWorkspace(_requestContext, _notification.WorkspaceName, _notification.WorkspaceOwner.UniqueName, submittedItems, false, submittedItems.Length, null, true).CurrentEnumerable<PendingChange>();
		}

		#endregion

		#region Identities

		public bool IsInGroup(string groupName, IdentityDescriptor member)
		{
			var ims = _requestContext.GetService<TeamFoundationIdentityService>();
			var group = ims.ReadIdentity(_requestContext, IdentitySearchFactor.DisplayName, groupName);
			return ims.IsMember(_requestContext, group.Descriptor, member);
		}

		#endregion

		#region private methods

		private Uri GetTFSUri()
		{
			var locationService = this._requestContext.GetService<TeamFoundationLocationService>();
			return new Uri(locationService.GetServerAccessMapping(_requestContext).AccessPoint + "/" + _requestContext.ServiceHost.Name);
		}

		private TfsTeamProjectCollection GetTeamProjectCollection()
		{
			if (tfsTeamProjectCollection == null)
			{
				tfsTeamProjectCollection = new TfsTeamProjectCollection(GetTFSUri());
			}
			return tfsTeamProjectCollection;
		}

		private string ResolveWiqMacros(string wiq)
		{
			var associatedWorkItems = _notification.NotificationInfo.WorkItemInfo;
			string resolvedWiq = wiq;

			if (Regex.IsMatch(wiq, "@AssociatedWorkItems", RegexOptions.IgnoreCase))
			{
				var workItemIds = associatedWorkItems.AsEnumerable().Select(wiInfo => wiInfo.Id);
				var formattedWorkItemIds = string.Format("({0})", String.Join(",", workItemIds));
				resolvedWiq = Regex.Replace(resolvedWiq, "@AssociatedWorkItems", formattedWorkItemIds, RegexOptions.IgnoreCase);
			}

			if (Regex.IsMatch(wiq, "@Me", RegexOptions.IgnoreCase))
			{
				string userDisplayNameWithQuotes = "'" + GetUserDisplayName() + "'";
				resolvedWiq = Regex.Replace(resolvedWiq, "@Me", userDisplayNameWithQuotes, RegexOptions.IgnoreCase);
			}

			return resolvedWiq;
		}

		private string GetUserDisplayName()
		{
			var ims = _requestContext.GetService<TeamFoundationIdentityService>();
			var user = ims.ReadIdentity(_requestContext, IdentitySearchFactor.AccountName, _requestContext.DomainUserName);
			return user.DisplayName;
		}

		#endregion

		#region IDisposable

		public void Dispose()
		{
			if (tfsTeamProjectCollection != null)
			{
				tfsTeamProjectCollection.Dispose();
			}
		}

		#endregion
	}
}
