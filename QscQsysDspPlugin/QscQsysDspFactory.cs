using System;
using System.Collections.Generic;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Config;

namespace QscQsysDspPlugin
{
	/// <summary>
	/// Plugin factory for devices that require communications using IBasicCommunications or custom communication methods
	/// </summary>
	public class QscQsysDspFactory : EssentialsPluginDeviceFactory<QscQsysDsp>
	{
		/// <summary>
		/// Plugin device factory constructor
		/// </summary>
		/// <remarks>
		/// Update the MinimumEssentialsFrameworkVersion & TypeNames as needed when creating a plugin
		/// </remarks>
		public QscQsysDspFactory()
		{
			// Set the minimum Essentials Framework Version
			MinimumEssentialsFrameworkVersion = "1.6.8";

			// In the constructor we initialize the list with the typenames that will build an instance of this device
			// only include unique typenames, when the constructur is used all the typenames will be evaluated in lower case.
			TypeNames = new List<string>() { "qscdsp", "qscqsys", "qsys" };
		}

		/// <summary>
		/// Builds and returns an instance of QscQsysDsp
		/// </summary>
		public override EssentialsDevice BuildDevice(DeviceConfig dc)
		{
			try
			{
				Debug.Console(0, new string('*', 80));
				Debug.Console(0, new string('*', 80));
				Debug.Console(0, "[{0}] Factory Attempting to create new device from type: {1}", dc.Key, dc.Type);

				var properties = dc.Properties.ToObject<QscQsysDspPropertiesConfig>();
				if (properties == null)
				{
					Debug.Console(2,"[{0}] QscQsysPlugin: failed to read properties config for {1}", dc.Key, dc.Name);
					return null;
				}

				// build the plugin device comms (for all other comms methods) & check for null			
				var comms = CommFactory.CreateCommForDevice(dc);
				if (comms != null) return new QscQsysDsp(dc.Key, dc.Name, comms, properties);
				Debug.Console(0, "[{0}] Factory: failed to create comm for {1}", dc.Key, dc.Name);
				
				return null;
			}
			catch (Exception ex)
			{
				Debug.Console(0, "[{0}] Factory BuildDevice Exception: {1}", dc.Key, ex);
				return null;
			}
		}
	}

}