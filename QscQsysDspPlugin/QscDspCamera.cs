using System;
using System.Collections.Generic;
using System.Linq;
using Crestron.SimplSharpPro.DeviceSupport;
using PepperDash.Core;
using PepperDash.Essentials.Core.Bridges;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Devices.Common.Cameras;

namespace QscQsysDspPlugin
{
	/// <summary>
	/// QSC DSP Camera class
	/// </summary>
    public class QscDspCamera : Device, ICommunicationMonitor, IBridgeAdvanced, IOnline, IHasCameraPtzControl, IHasCameraPresets
	{
	    readonly QscDsp _dsp;
		public QscDspCameraConfig Config { get; private set; }
		string _lastCmd;
		private bool _online;
	    public StatusMonitorBase CommunicationMonitor { get; private set; }
		public bool Online
		{
			set
			{
				_online = value;
				IsOnline.FireUpdate();
			}
			get
			{
				return _online;
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
			_dsp = dsp;
			Config = dc;
            IsOnline = new BoolFeedback(() => Online);
			DeviceManager.AddDevice(this);

		    CommunicationMonitor = _dsp.CommunicationMonitor;

		}

		/// <summary>
		/// Moves a camera 
		/// </summary>
		/// <param name="button">eCameraPtzControls</param>
		public void MoveCamera(eCameraPtzControls button)
		{
			string tag = null;

		    string cmdToSend;

			switch (button)
			{
				case eCameraPtzControls.Stop:
					{
                        cmdToSend = string.Format("csv \"{0}\" 0", _lastCmd);
						_dsp.SendLine(cmdToSend);
						break;
					}
				case eCameraPtzControls.PanLeft: tag = Config.PanLeftTag; break;
				case eCameraPtzControls.PanRight: tag = Config.PanRightTag; break;
				case eCameraPtzControls.TiltUp: tag = Config.TiltUpTag; break;
				case eCameraPtzControls.TiltDown: tag = Config.TiltDownTag; break;
				case eCameraPtzControls.ZoomIn: tag = Config.ZoomInTag; break;
				case eCameraPtzControls.ZoomOut: tag = Config.ZoomOutTag; break;
                case eCameraPtzControls.Home:
			        tag = Config.HomeTag;
			        break;


			}
		    if (tag == null) return;
		    cmdToSend = string.Format("csv \"{0}\" 1", tag);
		    _lastCmd = tag;
		    _dsp.SendLine(cmdToSend);
		}

		/// <summary>
		/// Camera privacy on
		/// </summary>
		public void PrivacyOn()
		{
            var cmdToSend = string.Format("csv \"{0}\" 1", Config.Privacy);
			_dsp.SendLine(cmdToSend);
		}

		/// <summary>
		/// Camera privacy off
		/// </summary>
		public void PrivacyOff()
		{
            var cmdToSend = string.Format("csv \"{0}\" 0", Config.Privacy);
			_dsp.SendLine(cmdToSend);
		}

		/// <summary>
		/// Recalls a preset with the provided number
		/// </summary>
		/// <param name="presetNumber">ushort</param>
		public void RecallPreset(ushort presetNumber)
		{
			Debug.Console(2, this, "Recall Camera Preset {0}", presetNumber);
		    if (Config.Presets.ElementAt(presetNumber).Value == null) return;
		    var preset = Config.Presets.ElementAt(presetNumber).Value;
		    var cmdToSend = string.Format("ssl {0} {1} 0", preset.Bank, preset.Number);
		    _dsp.SendLine(cmdToSend);
		}

		/// <summary>
		/// Saves a preset with the provided number
		/// </summary>
		/// <param name="presetNumber">ushort</param>
		public void SavePreset(ushort presetNumber)
		{
		    if (Config.Presets.ElementAt(presetNumber).Value == null) return;
		    var preset = Config.Presets.ElementAt(presetNumber).Value;
		    var cmdToSend = string.Format("sss {0} {1}", preset.Bank, preset.Number);
		    _dsp.SendLine(cmdToSend);
		}

		/// <summary>
		/// Writes the preset name
		/// </summary>
		/// <param name="newLabel">string</param>
		/// <param name="presetNumber">ushort</param>
		public void WritePresetName(string newLabel, ushort presetNumber)
		{
		    if (Config.Presets.ElementAt(presetNumber - 1).Value == null || newLabel.Length <= 0 ||
		        Config.Presets.ElementAt(presetNumber - 1).Value.Label == newLabel) return;
		    Config.Presets.ElementAt(presetNumber - 1).Value.Label = newLabel;
		    _dsp.Config.Properties["CameraControlBlocks"][Key]["Presets"][Config.Presets.ElementAt(presetNumber - 1).Key]["label"] = newLabel;

		    _dsp.WriteConfig();
		    OnPresetsListHasChanged();
		}

		/// <summary>
		/// Adds the command to the change group
		/// </summary>
		public void Subscribe()
		{
			try
			{
				// Do subscriptions and blah blah
			    if (Config.OnlineStatus == null) return;
			    var cmd = string.Format("cga 1 \"{0}\"", Config.OnlineStatus);
			    _dsp.SendLine(cmd);
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

		    switch (value)
		    {
		        case "true":
		            Online = true;
		            break;
		        case "false":
		            Online = false;
		            break;
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

        #region IHasCameraPtzControl Members

        public void PositionHome()
        {
            throw new NotImplementedException();
        }

        #endregion

        #region IHasCameraPanControl Members

        public void PanLeft()
        {
            MoveCamera(eCameraPtzControls.PanLeft);
        }

        public void PanRight()
        {
            MoveCamera(eCameraPtzControls.PanRight);
        }

        public void PanStop()
        {
            MoveCamera(eCameraPtzControls.Stop);
        }

        #endregion

        #region IHasCameraTiltControl Members

        public void TiltDown()
        {
            MoveCamera(eCameraPtzControls.TiltDown);
        }

        public void TiltStop()
        {
            MoveCamera(eCameraPtzControls.Stop);
        }

        public void TiltUp()
        {
            MoveCamera(eCameraPtzControls.TiltUp);
        }

        #endregion

        #region IHasCameraZoomControl Members

        public void ZoomIn()
        {
            MoveCamera(eCameraPtzControls.ZoomIn);
        }

        public void ZoomOut()
        {
            MoveCamera(eCameraPtzControls.ZoomOut);
        }

        public void ZoomStop()
        {
            MoveCamera(eCameraPtzControls.Stop);
        }

        #endregion

        #region IHasCameraPresets Members

        public void PresetSelect(int preset)
        {
            RecallPreset((ushort)preset);
        }

        public void PresetStore(int preset, string description)
        {
            SavePreset((ushort) preset);
            WritePresetName(description, (ushort) preset);
        }

        public List<CameraPreset> Presets
        {

            get { return GetPresets(); }
        }

	    private List<CameraPreset> GetPresets()
	    {
	        var presets = new List<CameraPreset>();
	        var iterator = 1;
	        foreach (var preset in Config.Presets)
	        {
	            var thisPreset = preset.Value;
                var cameraPreset = new CameraPreset(iterator, thisPreset.Label, true, true);
                presets.Add(cameraPreset);
	            iterator++;

	        }
	        return presets;
	    }

        public event EventHandler<EventArgs> PresetsListHasChanged;

        private void OnPresetsListHasChanged()
	    {
	        var handler = PresetsListHasChanged;
            if(handler != null)
                handler.Invoke(this, EventArgs.Empty);
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
		ZoomOut,
        Home
	}
}