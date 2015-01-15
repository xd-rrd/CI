using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.TeamFoundation.Common;
using Microsoft.TeamFoundation.Framework.Server;
using Microsoft.TeamFoundation.VersionControl.Server;
using Rrd.Tfs.Plugins.Config;
using Rrd.Tfs.Plugins.Policies;

namespace Rrd.Tfs.Plugins
{
	public class PolicyChecker : ISubscriber
	{
		public Type[] SubscribedTypes()
		{
			return new[] { typeof(CheckinNotification) };
		}

		public string Name
		{
			get { return string.Format("{0} {1}", Settings.CompanyName, this.GetType().Name); }
		}

		public SubscriberPriority Priority
		{
			get { return SubscriberPriority.High; }
		}

		public EventNotificationStatus ProcessEvent(
			TeamFoundationRequestContext requestContext, NotificationType notificationType,
			object notificationEventArgs, out int statusCode, out string statusMessage,
			out ExceptionPropertyCollection properties)
		{
			statusCode = 0;
			statusMessage = string.Empty;
			properties = null;
			try
			{
				var args = notificationEventArgs as CheckinNotification;
				if (notificationType == NotificationType.DecisionPoint && args != null)
				{
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
					string teamProjectName = distinctProjNames.FirstOrDefault();
					var pluginConfig = PluginConfigManager.Section;
					TeamProjectConfigurationSection teamProjConfig = null;
					foreach (TeamProjectConfigurationSection configElem in pluginConfig.TeamProjects)
					{
						if (configElem.Name.Equals(teamProjectName, StringComparison.InvariantCultureIgnoreCase))
						{
							teamProjConfig = configElem;
							break;
						}
					}

					if (teamProjConfig != null)
					{
						foreach (PolicyConfigurationElement policyConfiguration in teamProjConfig.Policies)
						{
							var policyEnforcer = PolicyEnforcerFactory.CreatePolicyEnforcer(policyConfiguration.TypeName);
							if (policyConfiguration != null && policyConfiguration.Enabled)
							{
								bool isValid = policyEnforcer.CheckPolicy(requestContext, args, policyConfiguration, out statusMessage);
								if (!isValid)
									return EventNotificationStatus.ActionDenied;
							}
						}
					}
				}
				return EventNotificationStatus.ActionPermitted;
			}
			catch (Exception ex)
			{
				statusMessage = string.Format("Error in {0}: {1}", this.Name, ex.Message);
				string errorDetails = string.Format("Error in plugin: {0}: {1}\n{2}", this.Name, ex.Message, ex.StackTrace);
				EventLog.WriteEntry("TFS Service", errorDetails, EventLogEntryType.Error);
				return EventNotificationStatus.ActionDenied;
			}
		}
	}
}
