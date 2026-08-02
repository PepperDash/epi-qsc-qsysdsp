using System.Collections.Generic;
using Newtonsoft.Json;
using PepperDash.Core;
using PepperDash.Essentials.Core;

namespace PepperDash.Essentials.Plugins.Qsc.Qsys.RemoteControlProtocol
{
	/// <summary>
	/// QSC Q-SYS Remote Control Protocol (QRC) properties config class
	/// </summary>
	/// <remarks>
	/// Tags (levelInstanceTag, muteInstanceTag, etc.) support two addressing modes:
	/// - Named Control: a flat string, e.g. "MainGain"
	/// - Component Control: "ComponentName#ControlName", split on '#'
	/// </remarks>
	/// <code>
	/// "key": "dsp-1",
	/// "name": "QSC Q-Sys QRC Plugin",
	/// "type": "qscDspQrc",
	/// "group": "plugin",
	/// "properties": {
	///		"control": {
	///			"method": "tcpIp",
	///			"tcpSshProperties": {
	///				"address": "",
	///				"port": 1710,
	///				"username": "",
	///				"password": "",
	///				"autoReconnect": true,
	///				"autoReconnectIntervalMs": 5000
	///			}
	///		},
	///		"prefix": "",
	///		"levelControlBlocks": {},
	///		"presets": {},
	///		"dialerControlBlock": {},
	///		"cameraControlBlocks": {}
	/// }
	/// </code>
	public class QsysQrcPropertiesConfig
	{
		public CommunicationMonitorConfig CommunicationMonitorProperties { get; set; }

		[JsonProperty("control")]
		public ControlPropertiesConfig Control { get; set; }

		[JsonProperty("prefix")]
		public string Prefix { get; set; }

		[JsonProperty("levelControlBlocks")]
		public Dictionary<string, QsysLevelControlBlockConfig> LevelControlBlocks { get; set; }

		[JsonProperty("dialerControlBlocks")]
		public Dictionary<string, QscDialerConfig> DialerControlBlocks { get; set; }

		[JsonProperty("presets")]
		public Dictionary<string, QsysPresets> Presets { get; set; }

		[JsonProperty("cameraControlBlocks")]
		public Dictionary<string, QsysCameraConfig> CameraControlBlocks { get; set; }
	}
}
