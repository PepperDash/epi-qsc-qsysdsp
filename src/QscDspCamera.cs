using System;
using System.Linq;
using Crestron.SimplSharpPro.DeviceSupport;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Bridges;

namespace QscQsysDspPlugin
{
	/// <summary>
	/// QSC DSP Camera class
	/// </summary>
    public class QscDspCamera : Device, IBridgeAdvanced, IOnline
	{
		QscDsp _Dsp;
		public QscDspCameraConfig Config { get; private set; }
		string LastCmd;
		private bool _Online;
		public bool Online
		{
			set
			{
				this._Online = value;
				IsOnline.FireUpdate();
			}
			get
			{
				return this._Online;
			}
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="dsp">QscDsp</param>
		/// <param name="key">string</param>
		/// <param name="name">string</param>
		/// <param name="dc">QscDspCameraConfig</param>
		public QscDspCamera(QscDsp dsp, string key, string name, QscDspCameraConfig dc)
			: base(key, name)
		{
			_Dsp = dsp;
			Config = dc;
            IsOnline = new BoolFeedback(() => Online);
			DeviceManager.AddDevice(this);

		}

		/// <summary>
		/// Moves a camera 
		/// </summary>
		/// <param name="button">eCameraPtzControls</param>
		public void MoveCamera(eCameraPtzControls button)
		{
			string tag = null;

			switch (button)
			{
				case eCameraPtzControls.Stop:
					{
						_Dsp.SendControlSetValue(LastCmd, 0.0);
						break;
					}
				case eCameraPtzControls.PanLeft:  tag = Config.PanLeftTag;  break;
				case eCameraPtzControls.PanRight: tag = Config.PanRightTag; break;
				case eCameraPtzControls.TiltUp:   tag = Config.TiltUpTag;   break;
				case eCameraPtzControls.TiltDown: tag = Config.TiltDownTag; break;
				case eCameraPtzControls.ZoomIn:   tag = Config.ZoomInTag;   break;
				case eCameraPtzControls.ZoomOut:  tag = Config.ZoomOutTag;  break;
			}

			if (tag != null)
			{
				LastCmd = tag;
				_Dsp.SendControlSetValue(tag, 1.0);
			}
		}

		/// <summary>
		/// Camera privacy on
		/// </summary>
		public void PrivacyOn()
		{
			_Dsp.SendControlSetValue(Config.Privacy, 1.0);
		}

		/// <summary>
		/// Camera privacy off
		/// </summary>
		public void PrivacyOff()
		{
			_Dsp.SendControlSetValue(Config.Privacy, 0.0);
		}

		/// <summary>
		/// Recalls a preset with the provided number
		/// </summary>
		/// <param name="presetNumber">ushort</param>
		public void RecallPreset(ushort presetNumber)
		{
			Debug.Console(2, this, "Recall Camera Preset {0}", presetNumber);
			if (Config.Presets.ElementAt(presetNumber).Value != null)
			{
				var preset = Config.Presets.ElementAt(presetNumber).Value;
				_Dsp.SendSnapshotLoad(preset.Bank, preset.Number);
			}
		}

		/// <summary>
		/// Saves a preset with the provided number
		/// </summary>
		/// <param name="presetNumber">ushort</param>
		public void SavePreset(ushort presetNumber)
		{
			if (Config.Presets.ElementAt(presetNumber).Value != null)
			{
				var preset = Config.Presets.ElementAt(presetNumber).Value;
				_Dsp.SendSnapshotSave(preset.Bank, preset.Number);
			}
		}

		/// <summary>
		/// Writes the preset name
		/// </summary>
		/// <param name="newLabel">string</param>
		/// <param name="presetNumber">ushort</param>
		public void WritePresetName(string newLabel, ushort presetNumber)
		{
			if (Config.Presets.ElementAt(presetNumber - 1).Value != null && newLabel.Length > 0 && Config.Presets.ElementAt(presetNumber - 1).Value.Label != newLabel)
			{
				Config.Presets.ElementAt(presetNumber - 1).Value.Label = newLabel;
				_Dsp.Config.Properties["CameraControlBlocks"][Key]["Presets"][Config.Presets.ElementAt(presetNumber - 1).Key]["label"] = newLabel;

				_Dsp.WriteConfig();
			}

		}

		/// <summary>
		/// Adds the camera online control to the QRC change group
		/// </summary>
		public void Subscribe()
		{
			try
			{
				if (!string.IsNullOrEmpty(Config.OnlineStatus))
				{
					_Dsp.AddControlToChangeGroup(Config.OnlineStatus);
				}
			}
			catch (Exception e)
			{
				Debug.Console(2, "QscDspCamera Subscription Error: '{0}'\n", e);
			}
		}

		/// <summary>
		/// Parses the change group subscription message
		/// </summary>
		/// <param name="customName"></param>
		/// <param name="value"></param>
		/// <param name="absoluteValue"></param>
		public void ParseSubscriptionMessage(string customName, string value, string absoluteValue)
		{

			// Check for valid subscription response
			Debug.Console(1, this, "CameraOnline {0} Response: '{1}'", customName, value);

			if (value == "true")
			{
				Online = true;

			}
			else if (value == "false")
			{
				Online = false;
			}

		}


	    public BoolFeedback IsOnline { get; private set; }

        #region IBridgeAdvanced Members

        /// <summary>
        /// Link to API
        /// </summary>
        /// <param name="trilist">BasicTrilist</param>
        /// <param name="joinStart">uint</param>
        /// <param name="joinMapKey">string</param>
        /// <param name="bridge">EiscApiAdvanced</param>
        public void LinkToApi(BasicTriList trilist, uint joinStart, string joinMapKey, EiscApiAdvanced bridge)
        {
            this.LinkToApiExt(trilist, joinStart, joinMapKey, bridge);
        }

        #endregion
    }

	/// <summary>
	/// Camera PTZ controls enum
	/// </summary>
	public enum eCameraPtzControls
	{
		Stop,
		PanLeft,
		PanRight,
		TiltUp,
		TiltDown,
		ZoomIn,
		ZoomOut
	}
}