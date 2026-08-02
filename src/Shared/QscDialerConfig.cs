using Newtonsoft.Json;

namespace PepperDash.Essentials.Plugins.Qsc.Qsys
{
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
}
