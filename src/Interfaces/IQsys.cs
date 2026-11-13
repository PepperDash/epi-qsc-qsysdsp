using System.Collections.Generic;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Config;

namespace PepperDash.Essentials.Plugins.Qsc.Qsys
{
	/// <summary>
	/// Protocol-neutral abstraction shared by the External Control (ECP) and Remote Control (QRC)
	/// implementations, so control points (levels, dialers, cameras) can be used with either protocol.
	/// </summary>
	public interface IQsys : IKeyed
	{
		string DspName { get; }
		DeviceConfig Config { get; }
		StatusMonitorBase CommunicationMonitor { get; }
		BoolFeedback IsOnline { get; }
		BoolFeedback IsPrimaryFeedback { get; }
		BoolFeedback IsActiveFeedback { get; }
		string AutoTrackingKey { get; }

		Dictionary<string, QsysLevelControl> LevelControlPoints { get; }
		Dictionary<string, QsysDialer> Dialers { get; }
		Dictionary<string, QsysCamera> Cameras { get; }
		List<QsysPresets> PresetList { get; }

		void SetPrefix(string prefix);
		void SetIpAddress(string hostname);
		void StatusGet(bool enable);
		void ProcessSimulatedRx(string s);
		void WriteConfig();

		void AddPreset(QsysPresets s);
		void RunPreset(string name);
		void RunPresetNumber(ushort n);
		void SavePreset(string name);
		void SavePresetNumber(ushort n);
		void RecallPreset(string key);

		/// <summary>
		/// Sets a control to an absolute value (e.g. mute state, dialer digit/tag values)
		/// </summary>
		void SendControlValue(string tag, string value);

		/// <summary>
		/// Sets a control to a normalized 0-1 position (e.g. fader level)
		/// </summary>
		void SendControlPosition(string tag, string value);

		/// <summary>
		/// Ramps a control up or down (e.g. press-and-hold volume up/down)
		/// </summary>
		void SendControlRelative(string tag, bool increase);

		/// <summary>
		/// Sets a control to a string value (e.g. dial string)
		/// </summary>
		void SendControlString(string tag, string value);

		/// <summary>
		/// Triggers a momentary control (e.g. keypad press, connect/disconnect)
		/// </summary>
		void TriggerControl(string tag);

		/// <summary>
		/// Requests the current value of a control
		/// </summary>
		void GetControl(string tag);

		/// <summary>
		/// Subscribes to change notifications for a control
		/// </summary>
		void SubscribeControl(string tag);

		/// <summary>
		/// Recalls a snapshot/preset bank and number (e.g. camera presets)
		/// </summary>
		void RecallSnapshot(string bank, string number, string rampTime);

		/// <summary>
		/// Saves a snapshot/preset bank and number (e.g. camera presets)
		/// </summary>
		void SaveSnapshot(string bank, string number);

		/// <summary>
		/// Enumerates every component/control in the running design and writes the result to a JSON file
		/// </summary>
		void GetAllComponentsAndControls();
	}
}
