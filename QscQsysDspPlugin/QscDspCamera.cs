using System;
using System.Linq;
using Crestron.SimplSharpPro.DeviceSupport;
using PepperDash.Core;
using PepperDash.Essentials.Bridges;
using PepperDash.Essentials.Core;
using QscQsysDspPlugin;

namespace QscQsysDsp
{
	public class QscDspCamera : Device, IBridge
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
				OnlineFeedback.FireUpdate();
			}
			get
			{
				return this._Online;
			}
		}
		public BoolFeedback OnlineFeedback;

		public QscDspCamera(QscDsp dsp, string key, string name, QscDspCameraConfig dc)
			: base(key, name)
		{
			_Dsp = dsp;
			Config = dc;
			OnlineFeedback = new BoolFeedback(() => { return Online; });
			DeviceManager.AddDevice(this);

		}

		public void MoveCamera(eCameraPtzControls button)
		{
			string tag = null;

			switch (button)
			{
				case eCameraPtzControls.Stop:
					{
						var cmdToSend = string.Format("csv {0} 0", LastCmd);
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
				var cmdToSend = string.Format("csv {0} 1", tag);
				LastCmd = tag;
				_Dsp.SendLine(cmdToSend);

			}
		}
		public void PrivacyOn()
		{
			var cmdToSend = string.Format("csv {0} 1", Config.Privacy);
			_Dsp.SendLine(cmdToSend);
		}
		public void PrivacyOff()
		{
			var cmdToSend = string.Format("csv {0} 0", Config.Privacy);
			_Dsp.SendLine(cmdToSend);
		}

		public void RecallPreset(ushort presetNumber)
		{
			Debug.Console(2, this, "Recall Camera Preset {0}", presetNumber);
			if (Config.Presets.ElementAt(presetNumber).Value != null)
			{
				var preset = Config.Presets.ElementAt(presetNumber).Value;
				var cmdToSend = string.Format("ssl {0} {1} 0", preset.Bank, preset.number);
				_Dsp.SendLine(cmdToSend);
			}
		}
		public void SavePreset(ushort presetNumber)
		{
			if (Config.Presets.ElementAt(presetNumber).Value != null)
			{
				var preset = Config.Presets.ElementAt(presetNumber).Value;
				var cmdToSend = string.Format("sss {0} {1}", preset.Bank, preset.number);
				_Dsp.SendLine(cmdToSend);
			}
		}
		public void WritePresetName(string newLabel, ushort presetNumber)
		{
			if (Config.Presets.ElementAt(presetNumber - 1).Value != null && newLabel.Length > 0 && Config.Presets.ElementAt(presetNumber - 1).Value.label != newLabel)
			{
				Config.Presets.ElementAt(presetNumber - 1).Value.label = newLabel;
				_Dsp.Config.Properties["CameraControlBlocks"][Key]["Presets"][Config.Presets.ElementAt(presetNumber - 1).Key]["label"] = newLabel;

				_Dsp.WriteConfig();
			}

		}
		public void Subscribe()
		{
			try
			{
				// Do subscriptions and blah blah
				if (Config.OnlineStatus != null)
				{
					var cmd = string.Format("cga {0} {1}", 1, Config.OnlineStatus);
					_Dsp.SendLine(cmd);
				}

			}
			catch (Exception e)
			{

				Debug.Console(2, "QscDspCamera Subscription Error: '{0}'\n", e);
			}
		}
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

		public void LinkToApi(BasicTriList trilist, uint joinStart, string joinMapKey)
		{
			this.LinkToApiExt(trilist, joinStart, joinMapKey);
		}

		#endregion


	}
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