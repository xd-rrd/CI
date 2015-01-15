using Microsoft.TeamFoundation.Framework.Server;
using Microsoft.TeamFoundation.VersionControl.Server;
using Rrd.Tfs.Plugins.Config;
using Rrd.Tfs.Plugins.Properties;

namespace Rrd.Tfs.Plugins.Policies
{
	public class NeedCommentPolicyEnforcer:IPolicyEnforcer
	{
		public bool CheckPolicy(TeamFoundationRequestContext requestContext, CheckinNotification args,
			PolicyConfigurationElement policyConfiguration, out string statusMessage)
		{
			statusMessage = string.Empty;
			if (string.IsNullOrEmpty(args.Comment))
			{
				statusMessage = Resources.NeedComment;
				return false;
			}
			return true;
		}
	}
}
