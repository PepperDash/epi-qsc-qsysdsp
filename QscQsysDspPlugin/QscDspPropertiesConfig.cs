using System.Collections.Generic;
using Newtonsoft.Json;
using PepperDash.Core;
using PepperDash.Essentials.Core;

namespace QscQsysDsp
{
	/// <summary>
	/// 
	/// </summary>
	public class QscDspPropertiesConfig
	{
		public CommunicationMonitorConfig CommunicationMonitorProperties { get; set; }

		public ControlPropertiesConfig Control { get; set; }

		/// <summary>
		/// These are key-value pairs, string id, string type.  
		/// Valid types are level and mute.
		/// Need to include the index values somehow
		/// </summary>
		/// 

		public string Prefix { get; set; }
		public Dictionary<string, QscDspLevelControlBlockConfig> LevelControlBlocks { get; set; }
		public Dictionary<string, QscDialerConfig> dialerControlBlocks { get; set; }
		public Dictionary<string, QscDspPresets> presets { get; set; }
		public Dictionary<string, QscDspCameraConfig> CameraControlBlocks { get; set; }
	}
	public class QscDspLevelControlBlockConfig
	{
		public bool Disabled { get; set; }
		public string Label { get; set; }
		public string LevelInstanceTag { get; set; }
		public string MuteInstanceTag { get; set; }
		public bool HasMute { get; set; }
		public bool HasLevel { get; set; }
		public bool IsMic { get; set; }
		public bool UseAbsoluteValue { get; set; }
		public bool UnmuteOnVolChange { get; set; }
	}

	public class QscDialerConfig
	{		
		public string incomingCallRingerTag { get; set; }
		public string dialStringTag { get; set; }
		public string disconnectTag { get; set; }
		public string connectTag { get; set; }
		public string callStatusTag { get; set; }
		public string hookStatusTag { get; set; }
		public string doNotDisturbTag { get; set; }
		public string autoAnswerTag { get; set; }

		public string keypadBackspaceTag { get; set; }
		public string keypadClearTag { get; set; }
		public string keypad1Tag { get; set; }
		public string keypad2Tag { get; set; }
		public string keypad3Tag { get; set; }
		public string keypad4Tag { get; set; }
		public string keypad5Tag { get; set; }
		public string keypad6Tag { get; set; }
		public string keypad7Tag { get; set; }
		public string keypad8Tag { get; set; }
		public string keypad9Tag { get; set; }
		public string keypad0Tag { get; set; }
		public string keypadPoundTag { get; set; }
		public string keypadStarTag { get; set; }

		public bool ClearOnHangup { get; set; }
	}

	public class QscDspPresets
	{
		private string _label;
		public string label
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
		public string preset { get; set; }
		public string Bank { get; set; }
		public int number { get; set; }
		public StringFeedback LabelFeedback;
		public QscDspPresets()
		{
			LabelFeedback = new StringFeedback(() => { return label; });
		}
	}

	public class QscDspCameraConfig
	{
		public string PanLeftTag { get; set; }
		public string PanRightTag { get; set; }
		public string TiltUpTag { get; set; }
		public string TiltDownTag { get; set; }
		public string ZoomInTag { get; set; }
		public string ZoomOutTag { get; set; }
		public string PresetBankTag { get; set; }
		public string Privacy { get; set; }
		public Dictionary<string, QscDspPresets> Presets { get; set; }
		public string OnlineStatus { get; set; }
	}
}