using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.TeamFoundation.Framework.Server;
using Microsoft.TeamFoundation.VersionControl.Server;
using Rrd.Tfs.Plugins.Config;
using Rrd.Tfs.Plugins.Properties;

namespace Rrd.Tfs.Plugins.Policies
{
	public class SingleTeamProjectCodePolicyEnforcer : IPolicyEnforcer
	{
		public bool CheckPolicy(TeamFoundationRequestContext requestContext, CheckinNotification args,
			PolicyConfigurationElement policyConfiguration, out string statusMessage)
		{
			statusMessage = string.Empty;
			var submittedItems = args.GetSubmittedItems(requestContext);
			var regex = new Regex(@"\$/([^/]+)/(.+)");
			List<string> distinctProjNames = new List<string>();
			foreach (var item in submittedItems)
			{
				if (regex.IsMatch(item))
				{
					string projName = regex.Match(item).Groups[1].Value;
					if (!distinctProjNames.Contains(projName))
					{
						distinctProjNames.Add(projName);
					}
				}
			}
			if (distinctProjNames.Count != 1)
			{
				statusMessage = Resources.SingleTeamProject;
				return false;
			}
			return true;
		}
	}
}
