using System.Linq;
using Crestron.SimplSharp.Reflection;
using Crestron.SimplSharpPro.DeviceSupport;
using Newtonsoft.Json;
using PepperDash.Core;
using PepperDash.Essentials.Core;

namespace QscQsysDspPlugin
{
	/// <summary>
	/// QSC DSP api extensions
	/// </summary>
	public static class QscDspDeviceApiExtensions
	{
		public static void LinkToApiExt(this QscDsp DspDevice, BasicTriList trilist, uint joinStart, string joinMapKey)
		{
			var joinMap = new QscDspDeviceJoinMap();

			var joinMapSerialized = JoinMapHelper.GetJoinMapForDevice(joinMapKey);

			if (!string.IsNullOrEmpty(joinMapSerialized))
				joinMap = JsonConvert.DeserializeObject<QscDspDeviceJoinMap>(joinMapSerialized);

			joinMap.OffsetJoinNumbers(joinStart);
			Debug.Console(1, DspDevice, "Linking to Trilist '{0}'", trilist.ID.ToString("X"));
			ushort x = 1;
			var comm = DspDevice as ICommunicationMonitor;

			// from Plugin > to SiMPL
			DspDevice.CommunicationMonitor.IsOnlineFeedback.LinkInputSig(trilist.BooleanInput[joinMap.IsOnline]);

			// from SiMPL > to Plugin
			trilist.SetStringSigAction(joinMap.Prefix, (s) => { DspDevice.SetPrefix(s); });
			trilist.SetStringSigAction(joinMap.Address, (s) => { DspDevice.SetIpAddress(s); });

			foreach (var channel in DspDevice.LevelControlPoints)
			{
				//var QscChannel = channel.Value as QSC.DSP.EPI.QscDspLevelControl;
				Debug.Console(2, "QscChannel {0} connect", x);

				var genericChannel = channel.Value as IBasicVolumeWithFeedback;
				if (channel.Value.Enabled)
				{
					// from SiMPL > to Plugin
					trilist.StringInput[joinMap.ChannelName + x].StringValue = channel.Value.LevelCustomName;
					trilist.UShortInput[joinMap.ChannelType + x].UShortValue = (ushort)channel.Value.Type;
					trilist.BooleanInput[joinMap.ChannelVisible + x].BoolValue = true;

					// from Plugin > to SiMPL
					genericChannel.MuteFeedback.LinkInputSig(trilist.BooleanInput[joinMap.ChannelMuteToggle + x]);
					genericChannel.VolumeLevelFeedback.LinkInputSig(trilist.UShortInput[joinMap.ChannelVolume + x]);

					// from SiMPL > to Plugin
					trilist.SetSigTrueAction(joinMap.ChannelMuteToggle + x, () => genericChannel.MuteToggle());
					trilist.SetSigTrueAction(joinMap.ChannelMuteOn + x, () => genericChannel.MuteOn());
					trilist.SetSigTrueAction(joinMap.ChannelMuteOff + x, () => genericChannel.MuteOff());
					// from SiMPL > to Plugin
					trilist.SetBoolSigAction(joinMap.ChannelVolumeUp + x, b => genericChannel.VolumeUp(b));
					trilist.SetBoolSigAction(joinMap.ChannelVolumeDown + x, b => genericChannel.VolumeDown(b));
					// from SiMPL > to Plugin
					trilist.SetUShortSigAction(joinMap.ChannelVolume + x, u => { if (u > 0) { genericChannel.SetVolume(u); } });
				}
				x++;
			}


			// Presets 
			x = 0;
			// from SiMPL > to Plugin
			trilist.SetStringSigAction(joinMap.Presets, s => DspDevice.RunPreset(s));
			foreach (var preset in DspDevice.PresetList)
			{
				var temp = x;
				// from SiMPL > to Plugin
				trilist.StringInput[joinMap.Presets + temp + 1].StringValue = preset.Label;
				trilist.SetSigTrueAction(joinMap.Presets + temp + 1, () => DspDevice.RunPresetNumber(temp));
				x++;
			}

			// VoIP Dialer
			uint lineOffset = 0;
			foreach (var line in DspDevice.Dialers)
			{
				var dialer = line;

				var dialerLineOffset = lineOffset;
				Debug.Console(2, "AddingDialerBRidge {0} {1} Offset", dialer.Key, dialerLineOffset);

				// from SiMPL > to Plugin
				trilist.SetSigTrueAction((joinMap.Keypad0 + dialerLineOffset), () => DspDevice.Dialers[dialer.Key].SendKeypad(QscDspDialer.EKeypadKeys.Num0));
				trilist.SetSigTrueAction((joinMap.Keypad1 + dialerLineOffset), () => dialer.Value.SendKeypad(QscDspDialer.EKeypadKeys.Num1));
				trilist.SetSigTrueAction((joinMap.Keypad2 + dialerLineOffset), () => dialer.Value.SendKeypad(QscDspDialer.EKeypadKeys.Num2));
				trilist.SetSigTrueAction((joinMap.Keypad3 + dialerLineOffset), () => dialer.Value.SendKeypad(QscDspDialer.EKeypadKeys.Num3));
				trilist.SetSigTrueAction((joinMap.Keypad4 + dialerLineOffset), () => dialer.Value.SendKeypad(QscDspDialer.EKeypadKeys.Num4));
				trilist.SetSigTrueAction((joinMap.Keypad5 + dialerLineOffset), () => dialer.Value.SendKeypad(QscDspDialer.EKeypadKeys.Num5));
				trilist.SetSigTrueAction((joinMap.Keypad6 + dialerLineOffset), () => dialer.Value.SendKeypad(QscDspDialer.EKeypadKeys.Num6));
				trilist.SetSigTrueAction((joinMap.Keypad7 + dialerLineOffset), () => dialer.Value.SendKeypad(QscDspDialer.EKeypadKeys.Num7));
				trilist.SetSigTrueAction((joinMap.Keypad8 + dialerLineOffset), () => dialer.Value.SendKeypad(QscDspDialer.EKeypadKeys.Num8));
				trilist.SetSigTrueAction((joinMap.Keypad9 + dialerLineOffset), () => dialer.Value.SendKeypad(QscDspDialer.EKeypadKeys.Num9));
				trilist.SetSigTrueAction((joinMap.KeypadStar + dialerLineOffset), () => dialer.Value.SendKeypad(QscDspDialer.EKeypadKeys.Star));
				trilist.SetSigTrueAction((joinMap.KeypadPound + dialerLineOffset), () => dialer.Value.SendKeypad(QscDspDialer.EKeypadKeys.Pound));
				trilist.SetSigTrueAction((joinMap.KeypadClear + dialerLineOffset), () => dialer.Value.SendKeypad(QscDspDialer.EKeypadKeys.Clear));
				trilist.SetSigTrueAction((joinMap.KeypadBackspace + dialerLineOffset), () => dialer.Value.SendKeypad(QscDspDialer.EKeypadKeys.Backspace));
				// from SiMPL > to Plugin
				trilist.SetSigTrueAction(joinMap.Dial + dialerLineOffset, () => dialer.Value.Dial());
				trilist.SetSigTrueAction(joinMap.DoNotDisturbToggle + dialerLineOffset, () => dialer.Value.DoNotDisturbToggle());
				trilist.SetSigTrueAction(joinMap.DoNotDisturbOn + dialerLineOffset, () => dialer.Value.DoNotDisturbOn());
				trilist.SetSigTrueAction(joinMap.DoNotDisturbOff + dialerLineOffset, () => dialer.Value.DoNotDisturbOff());
				trilist.SetSigTrueAction(joinMap.AutoAnswerToggle + dialerLineOffset, () => dialer.Value.AutoAnswerToggle());
				trilist.SetSigTrueAction(joinMap.AutoAnswerOn + dialerLineOffset, () => dialer.Value.AutoAnswerOn());
				trilist.SetSigTrueAction(joinMap.AutoAnswerOff + dialerLineOffset, () => dialer.Value.AutoAnswerOff());
				trilist.SetSigTrueAction(joinMap.EndCall + dialerLineOffset, () => dialer.Value.EndAllCalls());

				// from Plugin > to SiMPL
				dialer.Value.DoNotDisturbFeedback.LinkInputSig(trilist.BooleanInput[joinMap.DoNotDisturbToggle + dialerLineOffset]);
				dialer.Value.DoNotDisturbFeedback.LinkInputSig(trilist.BooleanInput[joinMap.DoNotDisturbOn + dialerLineOffset]);
				dialer.Value.DoNotDisturbFeedback.LinkComplementInputSig(trilist.BooleanInput[joinMap.DoNotDisturbOff + dialerLineOffset]);

				// from Plugin > to SiMPL
				dialer.Value.AutoAnswerFeedback.LinkInputSig(trilist.BooleanInput[joinMap.AutoAnswerToggle + dialerLineOffset]);
				dialer.Value.AutoAnswerFeedback.LinkInputSig(trilist.BooleanInput[joinMap.AutoAnswerOn + dialerLineOffset]);
				dialer.Value.AutoAnswerFeedback.LinkComplementInputSig(trilist.BooleanInput[joinMap.AutoAnswerOff + dialerLineOffset]);
				dialer.Value.CallerIdNumberFeedback.LinkInputSig(trilist.StringInput[joinMap.CallerIdNumberFb + dialerLineOffset]);

				// from Plugin > to SiMPL
				dialer.Value.OffHookFeedback.LinkInputSig(trilist.BooleanInput[joinMap.Dial + dialerLineOffset]);
				dialer.Value.OffHookFeedback.LinkInputSig(trilist.BooleanInput[joinMap.OffHook + dialerLineOffset]);
				dialer.Value.OffHookFeedback.LinkComplementInputSig(trilist.BooleanInput[joinMap.OnHook + dialerLineOffset]);
				dialer.Value.DialStringFeedback.LinkInputSig(trilist.StringInput[joinMap.DialStringCmd + dialerLineOffset]);

				// from Plugin > to SiMPL
				dialer.Value.IncomingCallFeedback.LinkInputSig(trilist.BooleanInput[joinMap.IncomingCall + dialerLineOffset]);

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

		public QscDspDeviceJoinMap()
		{			
			IsOnline = 1;				// digitial single feedback
			Presets = 100;				// digital array input, analog single input, serial array feedback			
			ChannelVisible = 200;		// digital array feedback			
			ChannelMuteToggle = 400;	// digital array input/feedback
			ChannelMuteOn = 600;		// digital array input/feedback
			ChannelMuteOff = 800;		// digital array input/feedback
			ChannelVolumeUp = 1000;		// digital array input
			ChannelVolumeDown = 1200;	// digital array input


			ChannelVolume = 200;		// analog array input/feedback
			ChannelType = 400;			// analog array feedback


			Address = 1;				// serial single input
			Prefix = 2;					// serial single input
			ChannelName = 200;			// serial array feedback
			
			// dialer			
			IncomingCall = 3100;		// digital single feedback
			DialStringCmd = 3100;		// serial single input/feedback

			CallerIdNumberFb = 3104;	// serial single feedback
			EndCall = 3107;				// digital single feedback

			Keypad0 = 3110;				// digital single input
			Keypad1 = 3111;				// digital single input
			Keypad2 = 3112;				// digital single input
			Keypad3 = 3113;				// digital single input
			Keypad4 = 3114;				// digital single input
			Keypad5 = 3115;				// digital single input
			Keypad6 = 3116;				// digital single input
			Keypad7 = 3117;				// digital single input
			Keypad8 = 3118;				// digital single input
			Keypad9 = 3119;				// digital single input
			KeypadStar = 3120;			// digital single input
			KeypadPound = 3121;			// digital single input
			KeypadClear = 3122;			// digital single input
			KeypadBackspace = 3123;		// digital single input

			Dial = 3124;				// digital single input

			AutoAnswerOn = 3125;		// digital single input/feedback
			AutoAnswerOff = 3126;		// digital single input/feedback
			AutoAnswerToggle = 3127;	// digital single input/feedback

			OnHook = 3129;				// digital single input/feedback
			OffHook = 3130;				// digital single input/feedback

			DoNotDisturbToggle = 3132;	// digital single input/feedback
			DoNotDisturbOn = 3133;		// digital single input/feedback
			DoNotDisturbOff = 3134;		// digital single input/feedback
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

}