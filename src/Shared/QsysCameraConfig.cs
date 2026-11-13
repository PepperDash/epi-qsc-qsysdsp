using System.Collections.Generic;
using Newtonsoft.Json;

namespace PepperDash.Essentials.Plugins.Qsc.Qsys
{
	/// <summary>
	/// QSC Camera Configuration
	/// </summary>
	/// <code>
	/// "cameraControlBlocks": {
	///		"camera-1": {
	///			"panLeftTag": "CAM01_LEFT",
	///         "panRightTag": "CAM01_RIGHT",
	///			"tiltUpTag": "CAM01_UP",
	///			"tiltDownTag": "CAM01_DOWN",
	///			"zoomInTag": "CAM01_ZOOMIN",
	///			"zoomOutTag": "CAM01_ZOOMOUT",
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
	public class QsysCameraConfig
	{
		[JsonProperty("panLeftTag")]
		public string PanLeftTag { get; set; }

		[JsonProperty("panRightTag")]
		public string PanRightTag { get; set; }

		[JsonProperty("tiltUpTag")]
		public string TiltUpTag { get; set; }

		[JsonProperty("tiltDownTag")]
		public string TiltDownTag { get; set; }

		[JsonProperty("zoomInTag")]
		public string ZoomInTag { get; set; }

		[JsonProperty("zoomOutTag")]
		public string ZoomOutTag { get; set; }

		[JsonProperty("presetBankTag")]
		public string PresetBankTag { get; set; }

		[JsonProperty("privacy")]
		public string Privacy { get; set; }

		[JsonProperty("onlineStatus")]
		public string OnlineStatus { get; set; }

		[JsonProperty("presets")]
		public Dictionary<string, QsysPresets> Presets { get; set; }
	}
}
