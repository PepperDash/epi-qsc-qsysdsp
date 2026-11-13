using System.Collections.Generic;
using Newtonsoft.Json;
using PepperDash.Core;
using PepperDash.Essentials.Core;


namespace PepperDash.Essentials.Plugins.Qsc.Qsys.ExternalControlProtocol
{
	/// <summary>
	/// QSC DSP Properties config class
	/// </summary>
	/// <remarks>
	/// These are key-value paris, string id, string type.
	/// Valid types are level and mute.
	/// Need to include the index values somehow.
	/// </remarks>
	/// <code>
	/// "key": "dsp-1",
	/// "name": "QSC Q-Sys DSP Plugin",
	/// "type": "qscdsp",
	/// "group": "plugin",
	/// "properties": {
	///		"control": {
	///			"method": "tcpIp",
	///			"endOfLineString": "\n",
	///			"deviceReadyResponse": "",
	///			"tcpSshProperties": {
	///				"address": "",
	///				"port": 1702,
	///				"username": "default",
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
	public class QsysEcpPropertiesConfig
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
