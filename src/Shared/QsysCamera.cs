using System;
using System.Linq;
using Crestron.SimplSharpPro.DeviceSupport;
using PepperDash.Core;
using PepperDash.Core.Logging;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Bridges;

namespace PepperDash.Essentials.Plugins.Qsc.Qsys
{
	/// <summary>
	/// QSC DSP Camera class
	/// </summary>
    public class QsysCamera : Device, IBridgeAdvanced, IOnline
	{
		ExternalControlProtocol.QsysEcpController _Dsp;
		public QsysCameraConfig Config { get; private set; }
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
		/// <param name="dsp">Qsys</param>
		/// <param name="key">string</param>
		/// <param name="name">string</param>
		/// <param name="dc">QsysCameraConfig</param>
		public QsysCamera(ExternalControlProtocol.QsysEcpController dsp, string key, string name, QsysCameraConfig dc)
			: base(key, name)
		{
			_Dsp = dsp;
			Config = dc;
            IsOnline = new BoolFeedback(dsp.Key + "-" + key + "-IsOnline", () => Online);
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
                        var cmdToSend = string.Format("csv \"{0}\" 0", LastCmd);
						_Dsp.SendLine(cmdToSend);
						break;
					}
				case eCameraPtzControls.PanLeft: tag = Config.PanLeftTag; break;
				case eCameraPtzControls.PanRight: tag = Config.PanRightTag; break;
				case eCameraPtzControls.TiltUp: tag = Config.TiltUpTag; break;
				case eCameraPtzControls.TiltDown: tag = Config.TiltDownTag; break;
				case eCameraPtzControls.ZoomIn: tag = Config.ZoomInTag; break;
				case eCameraPtzControls.ZoomOut: tag = Config.ZoomOutTag; break;


			}
			if (tag != null)
			{
                var cmdToSend = string.Format("csv \"{0}\" 1", tag);
				LastCmd = tag;
				_Dsp.SendLine(cmdToSend);

			}
		}

		/// <summary>
		/// Camera privacy on
		/// </summary>
		public void PrivacyOn()
		{
            var cmdToSend = string.Format("csv \"{0}\" 1", Config.Privacy);
			_Dsp.SendLine(cmdToSend);
		}

		/// <summary>
		/// Camera privacy off
		/// </summary>
		public void PrivacyOff()
		{
            var cmdToSend = string.Format("csv \"{0}\" 0", Config.Privacy);
			_Dsp.SendLine(cmdToSend);
		}

		/// <summary>
		/// Recalls a preset with the provided number
		/// </summary>
		/// <param name="presetNumber">ushort</param>
		public void RecallPreset(ushort presetNumber)
		{
			this.LogVerbose("Recall Camera Preset {0}", presetNumber);
			if (Config.Presets.ElementAt(presetNumber).Value != null)
			{
				var preset = Config.Presets.ElementAt(presetNumber).Value;
				var cmdToSend = string.Format("ssl {0} {1} 0", preset.Bank, preset.Number);
				_Dsp.SendLine(cmdToSend);
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
				var cmdToSend = string.Format("sss {0} {1}", preset.Bank, preset.Number);
				_Dsp.SendLine(cmdToSend);
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
		/// Adds the command to the change group
		/// </summary>
		public void Subscribe()
		{
			try
			{
				// Do subscriptions and blah blah
				if (Config.OnlineStatus != null)
				{
                    var cmd = string.Format("cga 1 \"{0}\"", Config.OnlineStatus);
					_Dsp.SendLine(cmd);
				}
			}
			catch (Exception e)
			{
				this.LogVerbose(e, "QsysCamera Subscription Error");
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
			this.LogWarning("CameraOnline {0} Response: '{1}'", customName, value);

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