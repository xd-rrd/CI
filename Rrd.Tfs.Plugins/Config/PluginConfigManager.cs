using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Reflection;

namespace Rrd.Tfs.Plugins.Config
{
	public class PluginConfigManager
	{
		private static readonly Assembly configurationDefiningAssembly;

		static PluginConfigManager()
		{
			string strPluginFile = new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath;
			string message = string.Format("Loading tfs code checkin policy configuration using assenbly {0}", strPluginFile);
			EventLog.WriteEntry("TFS Service", message, EventLogEntryType.Information);

			configurationDefiningAssembly = Assembly.LoadFrom(strPluginFile);
			var exeFileMap = new ExeConfigurationFileMap();
			exeFileMap.ExeConfigFilename = string.Format("{0}.config", strPluginFile);
			message = string.Format("Loading tfs code checkin policy configuration from file {0}", exeFileMap.ExeConfigFilename);
			EventLog.WriteEntry("TFS Service", message, EventLogEntryType.Information);

			var customConfig = ConfigurationManager.OpenMappedExeConfiguration(exeFileMap, ConfigurationUserLevel.None);

			// This handler is needed so that the GetSection is able to retrieve the type since this dll isn't in the GAC
			AppDomain.CurrentDomain.AssemblyResolve += ConfigResolveEventHandler;
			var configSection = customConfig.GetSection("TfsCheckinPolicy") as PluginConfigSection;
			if (configSection != null)
			{
				foreach (TeamProjectConfigurationSection teamProjConfig in configSection.TeamProjects)
				{
					teamProjConfig.Policies = new List<PolicyConfigurationElement>();
					if (teamProjConfig.NeedCodeReviewPolicy != null)
					{
						teamProjConfig.Policies.Add(teamProjConfig.NeedCodeReviewPolicy);
					}
					if (teamProjConfig.NeedCommentPolicy != null)
					{
						teamProjConfig.Policies.Add(teamProjConfig.NeedCommentPolicy);
					}
					if (teamProjConfig.TeamProjectPolicy != null)
					{
						teamProjConfig.Policies.Add(teamProjConfig.TeamProjectPolicy);
					}
					if (teamProjConfig.WorkItemPolicy != null)
					{
						teamProjConfig.Policies.Add(teamProjConfig.WorkItemPolicy);
					}
				}
			}
			else
			{
				message = string.Format("Failed to load tfs code checkin policy configuration from file {0}", exeFileMap.ExeConfigFilename);
				EventLog.WriteEntry("TFS Service", message, EventLogEntryType.Error);
			}
			Section = configSection;
			AppDomain.CurrentDomain.AssemblyResolve -= ConfigResolveEventHandler;
		}

		public static PluginConfigSection Section { get; private set; }

		private static Assembly ConfigResolveEventHandler(object sender, ResolveEventArgs args)
		{
			return configurationDefiningAssembly;
		}
	}
}
