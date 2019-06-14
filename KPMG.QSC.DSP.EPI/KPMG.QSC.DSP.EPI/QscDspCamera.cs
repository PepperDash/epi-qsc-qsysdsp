using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Devices;
using PepperDash.Essentials.Devices.Common.Codec;

using PepperDash.Essentials.Devices.Common.DSP;
using Newtonsoft.Json;
using PepperDash.Essentials.Core.Config;

namespace QSC.DSP.EPI
{
	public class QscDspCamera : Device
	{
		QscDsp _Dsp;
		QscDspCameraConfig _Config;
		public QscDspCamera(QscDsp dsp, string key, string name, DeviceConfig dc) : base(key, name)
		{
			_Dsp = dsp;
			DeviceManager.AddDevice(this);
		}
		public void PanLeft();
		public void PanRight();
		public void TiltDown();
		public void TiltUp();
		public void ZoomIn();
		public void ZoomOut();
		public void Stop();
		public void RecallPreset ();
		public void StorePreset();
		public void MoveCamera(eCameraPtzControls button)
		{
			string tag = null;
			switch (button)
			{
				case eCameraPtzControls.Stop: tag = _Config. break;
				case eKeypadKeys.Num1: keypadTag = Tags.keypad1Tag; break;
				case eKeypadKeys.Num2: keypadTag = Tags.keypad2Tag; break;
				case eKeypadKeys.Num3: keypadTag = Tags.keypad3Tag; break;
				case eKeypadKeys.Num4: keypadTag = Tags.keypad4Tag; break;
				case eKeypadKeys.Num5: keypadTag = Tags.keypad5Tag; break;
				case eKeypadKeys.Num6: keypadTag = Tags.keypad6Tag; break;
				case eKeypadKeys.Num7: keypadTag = Tags.keypad7Tag; break;
				case eKeypadKeys.Num8: keypadTag = Tags.keypad8Tag; break;
				case eKeypadKeys.Num9: keypadTag = Tags.keypad9Tag; break;
				case eKeypadKeys.Pound: keypadTag = Tags.keypadPoundTag; break;
				case eKeypadKeys.Star: keypadTag = Tags.keypadStarTag; break;
				case eKeypadKeys.Backspace: keypadTag = Tags.keypadBackspaceTag; break;
				case eKeypadKeys.Clear: keypadTag = Tags.keypadClearTag; break;
			}
			if (keypadTag != null)
			{
				var cmdToSend = string.Format("ct {0}", keypadTag);
				Parent.SendLine(cmdToSend);
				PollKeypad();
			}
		}
	}
	enum eCameraPtzControls
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