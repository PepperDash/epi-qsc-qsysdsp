using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro.DeviceSupport;
using PepperDash.Core;
using PepperDash.Essentials.Bridges;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Bridges;
using PepperDash.Essentials.Devices.Common.VideoCodec.Cisco;

namespace QscQsysDspPlugin
{
	/// <summary>
	/// QSC DSP Camera class
	/// </summary>
	public class QscQsysCamera : EssentialsBridgeableDevice
	{
		/// <summary>
		/// Parent DSP device
		/// </summary>
		public QscQsysDsp Parent;

		/// <summary>
		/// Camera configuation 
		/// </summary>
		public QscQsysCameraConfig Config { get; private set; }


		private bool _online;
		/// <summary>
		/// Online property
		/// </summary>
		public bool Online
		{
			get { return _online; }
			set
			{
				_online = value;
				OnlineFeedback.FireUpdate();
			}
		}

		/// <summary>
		/// Online feedback
		/// </summary>
		public BoolFeedback OnlineFeedback;


		private string _lastCmd;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="parent">Parent DSP</param>
		/// <param name="key">camera instance key</param>
		/// <param name="name">camera instance name</param>
		/// <param name="config">camera instance configuration</param>
		public QscQsysCamera(QscQsysDsp parent, string key, string name, QscQsysCameraConfig config)
			: base(key, name)
		{
			Parent = parent;
			Config = config;

			OnlineFeedback = new BoolFeedback(()=> Online);

			DeviceManager.AddDevice(this);
		}

		/// <summary>
		/// Moves a camera 
		/// </summary>
		/// <param name="button">eCameraPtzControls</param>
		public void MoveCamera(ECameraPtzControls button)
		{
			string tag = null;

			switch (button)
			{
				case ECameraPtzControls.Stop:
					{
						var cmdToSend = string.Format("csv {0} 0", _lastCmd);
						Parent.SendText(cmdToSend);
						break;
					}
				case ECameraPtzControls.PanLeft: tag = Config.PanLeftTag; break;
				case ECameraPtzControls.PanRight: tag = Config.PanRightTag; break;
				case ECameraPtzControls.TiltUp: tag = Config.TiltUpTag; break;
				case ECameraPtzControls.TiltDown: tag = Config.TiltDownTag; break;
				case ECameraPtzControls.ZoomIn: tag = Config.ZoomInTag; break;
				case ECameraPtzControls.ZoomOut: tag = Config.ZoomOutTag; break;


			}
			if (tag != null)
			{
				var cmdToSend = string.Format("csv {0} 1", tag);
				_lastCmd = tag;
				Parent.SendText(cmdToSend);

			}
		}

		/// <summary>
		/// Camera privacy on
		/// </summary>
		public void PrivacyOn()
		{
			var cmdToSend = string.Format("csv {0} 1", Config.Privacy);
			Parent.SendText(cmdToSend);
		}

		/// <summary>
		/// Camera privacy off
		/// </summary>
		public void PrivacyOff()
		{
			var cmdToSend = string.Format("csv {0} 0", Config.Privacy);
			Parent.SendText(cmdToSend);
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
				var cmdToSend = string.Format("ssl {0} {1} 0", preset.Bank, preset.Number);
				Parent.SendText(cmdToSend);
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
				Parent.SendText(cmdToSend);
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
				
				// [ ] TODO - Implement Camera preset label writing
				// Original code
				//_Dsp.Config.Properties["CameraControlBlocks"][Key]["Presets"][Config.Presets.ElementAt(presetNumber - 1).Key]["label"] = newLabel;
				//_Dsp.WriteConfig();

				// New based on changes to updated configuration object
				//Parent.Cameras[Key]["presets"][Config.Presets.ElementAt(presetNumber - 1).Key]["label"] = newLabel;
				//Parent.WriteConfig();
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
					var cmd = string.Format("cga {0} {1}", 1, Config.OnlineStatus);
					Parent.SendText(cmd);
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

		#region IBridge Members

		/// <summary>
		/// Link to API
		/// </summary>
		/// <param name="trilist"></param>
		/// <param name="joinStart"></param>
		/// <param name="joinMapKey"></param>
		//public override void LinkToApi(BasicTriList trilist, uint joinStart, string joinMapKey, EiscApiAdvanced)
		//{
		//    this.LinkToApiExt(trilist, joinStart, joinMapKey);
		//}

		/// <summary>
		/// Link to API
		/// </summary>
		/// <param name="trilist"></param>
		/// <param name="joinStart"></param>
		/// <param name="joinMapKey"></param>
		/// <param name="bridge"></param>
		public override void LinkToApi(BasicTriList trilist, uint joinStart, string joinMapKey, EiscApiAdvanced bridge)
		{
			var joinMap = new QscQsysCameraBridgeJoinMap(joinStart);

			if (bridge != null)
			{
				bridge.AddJoinMap(Key, joinMap);
			}

			var customJoins = JoinMapHelper.TryGetJoinMapAdvancedForDevice(joinMapKey);
			if (customJoins != null)
			{
				joinMap.SetCustomJoinData(customJoins);
			}

			Debug.Console(1, "Linking to Trilist '{0}'", trilist.ID.ToString("X"));
			Debug.Console(0, "Linking to Bridge Type '{0}'", GetType().Name);


		}

		#endregion
	}

	/// <summary>
	/// Camera PTZ controls enum
	/// </summary>
	public enum ECameraPtzControls
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