using Crestron.SimplSharpPro.DeviceSupport;
using Newtonsoft.Json;
using PepperDash.Core;
using PepperDash.Essentials.Core;

namespace QscQsysDspPlugin
{
	/// <summary>
	/// QSC DSP Camera api extensions
	/// </summary>
	public static class QscDspCameraDeviceApiExtensions
	{
		public static void LinkToApiExt(this QscDspCamera camera, BasicTriList trilist, uint joinStart, string joinMapKey)
		{
			var joinMap = new QscDspCameraDeviceJoinMap();
			var joinMapSerialized = JoinMapHelper.GetJoinMapForDevice(joinMapKey); //as QscDspCameraDeviceJoinMap;

			if (!string.IsNullOrEmpty(joinMapSerialized))
				joinMap = JsonConvert.DeserializeObject<QscDspCameraDeviceJoinMap>(joinMapSerialized);

			//if (joinMap == null)
			//    joinMap = new QscDspCameraDeviceJoinMap();

			joinMap.OffsetJoinNumbers(joinStart);
			Debug.Console(1, camera, "Linking to Trilist '{0}'", trilist.ID.ToString("X"));

			// from Plugin > to SiMPL
			camera.IsOnline.LinkInputSig(trilist.BooleanInput[joinMap.Online]);

			// from SiMPL > to Plugin
			// ternary: camera.MoveCamera(bool ? [bool == true, method to execute] : [bool == false, method to execute])
			trilist.SetBoolSigAction(joinMap.Up, (b) => camera.MoveCamera(b ? eCameraPtzControls.TiltUp : eCameraPtzControls.Stop));
			trilist.SetBoolSigAction(joinMap.Down, (b) => camera.MoveCamera(b ? eCameraPtzControls.TiltDown : eCameraPtzControls.Stop));
			trilist.SetBoolSigAction(joinMap.Left, (b) => camera.MoveCamera(b ? eCameraPtzControls.PanLeft : eCameraPtzControls.Stop));
			trilist.SetBoolSigAction(joinMap.Right, (b) => camera.MoveCamera(b ? eCameraPtzControls.PanRight : eCameraPtzControls.Stop));
			trilist.SetBoolSigAction(joinMap.ZoomIn, (b) => camera.MoveCamera(b ? eCameraPtzControls.ZoomIn : eCameraPtzControls.Stop));
			trilist.SetBoolSigAction(joinMap.ZoomOut, (b) => camera.MoveCamera(b ? eCameraPtzControls.ZoomOut : eCameraPtzControls.Stop));
			
			ushort x = 0;
			foreach (var preset in camera.Config.Presets)
			{
				var temp = x;
				// from SiMPL > to Plugin
				trilist.SetSigTrueAction(joinMap.PresetRecallStart + temp + 1, () => camera.RecallPreset(temp));
				trilist.SetSigTrueAction(joinMap.PresetStoreStart + temp + 1, () => camera.SavePreset(temp));
				trilist.SetStringSigAction(joinMap.PresetNamesStart + temp, (s) => camera.WritePresetName(s, (ushort)(temp + 1)));
				// from Plugin > to SiMPL
				preset.Value.LabelFeedback.LinkInputSig(trilist.StringInput[joinMap.PresetNamesStart + temp]);
				
				x++;
			}

			// from SiMPL > to Plugin
			trilist.SetSigTrueAction(joinMap.PrivacyOn, () => camera.PrivacyOn());
			trilist.SetSigTrueAction(joinMap.PrivacyOff, () => camera.PrivacyOff());

		}
	}

	/// <summary>
	/// QSC DSP Camera control join map
	/// </summary>
	public class QscDspCameraDeviceJoinMap : JoinMapBase
	{

		public uint Up { get; set; }
		public uint Down { get; set; }
		public uint Left { get; set; }
		public uint Right { get; set; }
		public uint ZoomIn { get; set; }
		public uint ZoomOut { get; set; }
		public uint Online { get; set; }
		public uint PresetRecallStart { get; set; }
		public uint PresetStoreStart { get; set; }
		public uint PresetNamesStart { get; set; }
		public uint PrivacyOn { get; set; }
		public uint PrivacyOff { get; set; }

		public QscDspCameraDeviceJoinMap()
		{
			// Arrays
			Up = 1;
			Down = 2;
			Left = 3;
			Right = 4;
			ZoomIn = 5;
			ZoomOut = 6;
			Online = 9;
			PresetRecallStart = 10;
			PresetStoreStart = 30;
			PresetNamesStart = 2;
			PrivacyOn = 48;
			PrivacyOff = 49;
		}

		public override void OffsetJoinNumbers(uint joinStart)
		{
			var joinOffset = joinStart - 1;
			Up = Up + joinOffset;
			Down = Down + joinOffset;
			Left = Left + joinOffset;
			Right = Right + joinOffset;
			ZoomIn = ZoomIn + joinOffset;
			PresetNamesStart = PresetNamesStart + joinOffset;
			ZoomOut = ZoomOut + joinOffset;
			PresetRecallStart = PresetRecallStart + joinOffset;
			PresetStoreStart = PresetStoreStart + joinOffset;
			PrivacyOn = PrivacyOn + joinOffset;
			PrivacyOff = PrivacyOff + joinOffset;
			Online = Online + joinOffset;
		}
	}
}