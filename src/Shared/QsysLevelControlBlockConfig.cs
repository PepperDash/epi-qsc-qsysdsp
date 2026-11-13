using Newtonsoft.Json;

namespace PepperDash.Essentials.Plugins.Qsc.Qsys
{
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
	public class QsysLevelControlBlockConfig
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
}
