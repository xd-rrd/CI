using System.Collections.Generic;
using Microsoft.TeamFoundation.Framework.Server;
using System;
using System.Linq;
using Microsoft.TeamFoundation.VersionControl.Server;
using Rrd.Tfs.Plugins.Config;

namespace Rrd.Tfs.Plugins.Policies
{
	public interface IPolicyEnforcer
	{
		bool CheckPolicy(TeamFoundationRequestContext requestContext, CheckinNotification args, PolicyConfigurationElement policyConfiguration, out string statusMessage);
	}

	public class PolicyEnforcerFactory
	{
		private static readonly Dictionary<string, Type> _policyTypes;

		static PolicyEnforcerFactory()
		{
			var types =
				typeof (PolicyEnforcerFactory).Assembly.GetTypes()
					.Where(t => t.IsClass && t.GetInterface(typeof (IPolicyEnforcer).Name, false) != null)
					.ToList();
			_policyTypes=new Dictionary<string, Type>();
			foreach (var type in types)
			{
				_policyTypes.Add(type.Name, type);
			}
		}

		public static IPolicyEnforcer CreatePolicyEnforcer(string policyTypeName)
		{
			if (_policyTypes.ContainsKey(policyTypeName))
			{
				var instance = Activator.CreateInstance(_policyTypes[policyTypeName]);
				return instance as IPolicyEnforcer;
			}
			return null;
		}
	}
}
