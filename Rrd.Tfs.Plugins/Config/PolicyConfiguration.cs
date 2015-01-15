using System;
using System.Collections.Generic;
using System.Configuration;

namespace Rrd.Tfs.Plugins.Config
{
	public class PluginConfigSection : ConfigurationSection
	{
		[ConfigurationProperty("teamProjects")]
		public TeamProjectConfigurationElementCollection TeamProjects {
			get
			{
				return (TeamProjectConfigurationElementCollection)this["teamProjects"];
			} 
		}
	}

	public class TeamProjectConfigurationElementCollection : ConfigurationElementCollection
	{
		protected override ConfigurationElement CreateNewElement()
		{
			return new TeamProjectConfigurationSection();
		}

		protected override object GetElementKey(ConfigurationElement element)
		{
			if (element == null)
			{
				throw new ArgumentNullException("element");
			}
			return ((TeamProjectConfigurationSection) element).Name;
		}
	}

	public class TeamProjectConfigurationSection : ConfigurationSection
	{
		[ConfigurationProperty("name", IsRequired = true)]
		public string Name
		{
			get { return (string) base["name"]; }
		}

		public List<PolicyConfigurationElement> Policies { get; set; }

		[ConfigurationProperty("needCodeReviewPolicy")]
		public NeedCodeReviewConfigurationElement NeedCodeReviewPolicy
		{
			get { return (NeedCodeReviewConfigurationElement)this["needCodeReviewPolicy"]; }
		}

		[ConfigurationProperty("needCommentPolicy")]
		public NeedCommentConfigurationElement NeedCommentPolicy
		{
			get { return (NeedCommentConfigurationElement)this["needCommentPolicy"]; }
		}

		[ConfigurationProperty("associateWithSingleTeamProjectPolicy")]
		public AssociateWithSingleTeamProjectConfigurationElement TeamProjectPolicy
		{
			get { return (AssociateWithSingleTeamProjectConfigurationElement)this["associateWithSingleTeamProjectPolicy"]; }
		}

		[ConfigurationProperty("associateWithWorkItemPolicy")]
		public AssociateWithWorkItemConfigurationElement WorkItemPolicy
		{
			get { return (AssociateWithWorkItemConfigurationElement)this["associateWithWorkItemPolicy"]; }
		}
	}

	public class NeedCodeReviewConfigurationElement : PolicyConfigurationElement
	{
		[ConfigurationProperty("allowedReviewers")]
		public string AllowedReviewers
		{
			get { return (string)base["allowedReviewers"]; }
		}

		[ConfigurationProperty("allowSelfReview", DefaultValue=false)]
		public bool AllowSelfReview
		{
			get { return (bool) base["allowSelfReview"]; }
		}
	}

	public class NeedCommentConfigurationElement : PolicyConfigurationElement
	{
	}

	public class AssociateWithSingleTeamProjectConfigurationElement : PolicyConfigurationElement
	{
	}

	public class AssociateWithWorkItemConfigurationElement : PolicyConfigurationElement
	{
		[ConfigurationProperty("allowedWorkItemTypes", DefaultValue = "Task, Bug")]
		public string AllowedWorkItemTypes
		{
			get { return (string)base["allowedWorkItemTypes"]; }
		}
	}

	public class PolicyConfigurationElement : ConfigurationElement
	{
		[ConfigurationProperty("enabled", DefaultValue = false)]
		public bool Enabled
		{
			get { return (bool)base["enabled"]; }
		}

		[ConfigurationProperty("type", IsRequired = true)]
		public string TypeName
		{
			get { return (string) base["type"]; }
		}
	}
}
