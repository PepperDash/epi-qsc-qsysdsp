using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro.DeviceSupport;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Devices;
using PepperDash.Essentials.Devices.Common.Codec;
using PepperDash.Essentials.Bridges;
using PepperDash.Essentials.Devices.Common.DSP;
using Newtonsoft.Json.Linq;
using PepperDash.Essentials.Core.Config;
using PepperDash.Essentials.Devices.Common;



namespace QSC.DSP.EPI
{
	public class QscDspCamera : Device, IBridge
	{
		QscDsp _Dsp;
		public QscDspCameraConfig Config{ get; private set;} 
		string LastCmd;
		
		public QscDspCamera(QscDsp dsp, string key, string name, QscDspCameraConfig dc)
			: base(key, name)
		{
			_Dsp = dsp;
			Config = dc;

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

		public void RecallPreset(ushort presetNumber)
		{
			Debug.Console(2, this, "Recall Camera Preset {0}", presetNumber);
			if (Config.Presets.ElementAt(presetNumber).Value != null)
			{
				var preset = Config.Presets.ElementAt(presetNumber - 1).Value;
				var cmdToSend = string.Format("ssl {0} {1} 0", preset.Bank, preset.number - 1);
				_Dsp.SendLine(cmdToSend);
			}
		}
		public void SavePreset(ushort presetNumber)
		{
			if (Config.Presets.ElementAt(presetNumber).Value != null)
			{
				var preset = Config.Presets.ElementAt(presetNumber - 1).Value;
				var cmdToSend = string.Format("sss {0} {1}", preset.Bank, preset.number - 1);
				_Dsp.SendLine(cmdToSend);
			}
		}
		public void WritePresetName(string newLabel, ushort presetNumber)
		{
			if (Config.Presets.ElementAt(presetNumber - 1).Value != null)
			{
				Config.Presets.ElementAt(presetNumber - 1).Value.label = newLabel;
				_Dsp.Config.Properties["CameraControlBlocks"][Key]["Presets"][Config.Presets.ElementAt(presetNumber - 1).Key]["label"] = newLabel;
				
				_Dsp.WriteConfig();
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