using System.Linq;
using Crestron.SimplSharp.Reflection;
using Crestron.SimplSharpPro.DeviceSupport;
using Newtonsoft.Json;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Bridges;

namespace QscQsysDspPlugin
{
	/// <summary>
	/// QSC DSP api extensions
	/// </summary>
	public static class QscDspDeviceApiExtensions
	{
		public static void LinkToApiExt(this QscDsp DspDevice, BasicTriList trilist, uint joinStart, string joinMapKey, EiscApiAdvanced bridge)
		{
            var joinMap = new QscDspDeviceJoinMapAdvanced(joinStart);
            var joinMapSerialized = JoinMapHelper.TryGetJoinMapAdvancedForDevice(joinMapKey); //as QscDspCameraDeviceJoinMap;

            if (joinMapSerialized != null)
                joinMap.SetCustomJoinData(joinMapSerialized);
            if (bridge != null)
            {
                bridge.AddJoinMap(DspDevice.Key, joinMap);
            }
            //if (joinMap == null)
            //    joinMap = new QscDspCameraDeviceJoinMap();
            Debug.Console(1, DspDevice, "Linking to Trilist '{0}'", trilist.ID.ToString("X"));
			ushort x = 1;
			var comm = DspDevice as ICommunicationMonitor;

			// from Plugin > to SiMPL
			DspDevice.IsOnline.LinkInputSig(trilist.BooleanInput[joinMap.IsOnline.JoinNumber]);
            trilist.StringInput[joinMap.DspName.JoinNumber].StringValue = DspDevice.DspName;

            DspDevice.IsPrimaryFeedback.LinkInputSig(trilist.BooleanInput[joinMap.IsPrimary.JoinNumber]);
            DspDevice.IsPrimaryFeedback.LinkComplementInputSig(trilist.BooleanInput[joinMap.IsSecondary.JoinNumber]);
            DspDevice.IsActiveFeedback.LinkInputSig(trilist.BooleanInput[joinMap.IsActive.JoinNumber]);
            DspDevice.IsActiveFeedback.LinkComplementInputSig(trilist.BooleanInput[joinMap.IsInactive.JoinNumber]);

			// from SiMPL > to Plugin
            trilist.SetStringSigAction(joinMap.Prefix.JoinNumber, DspDevice.SetPrefix);
            trilist.SetStringSigAction(joinMap.Address.JoinNumber, DspDevice.SetIpAddress);

            trilist.SetBoolSigAction(joinMap.GetStatus.JoinNumber, DspDevice.StatusGet);

            trilist.SetStringSigAction(joinMap.SimTxRx.JoinNumber, DspDevice.ProcessSimulatedRx);

			foreach (var channel in DspDevice.LevelControlPoints)
			{
				//var QscChannel = channel.Value as QSC.DSP.EPI.QscDspLevelControl;
				Debug.Console(2, "QscChannel {0} connect", x);

				var genericChannel = channel.Value as IBasicVolumeWithFeedback;
				if (channel.Value.Enabled)
				{
					// from SiMPL > to Plugin
                    trilist.StringInput[joinMap.ChannelName.JoinNumber + x].StringValue = channel.Value.LevelCustomName;
                    trilist.UShortInput[joinMap.ChannelType.JoinNumber + x].UShortValue = (ushort)channel.Value.Type;
                    trilist.BooleanInput[joinMap.ChannelVisible.JoinNumber + x].BoolValue = true;

					// from Plugin > to SiMPL
                    genericChannel.MuteFeedback.LinkInputSig(trilist.BooleanInput[joinMap.ChannelMuteToggle.JoinNumber + x]);
                    genericChannel.MuteFeedback.LinkComplementInputSig(trilist.BooleanInput[joinMap.ChannelMuteOff.JoinNumber + x]);
                    genericChannel.VolumeLevelFeedback.LinkInputSig(trilist.UShortInput[joinMap.ChannelVolume.JoinNumber + x]);

					// from SiMPL > to Plugin
                    trilist.SetSigTrueAction(joinMap.ChannelMuteToggle.JoinNumber + x, () => genericChannel.MuteToggle());
                    trilist.SetSigTrueAction(joinMap.ChannelMuteOn.JoinNumber + x, () => genericChannel.MuteOn());
                    trilist.SetSigTrueAction(joinMap.ChannelMuteOff.JoinNumber + x, () => genericChannel.MuteOff());
					// from SiMPL > to Plugin
                    trilist.SetBoolSigAction(joinMap.ChannelVolumeUp.JoinNumber + x, b => genericChannel.VolumeUp(b));
                    trilist.SetBoolSigAction(joinMap.ChannelVolumeDown.JoinNumber + x, b => genericChannel.VolumeDown(b));
					// from SiMPL > to Plugin
                    trilist.SetUShortSigAction(joinMap.ChannelVolume.JoinNumber + x, u => { if (u > 0) { genericChannel.SetVolume(u); } });
				}
				x++;
			}


			// Presets 
			x = 0;
			// from SiMPL > to Plugin
            trilist.SetStringSigAction(joinMap.Presets.JoinNumber, s => DspDevice.RunPreset(s));
			foreach (var preset in DspDevice.PresetList)
			{
				var temp = x;
                var presetNum = joinMap.Presets.JoinNumber + temp + 1;
				// from SiMPL > to Plugin
				trilist.StringInput[presetNum].StringValue = preset.Label;
				//trilist.SetSigTrueAction(presetNum, () => DspDevice.RunPresetNumber(temp));
				trilist.SetSigHeldAction(presetNum, 5000, () => DspDevice.RunPresetNumber(temp), () => DspDevice.SavePresetNumber(temp));
				x++;
			}

			// VoIP Dialer
			uint lineOffset = 0;
			foreach (var line in DspDevice.Dialers)
			{
				var dialer = line;

				var dialerLineOffset = lineOffset;
				Debug.Console(0, "AddingDialerBridge {0} {1} Offset", dialer.Key, dialerLineOffset);
				
				// from SiMPL > to Plugin
                trilist.SetSigTrueAction((joinMap.Keypad0.JoinNumber + dialerLineOffset), () => DspDevice.Dialers[dialer.Key].SendKeypad(QscDspDialer.EKeypadKeys.Num0));
                trilist.SetSigTrueAction((joinMap.Keypad1.JoinNumber + dialerLineOffset), () => dialer.Value.SendKeypad(QscDspDialer.EKeypadKeys.Num1));
                trilist.SetSigTrueAction((joinMap.Keypad2.JoinNumber + dialerLineOffset), () => dialer.Value.SendKeypad(QscDspDialer.EKeypadKeys.Num2));
                trilist.SetSigTrueAction((joinMap.Keypad3.JoinNumber + dialerLineOffset), () => dialer.Value.SendKeypad(QscDspDialer.EKeypadKeys.Num3));
                trilist.SetSigTrueAction((joinMap.Keypad4.JoinNumber + dialerLineOffset), () => dialer.Value.SendKeypad(QscDspDialer.EKeypadKeys.Num4));
                trilist.SetSigTrueAction((joinMap.Keypad5.JoinNumber + dialerLineOffset), () => dialer.Value.SendKeypad(QscDspDialer.EKeypadKeys.Num5));
                trilist.SetSigTrueAction((joinMap.Keypad6.JoinNumber + dialerLineOffset), () => dialer.Value.SendKeypad(QscDspDialer.EKeypadKeys.Num6));
                trilist.SetSigTrueAction((joinMap.Keypad7.JoinNumber + dialerLineOffset), () => dialer.Value.SendKeypad(QscDspDialer.EKeypadKeys.Num7));
                trilist.SetSigTrueAction((joinMap.Keypad8.JoinNumber + dialerLineOffset), () => dialer.Value.SendKeypad(QscDspDialer.EKeypadKeys.Num8));
                trilist.SetSigTrueAction((joinMap.Keypad9.JoinNumber + dialerLineOffset), () => dialer.Value.SendKeypad(QscDspDialer.EKeypadKeys.Num9));
                trilist.SetSigTrueAction((joinMap.KeypadStar.JoinNumber + dialerLineOffset), () => dialer.Value.SendKeypad(QscDspDialer.EKeypadKeys.Star));
                trilist.SetSigTrueAction((joinMap.KeypadPound.JoinNumber + dialerLineOffset), () => dialer.Value.SendKeypad(QscDspDialer.EKeypadKeys.Pound));
                trilist.SetSigTrueAction((joinMap.KeypadClear.JoinNumber + dialerLineOffset), () => dialer.Value.SendKeypad(QscDspDialer.EKeypadKeys.Clear));
                trilist.SetSigTrueAction((joinMap.KeypadBackspace.JoinNumber + dialerLineOffset), () => dialer.Value.SendKeypad(QscDspDialer.EKeypadKeys.Backspace));
				// from SiMPL > to Plugin
                trilist.SetSigTrueAction(joinMap.Dial.JoinNumber + dialerLineOffset, () => dialer.Value.Dial());
                trilist.SetStringSigAction(joinMap.DialStringCmd.JoinNumber + dialerLineOffset, dialer.Value.Dial);
                trilist.SetSigTrueAction(joinMap.DoNotDisturbToggle.JoinNumber + dialerLineOffset, () => dialer.Value.DoNotDisturbToggle());
                trilist.SetSigTrueAction(joinMap.DoNotDisturbOn.JoinNumber + dialerLineOffset, () => dialer.Value.DoNotDisturbOn());
                trilist.SetSigTrueAction(joinMap.DoNotDisturbOff.JoinNumber + dialerLineOffset, () => dialer.Value.DoNotDisturbOff());
                trilist.SetSigTrueAction(joinMap.AutoAnswerToggle.JoinNumber + dialerLineOffset, () => dialer.Value.AutoAnswerToggle());
                trilist.SetSigTrueAction(joinMap.AutoAnswerOn.JoinNumber + dialerLineOffset, () => dialer.Value.AutoAnswerOn());
                trilist.SetSigTrueAction(joinMap.AutoAnswerOff.JoinNumber + dialerLineOffset, () => dialer.Value.AutoAnswerOff());
                trilist.SetSigTrueAction(joinMap.EndCall.JoinNumber + dialerLineOffset, () => dialer.Value.EndAllCalls());
                trilist.SetSigTrueAction(joinMap.IncomingCallAccept.JoinNumber + dialerLineOffset, () => dialer.Value.AcceptCall());
                trilist.SetSigTrueAction(joinMap.IncomingCallReject.JoinNumber + dialerLineOffset, () => dialer.Value.RejectCall());

                // from SIMPL > to Plugin
                trilist.SetStringSigAction(joinMap.DialStringCmd.JoinNumber + dialerLineOffset, directDialString => dialer.Value.Dial(directDialString));

				// from Plugin > to SiMPL
                dialer.Value.DoNotDisturbFeedback.LinkInputSig(trilist.BooleanInput[joinMap.DoNotDisturbToggle.JoinNumber + dialerLineOffset]);
                dialer.Value.DoNotDisturbFeedback.LinkInputSig(trilist.BooleanInput[joinMap.DoNotDisturbOn.JoinNumber + dialerLineOffset]);
                dialer.Value.DoNotDisturbFeedback.LinkComplementInputSig(trilist.BooleanInput[joinMap.DoNotDisturbOff.JoinNumber + dialerLineOffset]);

				// from Plugin > to SiMPL
                dialer.Value.AutoAnswerFeedback.LinkInputSig(trilist.BooleanInput[joinMap.AutoAnswerToggle.JoinNumber + dialerLineOffset]);
                dialer.Value.AutoAnswerFeedback.LinkInputSig(trilist.BooleanInput[joinMap.AutoAnswerOn.JoinNumber + dialerLineOffset]);
                dialer.Value.AutoAnswerFeedback.LinkComplementInputSig(trilist.BooleanInput[joinMap.AutoAnswerOff.JoinNumber + dialerLineOffset]);
                dialer.Value.CallerIdNumberFeedback.LinkInputSig(trilist.StringInput[joinMap.CallerIdNumberFb.JoinNumber + dialerLineOffset]);

				// from Plugin > to SiMPL
                dialer.Value.OffHookFeedback.LinkInputSig(trilist.BooleanInput[joinMap.Dial.JoinNumber + dialerLineOffset]);
                dialer.Value.OffHookFeedback.LinkInputSig(trilist.BooleanInput[joinMap.OffHook.JoinNumber + dialerLineOffset]);
                dialer.Value.OffHookFeedback.LinkComplementInputSig(trilist.BooleanInput[joinMap.OnHook.JoinNumber + dialerLineOffset]);
                dialer.Value.DialStringFeedback.LinkInputSig(trilist.StringInput[joinMap.DialStringCmd.JoinNumber + dialerLineOffset]);

				// from Plugin > to SiMPL
                dialer.Value.IncomingCallFeedback.LinkInputSig(trilist.BooleanInput[joinMap.IncomingCall.JoinNumber + dialerLineOffset]);

				lineOffset = lineOffset + 50;
			}
		}
	}

	/// <summary>
	/// QSC DSP Join Map
	/// </summary>
	public class QscDspDeviceJoinMap : JoinMapBase
	{
		public uint IsOnline { get; set; }
        public uint IsPrimary { get; set; }
        public uint IsSecondary { get; set; }
        public uint IsActive { get; set; }
        public uint SimTxRx { get; set; }
        public uint IsInactive { get; set; }
        public uint GetStatus { get; set; }
        public uint DspName { get; set; }
		public uint Address { get; set; }
		public uint Prefix { get; set; }
		public uint ChannelMuteToggle { get; set; }
		public uint ChannelMuteOn { get; set; }
		public uint ChannelMuteOff { get; set; }
		public uint ChannelVolume { get; set; }
		public uint ChannelType { get; set; }
		public uint ChannelName { get; set; }
		public uint ChannelVolumeUp { get; set; }
		public uint ChannelVolumeDown { get; set; }
		public uint Presets { get; set; }
		public uint DialStringCmd { get; set; }
		public uint IncomingCall { get; set; }
		public uint Keypad0 { get; set; }
		public uint Keypad1 { get; set; }
		public uint Keypad2 { get; set; }
		public uint Keypad3 { get; set; }
		public uint Keypad4 { get; set; }
		public uint Keypad5 { get; set; }
		public uint Keypad6 { get; set; }
		public uint Keypad7 { get; set; }
		public uint Keypad8 { get; set; }
		public uint Keypad9 { get; set; }
		public uint KeypadStar { get; set; }
		public uint KeypadPound { get; set; }
		public uint KeypadClear { get; set; }
		public uint KeypadBackspace { get; set; }
		public uint Dial { get; set; }
		public uint DoNotDisturbToggle { get; set; }
		public uint DoNotDisturbOn { get; set; }
		public uint DoNotDisturbOff { get; set; }
		public uint AutoAnswerToggle { get; set; }
		public uint AutoAnswerOn { get; set; }
		public uint AutoAnswerOff { get; set; }
		public uint OffHook { get; set; }
		public uint OnHook { get; set; }
		public uint ChannelVisible { get; set; }
		public uint CallerIdNumberFb { get; set; }
		public uint EndCall { get; set; }
		public uint IncomingCallAccept { get; set; }
		public uint IncomingCallReject { get; set; }

		public QscDspDeviceJoinMap()
		{
			// Arrays
			ChannelName = 200;
			ChannelMuteToggle = 400;
			ChannelMuteOn = 600;
			ChannelMuteOff = 800;
			ChannelVolume = 200;
			ChannelVolumeUp = 1000;
			ChannelVolumeDown = 1200;
			ChannelType = 400;
			Presets = 100;
			ChannelVisible = 200;

			// SIngleJoins
			IsOnline = 1;
            IsPrimary = 2;
            IsSecondary = 3;
            IsActive = 4;
            IsInactive = 5;
            SimTxRx = 6;
            GetStatus = 2;
			Prefix = 2;
			Address = 1;
            DspName = 3;
			Presets = 100;
			DialStringCmd = 3100;
			IncomingCall = 3100;
			EndCall = 3107;
			Keypad0 = 3110;
			Keypad1 = 3111;
			Keypad2 = 3112;
			Keypad3 = 3113;
			Keypad4 = 3114;
			Keypad5 = 3115;
			Keypad6 = 3116;
			Keypad7 = 3117;
			Keypad8 = 3118;
			Keypad9 = 3119;
			KeypadStar = 3120;
			KeypadPound = 3121;
			KeypadClear = 3122;
			KeypadBackspace = 3123;
			DoNotDisturbToggle = 3132;
			DoNotDisturbOn = 3133;
			DoNotDisturbOff = 3134;
			AutoAnswerToggle = 3127;
			AutoAnswerOn = 3125;
			AutoAnswerOff = 3126;
			Dial = 3124;
			OffHook = 3130;
			OnHook = 3129;
			CallerIdNumberFb = 3104;
			IncomingCallAccept = 3136;
			IncomingCallReject = 3137;
		}

		public override void OffsetJoinNumbers(uint joinStart)
		{
			var joinOffset = joinStart - 1;
			var properties = this.GetType().GetCType().GetProperties().Where(o => o.PropertyType == typeof(uint)).ToList();
			foreach (var property in properties)
			{
				property.SetValue(this, (uint)property.GetValue(this, null) + joinOffset, null);
			}
		}
	}


    public class QscDspDeviceJoinMapAdvanced : JoinMapBaseAdvanced
    {
        [JoinName("ChannelName")]
        public JoinDataComplete ChannelName = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 201,
                JoinSpan = 200
            },
            new JoinMetadata
            {
                Description = "Fader Channel Name",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Serial
            });
        [JoinName("ChannelMuteToggle")]
        public JoinDataComplete ChannelMuteToggle = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 401,
                JoinSpan = 200
            },
            new JoinMetadata
            {
                Description = "Fader Channel Mute Toggle",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Digital
            });
        [JoinName("ChannelMuteOn")]
        public JoinDataComplete ChannelMuteOn = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 601,
                JoinSpan = 200
            },
            new JoinMetadata
            {
                Description = "Fader Channel Mute On Set/Get",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Digital
            });
        [JoinName("ChannelMuteOff")]
        public JoinDataComplete ChannelMuteOff = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 801,
                JoinSpan = 200
            },
            new JoinMetadata
            {
                Description = "Fader Channel Mute Off Set/Get",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Digital
            });
        [JoinName("ChannelVolume")]
        public JoinDataComplete ChannelVolume = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 201,
                JoinSpan = 200
            },
            new JoinMetadata
            {
                Description = "Fader Channel Volume Set / Get",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Analog
            });
        [JoinName("ChannelVolumeUp")]
        public JoinDataComplete ChannelVolumeUp = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 1001,
                JoinSpan = 200
            },
            new JoinMetadata
            {
                Description = "Fader Channel Volume Up",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });
        [JoinName("ChannelVolumeDown")]
        public JoinDataComplete ChannelVolumeDown = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 1201,
                JoinSpan = 200
            },
            new JoinMetadata
            {
                Description = "Fader Channel Volume Down",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });
        [JoinName("ChannelVisible")]
        public JoinDataComplete ChannelVisible = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 101,
                JoinSpan = 200
            },
            new JoinMetadata
            {
                Description = "Fader Channel Visible",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Digital
            });
        [JoinName("ChannelType")]
        public JoinDataComplete ChannelType = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 401,
                JoinSpan = 200
            },
            new JoinMetadata
            {
                Description = "Fader Channel Type",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Analog
            });
        [JoinName("Presets")]
        public JoinDataComplete Presets = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 101,
                JoinSpan = 200
            },
            new JoinMetadata
            {
                Description = "Trigger / Save presets and Preset Name",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.DigitalSerial
            });
        [JoinName("IsOnline")]
        public JoinDataComplete IsOnline = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 1,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Device Online",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Digital
            });
        [JoinName("IsPrimary")]
        public JoinDataComplete IsPrimary = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 2,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Device Is Primary DSP",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Digital
            });
        [JoinName("IsSecondary")]
        public JoinDataComplete IsSecondary = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 3,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Device is secondary DSP",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Digital
            });
        [JoinName("IsActive")]
        public JoinDataComplete IsActive = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 4,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Device is Active",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Digital
            });
        [JoinName("IsInactive")]
        public JoinDataComplete IsInactive = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 5,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Device is Inactive",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Digital
            });
        [JoinName("SimTxRx")]
        public JoinDataComplete SimTxRx = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 6,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Device communication stream",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Serial
            });
        [JoinName("GetStatus")]
        public JoinDataComplete GetStatus = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 2,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Poll Device Status",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });
        [JoinName("Prefix")]
        public JoinDataComplete Prefix = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 2,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Set Device Prefix",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Serial
            });
        [JoinName("Address")]
        public JoinDataComplete Address = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 1,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Set Device Address",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Serial
            });
        [JoinName("DspName")]
        public JoinDataComplete DspName = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 3,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Device Name",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Serial
            });

        [JoinName("DialStringCmd")]
        public JoinDataComplete DialStringCmd = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 3100,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "DialString Get/Set",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Serial
            });
        [JoinName("IncomingCall")]
        public JoinDataComplete IncomingCall = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 3101,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Incomig Call",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Digital
            });
        [JoinName("EndCall")]
        public JoinDataComplete EndCall = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 3107,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "End Call",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("Keypad0")]
        public JoinDataComplete Keypad0 = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 3110,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Keypad 0",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });
        [JoinName("Keypad1")]
        public JoinDataComplete Keypad1 = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 3111,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Keypad 1",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });
        [JoinName("Keypad2")]
        public JoinDataComplete Keypad2 = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 3112,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Keypad 2",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });
        [JoinName("Keypad3")]
        public JoinDataComplete Keypad3 = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 3113,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Keypad 3",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });
        [JoinName("Keypad4")]
        public JoinDataComplete Keypad4 = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 3114,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Keypad 4",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });
        [JoinName("Keypad5")]
        public JoinDataComplete Keypad5 = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 3115,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Keypad 5",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });
        [JoinName("Keypad6")]
        public JoinDataComplete Keypad6 = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 3116,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Keypad 6",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });
        [JoinName("Keypad7")]
        public JoinDataComplete Keypad7 = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 3117,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Keypad 7",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });
        [JoinName("Keypad8")]
        public JoinDataComplete Keypad8 = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 3118,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Keypad 8",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });
        [JoinName("Keypad9")]
        public JoinDataComplete Keypad9 = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 3119,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Keypad 9",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });
        [JoinName("KeypadStar")]
        public JoinDataComplete KeypadStar = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 3120,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Keypad Star",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });
        [JoinName("KeypadPound")]
        public JoinDataComplete KeypadPound = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 3121,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Keypad Pound",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });
        [JoinName("KeypadClear")]
        public JoinDataComplete KeypadClear = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 3122,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Keypad Clear",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });
        [JoinName("KeypadBackspace")]
        public JoinDataComplete KeypadBackspace = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 3123,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Keypad Backspace",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });
        [JoinName("DoNotDisturbToggle")]
        public JoinDataComplete DoNotDisturbToggle = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 3132,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Toggle Do Not Disturb",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Digital
            });
        [JoinName("DoNotDisturbOn")]
        public JoinDataComplete DoNotDisturbOn = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 3133,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Set/Get Do Not Disturb On",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Digital
            });
        [JoinName("DoNotDisturbOff")]
        public JoinDataComplete DoNotDisturbOff = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 3134,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Set/Get Do Not Disturb Off",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Digital
            });
        [JoinName("AutoAnswerToggle")]
        public JoinDataComplete AutoAnswerToggle = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 3127,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Toggle AutoAnswer",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Digital
            });
        [JoinName("AutoAnswerOn")]
        public JoinDataComplete AutoAnswerOn = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 3125,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Set/Get Auto Answer",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Digital
            });
        [JoinName("AutoAnswerOff")]
        public JoinDataComplete AutoAnswerOff = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 3134,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Set/Get Do Not Disturb Off",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Digital
            });
        [JoinName("Dial")]
        public JoinDataComplete Dial = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 3124,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Dial Call",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });
        [JoinName("OffHook")]
        public JoinDataComplete OffHook = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 3130,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Line is 'Off Hook'",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });
        [JoinName("OnHook")]
        public JoinDataComplete OnHook = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 3129,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Line is 'On Hook'",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });
        [JoinName("IncomingCallAccept")]
        public JoinDataComplete IncomingCallAccept = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 3136,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Accept Incoming Call",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });
        [JoinName("IncomingCallReject")]
        public JoinDataComplete IncomingCallReject = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 3137,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Reject Incoming Call",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });
        [JoinName("CallerIdNumberFb")]
        public JoinDataComplete CallerIdNumberFb = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 3104,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Caller ID Number",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Serial
            });



        public QscDspDeviceJoinMapAdvanced(uint joinStart) : base(joinStart, typeof(QscDspDeviceJoinMapAdvanced))
        {
        }
    }
}