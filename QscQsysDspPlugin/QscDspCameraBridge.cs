using Crestron.SimplSharpPro.DeviceSupport;
using Newtonsoft.Json;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Bridges;

namespace QscQsysDspPlugin
{
	/// <summary>
	/// QSC DSP Camera api extensions
	/// </summary>
	public static class QscDspCameraDeviceApiExtensions
	{
        public static void LinkToApiExt(this QscDspCamera camera, BasicTriList trilist, uint joinStart, string joinMapKey, EiscApiAdvanced bridge)
        {
            var joinMap = new QscDspCameraDeviceJoinMapAdvanced(joinStart);
            var joinMapSerialized = JoinMapHelper.TryGetJoinMapAdvancedForDevice(joinMapKey); //as QscDspCameraDeviceJoinMap;

            if (joinMapSerialized != null)
                   joinMap.SetCustomJoinData(joinMapSerialized);
            if (bridge != null)
            {
                bridge.AddJoinMap(camera.Key, joinMap);
            }
            //if (joinMap == null)
            //    joinMap = new QscDspCameraDeviceJoinMap();

            Debug.Console(1, camera, "Linking to Trilist '{0}'", trilist.ID.ToString("X"));

            // from Plugin > to SiMPL
            camera.IsOnline.LinkInputSig(trilist.BooleanInput[joinMap.Online.JoinNumber]);

            // from SiMPL > to Plugin
            // ternary: camera.MoveCamera(bool ? [bool == true, method to execute] : [bool == false, method to execute])
            trilist.SetBoolSigAction(joinMap.Up.JoinNumber, (b) => camera.MoveCamera(b ? eCameraPtzControls.TiltUp : eCameraPtzControls.Stop));
            trilist.SetBoolSigAction(joinMap.Down.JoinNumber, (b) => camera.MoveCamera(b ? eCameraPtzControls.TiltDown : eCameraPtzControls.Stop));
            trilist.SetBoolSigAction(joinMap.Left.JoinNumber, (b) => camera.MoveCamera(b ? eCameraPtzControls.PanLeft : eCameraPtzControls.Stop));
            trilist.SetBoolSigAction(joinMap.Right.JoinNumber, (b) => camera.MoveCamera(b ? eCameraPtzControls.PanRight : eCameraPtzControls.Stop));
            trilist.SetBoolSigAction(joinMap.ZoomIn.JoinNumber, (b) => camera.MoveCamera(b ? eCameraPtzControls.ZoomIn : eCameraPtzControls.Stop));
            trilist.SetBoolSigAction(joinMap.ZoomOut.JoinNumber, (b) => camera.MoveCamera(b ? eCameraPtzControls.ZoomOut : eCameraPtzControls.Stop));

            ushort x = 0;
            foreach (var preset in camera.Config.Presets)
            {
                var temp = x;
                // from SiMPL > to Plugin
                trilist.SetSigTrueAction(joinMap.PresetRecallStart.JoinNumber + temp + 1, () => camera.RecallPreset(temp));
                trilist.SetSigTrueAction(joinMap.PresetStoreStart.JoinNumber + temp + 1, () => camera.SavePreset(temp));
                trilist.SetStringSigAction(joinMap.PresetNamesStart.JoinNumber + temp, (s) => camera.WritePresetName(s, (ushort)(temp + 1)));
                // from Plugin > to SiMPL
                preset.Value.LabelFeedback.LinkInputSig(trilist.StringInput[joinMap.PresetNamesStart.JoinNumber + temp + 1]);
                trilist.SetString(joinMap.PresetNamesStart.JoinNumber + temp + 1, preset.Value.Label);
                x++;
            }

            // from SiMPL > to Plugin
            trilist.SetSigTrueAction(joinMap.PrivacyOn.JoinNumber, camera.PrivacyOn);
            trilist.SetSigTrueAction(joinMap.PrivacyOff.JoinNumber, camera.PrivacyOff);

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

    public class QscDspCameraDeviceJoinMapAdvanced : JoinMapBaseAdvanced
    {
        [JoinName("Up")]
        public JoinDataComplete Up = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 1,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Tilt Camera Up",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("Down")]
        public JoinDataComplete Down = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 2,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Tilt Camera Down",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("Left")]
        public JoinDataComplete Left = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 3,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Pan Camera Left",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("Right")]
        public JoinDataComplete Right = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 4,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Pan Camera Right",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });
        [JoinName("ZoomIn")]
        public JoinDataComplete ZoomIn = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 5,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Zoom Camera In",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });
        [JoinName("ZoomOut")]
        public JoinDataComplete ZoomOut = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 6,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Zoom Camera Out",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });
        [JoinName("Online")]
        public JoinDataComplete Online = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 9,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Camera Online",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Digital
            });
        [JoinName("PrivacyOn")]
        public JoinDataComplete PrivacyOn = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 48,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Camera Privacy On Get/Set",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Digital
            });
        [JoinName("PrivacyOff")]
        public JoinDataComplete PrivacyOff = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 49,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Camera Privacy Off Get/Set",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Digital
            });
        [JoinName("PresetRecallStart")]
        public JoinDataComplete PresetRecallStart = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 10,
                JoinSpan = 20
            },
            new JoinMetadata
            {
                Description = "Preset Recall",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });
        [JoinName("PresetStoreStart")]
        public JoinDataComplete PresetStoreStart = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 30,
                JoinSpan = 20
            },
            new JoinMetadata
            {
                Description = "Preset Store",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });
        [JoinName("PresetNamesStart")]
        public JoinDataComplete PresetNamesStart = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 10,
                JoinSpan = 20
            },
            new JoinMetadata
            {
                Description = "Preset Names",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Serial
            });


        public QscDspCameraDeviceJoinMapAdvanced(uint joinStart)
            : base(joinStart, typeof(QscDspCameraDeviceJoinMapAdvanced))
        {
        }

    }
}