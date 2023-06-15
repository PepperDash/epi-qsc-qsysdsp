using System.Collections.Generic;
using Newtonsoft.Json;
using PepperDash.Core;
using PepperDash.Essentials.Core;


namespace QscQsysDspPlugin
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
	public class QscDspPropertiesConfig
	{
		public CommunicationMonitorConfig CommunicationMonitorProperties { get; set; }

		[JsonProperty("control")]
		public ControlPropertiesConfig Control { get; set; }

		[JsonProperty("prefix")]
		public string Prefix { get; set; }

		[JsonProperty("levelControlBlocks")]
		public Dictionary<string, QscDspLevelControlBlockConfig> LevelControlBlocks { get; set; }

		[JsonProperty("dialerControlBlocks")]
		public Dictionary<string, QscDialerConfig> DialerControlBlocks { get; set; }

		[JsonProperty("presets")]
		public Dictionary<string, QscDspPresets> Presets { get; set; }

		[JsonProperty("cameraControlBlocks")]
		public Dictionary<string, QscDspCameraConfig> CameraControlBlocks { get; set; }
	}

	/// <summary>
	/// QSC Presets Configurations
	/// This class is used for Level Control Blocks as well as Camera presets
	/// </summary>
	/// <remarks>
	/// LabelFeedback is not required in the JSON configuration.  It is used to return the defined label as a feedback on the bridge.
	/// </remarks>
	/// <code>
	/// "presets": {
	///		"preset-key": {
	///			"label": "Preset X",
	///			"preset": "PRESET TAG"
	///			"bank": "PRESET BANK",
	///			"number": "PRESET NUMBER"
	///		}
	/// }
	/// </code>
	public class QscDspPresets
	{
		// backer field
		private string _label;

		[JsonProperty("label")]
		public string Label
		{
			get
			{
				return this._label;
			}
			set
			{
				this._label = value;
				LabelFeedback.FireUpdate();
			}
		}

		[JsonProperty("preset")]
		public string Preset { get; set; }

		[JsonProperty("bank")]
		public string Bank { get; set; }

		[JsonProperty("number")]
		public int Number { get; set; }

		[JsonProperty("labelFeedback")]
		public StringFeedback LabelFeedback;

		/// <summary>
		/// Constructor
		/// </summary>
		public QscDspPresets()
		{
			LabelFeedback = new StringFeedback(() => { return Label; });
		}
	}

	/// <summary>
	/// QSC Level Control Block Configuration 
	/// </summary>
	/// <code>
	/// "levelControlBlocks": {
	///		"fader-key": {
	///			"label": "Fader X",
	///			"levelInstanceTag": "NAMED_CONTROL_VOL",
	///			"muteInstanceTag": "NAMED_CONTROL_MUTE",
	///			"disabled": false,
	///			"hasLevel": true,
	///			"hasMute": true,
	///			"isMic": false,
	///			"useAbsoluteValue": false,
	///			"unmuteOnVolchange": true
	///		}
	/// }
	/// </code>
	public class QscDspLevelControlBlockConfig
	{
		[JsonProperty("label")]
		public string Label { get; set; }

		[JsonProperty("levelInstanceTag")]
		public string LevelInstanceTag { get; set; }

		[JsonProperty("muteInstanceTag")]
		public string MuteInstanceTag { get; set; }

		[JsonProperty("disabled")]
		public bool Disabled { get; set; }

		[JsonProperty("hasLevel")]
		public bool HasLevel { get; set; }

		[JsonProperty("hasMute")]
		public bool HasMute { get; set; }

		[JsonProperty("isMic")]
		public bool IsMic { get; set; }

		[JsonProperty("useAbsoluteValue")]
		public bool UseAbsoluteValue { get; set; }

		[JsonProperty("unmuteOnVolChange")]
		public bool UnmuteOnVolChange { get; set; }
	}



	/// <summary>
	/// QSC Dialer Block Configuration
	/// </summary>
	/// <code>
	/// "dialerControlBlock": {
	///		"dialer-1": {
	///			"ClearOnHangup": true,
	///			"incomingCallRingerTag": "VOIP_RINGTRIG",
	///			"dialStringTag": "VOIP_DIALSTRING",
	///			"disconnectTag": "VOIP_DISCONNECT",
	///			"connectTag": "VOIP_CONNECT",
	///			"callStatusTag": "VOIP_STATUS",
	///			"hookStatusTag": "VOIP_OFFHOOK",
	///			"doNotDisturbTag": "VOIP_DND",
	///			"autoAnswerTag": "VOIP_AUTO_ANSWER",
	///			"keypadBackspaceTag": "VOIP_DIALSTRING_DEL",
	///			"keypadClearTag": "VOIP_DIALSTRING_CLEAR",
	///			"keypad1Tag": "VOIP_DTMF_1",
	///			"keypad2Tag": "VOIP_DTMF_2",
	///			"keypad3Tag": "VOIP_DTMF_3",
	///			"keypad4Tag": "VOIP_DTMF_4",
	///			"keypad5Tag": "VOIP_DTMF_5",
	///			"keypad6Tag": "VOIP_DTMF_6",
	///			"keypad7Tag": "VOIP_DTMF_7",
	///			"keypad8Tag": "VOIP_DTMF_8",
	///			"keypad9Tag": "VOIP_DTMF_9",
	///			"keypad0Tag": "VOIP_DTMF_0",
	///			"keypadStarTag": "VOIP_DTMF_*",
	///			"keypadPoundTag": "VOIP_DTMF_#"
	///		}
	/// }
	/// </code>
	public class QscDialerConfig
	{
		[JsonProperty("ClearOnHangup")]
		public bool ClearOnHangup { get; set; }

		[JsonProperty("incomingCallRingerTag")]
		public string IncomingCallRingerTag { get; set; }

		[JsonProperty("dialStringTag")]
		public string DialStringTag { get; set; }

		[JsonProperty("disconnectTag")]
		public string DisconnectTag { get; set; }

		[JsonProperty("connectTag")]
		public string ConnectTag { get; set; }

		[JsonProperty("callStatusTag")]
		public string CallStatusTag { get; set; }

		[JsonProperty("hookStatusTag")]
		public string HookStatusTag { get; set; }

		[JsonProperty("doNotDisturbTag")]
		public string DoNotDisturbTag { get; set; }

		[JsonProperty("autoAnswerTag")]
		public string AutoAnswerTag { get; set; }

		[JsonProperty("keypadBackspaceTag")]
		public string KeypadBackspaceTag { get; set; }

		[JsonProperty("keypadClearTag")]
		public string KeypadClearTag { get; set; }

		[JsonProperty("keypad1Tag")]
		public string Keypad1Tag { get; set; }

		[JsonProperty("keypad2Tag")]
		public string Keypad2Tag { get; set; }

		[JsonProperty("keypad3Tag")]
		public string Keypad3Tag { get; set; }

		[JsonProperty("keypad4Tag")]
		public string Keypad4Tag { get; set; }

		[JsonProperty("keypad5Tag")]
		public string Keypad5Tag { get; set; }

		[JsonProperty("keypad6Tag")]
		public string Keypad6Tag { get; set; }

		[JsonProperty("keypad7Tag")]
		public string Keypad7Tag { get; set; }

		[JsonProperty("keypad8Tag")]
		public string Keypad8Tag { get; set; }

		[JsonProperty("keypad9Tag")]
		public string Keypad9Tag { get; set; }

		[JsonProperty("keypad0Tag")]
		public string Keypad0Tag { get; set; }

		[JsonProperty("keypadPoundTag")]
		public string KeypadPoundTag { get; set; }

		[JsonProperty("keypadStarTag")]
		public string KeypadStarTag { get; set; }
	}

	/// <summary>
	/// QSC Camera Configuration
	/// </summary>
	/// <code>
	/// "cameraControlBlocks": {
	///		"camera-1": {
	///			"panLeftTag": "CAM01_LEFT",
	///         "panRightTag": "CAM01_RIGHT",
    ///         "panSpeedTag": "CAM01_PANSPEED",
	///			"tiltUpTag": "CAM01_UP",
    ///			"tiltDownTag": "CAM01_DOWN",
    ///         "tiltSpeedTag": "CAM01_TILTSPEED",
	///			"zoomInTag": "CAM01_ZOOMIN",
    ///			"zoomOutTag": "CAM01_ZOOMOUT",
    ///         "zoomSpeedTag": "CAM01_ZOOMSPEED",
	///			"privacy": "CAM01_PRIVACY",
	///			"onlineStatus": "CAM01_STATUS",
	///			"presets": {
	///				"preset01": {
	///					"label": "Default",
	///					"bank": "CAM01_PRESETS",
	///					"number": 1
	///				},
	///				"preset02": {
	///					"label": "Tight",
	///					"bank": "CAM01_PRESETS",
	///					"number": 2
	///				},
	///				"preset03": {
	///					"label": "Wide",
	///					"bank": "CAM01_PRESETS",
	///					"number": 3
	///				},
	///				"preset04": {
	///					"label": "User",
	///					"bank": "CAM01_PRESETS",
	///					"number": 4
	///				}
	///			}
	///		}
	/// }
	/// </code>
	public class QscDspCameraConfig
	{
		[JsonProperty("panLeftTag")]
		public string PanLeftTag { get; set; }

		[JsonProperty("panRightTag")]
        public string PanRightTag { get; set; }

        [JsonProperty("panSpeedTag")]
        public string PanSpeedTag { get; set; }

		[JsonProperty("tiltUpTag")]
		public string TiltUpTag { get; set; }

		[JsonProperty("tiltDownTag")]
        public string TiltDownTag { get; set; }

        [JsonProperty("tiltSpeedTag")]
        public string TiltSpeedTag { get; set; }

		[JsonProperty("zoomInTag")]
		public string ZoomInTag { get; set; }

		[JsonProperty("zoomOutTag")]
        public string ZoomOutTag { get; set; }

        [JsonProperty("zoomSpeedTag")]
        public string ZoomSpeedTag { get; set; }

		[JsonProperty("presetBankTag")]
		public string PresetBankTag { get; set; }

		[JsonProperty("privacy")]
		public string Privacy { get; set; }

		[JsonProperty("onlineStatus")]
		public string OnlineStatus { get; set; }

		[JsonProperty("presets")]
		public Dictionary<string, QscDspPresets> Presets { get; set; }
	}
}