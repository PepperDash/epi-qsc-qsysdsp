using System;
using System.Linq;
using Crestron.SimplSharp.Reflection;
using Crestron.SimplSharpPro.CrestronThread;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Devices.Common.Codec;

namespace QscQsysDspPlugin
{
	/// <summary>
	/// QSC DSP Dialer class
	/// </summary>
	public class QscDspDialer : IHasDialer 
	{
		/// <summary>
		/// Parent DSP
		/// </summary>
		public QscDsp Parent { get; private set; }

		/// <summary>
		/// Dialer block configuration 
		/// </summary>
		public QscDialerConfig Tags;

		/// <summary>
		/// Tracks in call state
		/// </summary>
		public bool IsInCall { get; private set; }

		/// <summary>
		/// Dial string feedback 
		/// </summary>
		public StringFeedback DialStringFeedback;
		// Dial string backer field
		private string _dialString;
		/// <summary>
		/// Dial string property
		/// </summary>
		public string DialString
		{
			get { return _dialString; }
			private set
			{
				_dialString = value;
				DialStringFeedback.FireUpdate();
			}
		}

		/// <summary>
		/// Off hook feedback
		/// </summary>
		public BoolFeedback OffHookFeedback;
		// Off hook backer field
		private bool _offHook;
		/// <summary>
		/// Off Hook property
		/// </summary>
		public bool OffHook
		{
			get { return _offHook; }
			private set
			{
				_offHook = value;
				OffHookFeedback.FireUpdate();
			}
		}

		/// <summary>
		/// Auto answer feedback
		/// </summary>
		public BoolFeedback AutoAnswerFeedback;
		// Auto answer backer field
		private bool _autoAnswerState;
		/// <summary>
		/// Auto answer property
		/// </summary>
		public bool AutoAnswerState
		{
			get { return _autoAnswerState; }
			private set
			{
				_autoAnswerState = value;
				AutoAnswerFeedback.FireUpdate();
			}
		}

		/// <summary>
		/// Do not disturb feedback
		/// </summary>
		public BoolFeedback DoNotDisturbFeedback;
		// Do not disturb backer field
		private bool _doNotDisturbState;
		/// <summary>
		/// Do not disturb property
		/// </summary>
		public bool DoNotDisturbState
		{
			get { return _doNotDisturbState; }
			private set
			{
				_doNotDisturbState = value;
				DoNotDisturbFeedback.FireUpdate();
			}
		}

		/// <summary>
		/// Caller ID number feedback
		/// </summary>
		public StringFeedback CallerIdNumberFeedback;
		// Caller ID number backer field
		private string _callerIdNumber;
		/// <summary>
		///  Caller ID Number property
		/// </summary>
		public string CallerIdNumber
		{
			get { return _callerIdNumber; }
			set
			{
				_callerIdNumber = value;
				CallerIdNumberFeedback.FireUpdate();
			}
		}

		/// <summary>
		/// Incoming call feedback
		/// </summary>
		public BoolFeedback IncomingCallFeedback;
		// Incoming call backer field
		private bool _incomingCall;
		/// <summary>
		/// Incoming call property
		/// </summary>
		public bool IncomingCall
		{
			get { return _incomingCall; }
			set
			{
				_incomingCall = value;
				IncomingCallFeedback.FireUpdate();
			}
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="config">configuration object</param>
		/// <param name="parent">parent dsp instance</param>
		public QscDspDialer(QscDialerConfig config, QscDsp parent)
		{
			Tags = config;
			Parent = parent;

			IncomingCallFeedback = new BoolFeedback(() => { return IncomingCall; });
			DialStringFeedback = new StringFeedback(() => { return DialString; });
			OffHookFeedback = new BoolFeedback(() => { return OffHook; });
			AutoAnswerFeedback = new BoolFeedback(() => { return AutoAnswerState; });
			DoNotDisturbFeedback = new BoolFeedback(() => { return DoNotDisturbState; });
			CallerIdNumberFeedback = new StringFeedback(() => { return CallerIdNumber; });
		}

		/// <summary>
		/// Call status change event
		/// Interface requires this
		/// </summary>
		public event EventHandler<CodecCallStatusItemChangeEventArgs> CallStatusChange;

		/// <summary>
		/// Call status event handler
		/// </summary>
		/// <param name="args"></param>
		public void OnCallStatusChange(CodecCallStatusItemChangeEventArgs args)
		{
			var handler = CallStatusChange;
			if (handler == null) return;
			CallStatusChange(this, args);
		}

		/// <summary>
		/// Subscription method
		/// </summary>
		public void Subscribe()
		{
			try
			{
				// Do subscriptions and blah blah
				// This would be better using reflection JTA 2018-08-28
				//PropertyInfo[] properties = Tags.GetType().GetCType().GetProperties();
				var properties = Tags.GetType().GetCType().GetProperties();
				//GetPropertyValues(Tags);

				Debug.Console(2, "QscDspDialer Subscribe");
				foreach (var prop in properties)
				{
                    if (prop.Name.Contains("Tag") && !prop.Name.ToLower().Contains("keypad"))
					{
						var propValue = prop.GetValue(Tags, null) as string;
						Debug.Console(2, "Property {0}, {1}, {2}\n", prop.GetType().Name, prop.Name, propValue);
						SendSubscriptionCommand(propValue);
					}
				}
			}
			catch (Exception e)
			{
				Debug.Console(2, "QscDspDialer Subscription Error: '{0}'\n", e);
			}

			// SendSubscriptionCommand(, "1");
			// SendSubscriptionCommand(config. , "mute", 500);
		}

		/// <summary>
		/// Parses subscription messages
		/// </summary>
		/// <param name="customName"></param>
		/// <param name="value"></param>
		public void ParseSubscriptionMessage(string customName, string value)
		{
			// Check for valid subscription response
			Debug.Console(0, "ParseMessage customName: {0} value: '{1}'", customName, value);
			if (customName == Tags.DialStringTag)
			{
				Debug.Console(0, "ParseMessage customName: {0} == Tags.DialStringTag: {1} | value: {2}", customName, Tags.DialStringTag, value);
				DialString = value;
				DialStringFeedback.FireUpdate();
			}
			else if (customName == Tags.DoNotDisturbTag)
			{
				switch (value)
				{
					case "on":
						DoNotDisturbState = true;
						break;
					case "off":
						DoNotDisturbState = false;
						break;
				}
			}
			else if (customName == Tags.CallStatusTag)
			{
				// TODO [ ] Add incoming call/ringing to parse subscription message
				if (value == "Incoming")
				{
					this.IncomingCall = true;
				}
				else if (value.Contains("Ringing"))
				{
					this.IncomingCall = false;
					this.OffHook = true;
					var splitString = value.Split(' ');
					if (splitString.Count() >= 2)
					{
						CallerIdNumber = splitString[1];
					}
				}
				else if (value.Contains("Dialing") || value.Contains("Connected"))
				{
					OffHook = true;
					var splitString = value.Split(' ');
					if (splitString.Count() >= 2)
					{
						CallerIdNumber = splitString[1];
					}
				}
				else if (value == "Disconnected")
				{
					OffHook = false;
					IncomingCall = false;
					CallerIdNumber = "";
					if (Tags.ClearOnHangup)
					{
						SendKeypad(EKeypadKeys.Clear);
					}
				}
				else if (value == "Idle")
				{
					IncomingCall = false;
					OffHook = false;
					CallerIdNumber = "";
					if (Tags.ClearOnHangup)
					{
						SendKeypad(EKeypadKeys.Clear);
					}
				}
			}
			else if (customName == Tags.AutoAnswerTag)
			{
				switch (value)
				{
					case "on":
						AutoAnswerState = true;
						break;
					case "off":
						AutoAnswerState = false;
						break;
				}
			}
			else if (customName == Tags.HookStatusTag)
			{
				switch (value)
				{
					case "true":
						OffHook = true;
						break;
					case "false":
						OffHook = false;
						break;
				}
			}
		}

		/// <summary>
		/// Toggles the do not disturb state
		/// </summary>
		public void DoNotDisturbToggle()
		{
			Parent.SendControlSetValue(Tags.DoNotDisturbTag, DoNotDisturbState ? 0.0 : 1.0);
		}

		/// <summary>
		/// Sets the do not disturb state on
		/// </summary>
		public void DoNotDisturbOn()
		{
			Parent.SendControlSetValue(Tags.DoNotDisturbTag, 1.0);
		}

		/// <summary>
		/// Sets the do not disturb state off
		/// </summary>
		public void DoNotDisturbOff()
		{
			Parent.SendControlSetValue(Tags.DoNotDisturbTag, 0.0);
		}

		/// <summary>
		/// Toggles the auto answer state
		/// </summary>
		public void AutoAnswerToggle()
		{
			Parent.SendControlSetValue(Tags.AutoAnswerTag, AutoAnswerState ? 0.0 : 1.0);
		}

		/// <summary>
		/// Sets the auto answer state on
		/// </summary>
		public void AutoAnswerOn()
		{
			Parent.SendControlSetValue(Tags.AutoAnswerTag, 1.0);
		}

		/// <summary>
		/// Sets the auto answer state off
		/// </summary>
		public void AutoAnswerOff()
		{
			Parent.SendControlSetValue(Tags.AutoAnswerTag, 0.0);
		}

		private void PollKeypad()
		{
			Thread.Sleep(50);
			Parent.SendControlGet(Tags.DialStringTag);
		}

		/// <summary>
		/// Sends the pressed keypad number
		/// </summary>
		/// <param name="button">Button pressed</param>
		public void SendKeypad(EKeypadKeys button)
		{
			string keypadTag = null;
			// Debug.Console(2, "DIaler {0} SendKeypad {1}", this.ke);
			switch (button)
			{
				case EKeypadKeys.Num0: keypadTag = Tags.Keypad0Tag; break;
				case EKeypadKeys.Num1: keypadTag = Tags.Keypad1Tag; break;
				case EKeypadKeys.Num2: keypadTag = Tags.Keypad2Tag; break;
				case EKeypadKeys.Num3: keypadTag = Tags.Keypad3Tag; break;
				case EKeypadKeys.Num4: keypadTag = Tags.Keypad4Tag; break;
				case EKeypadKeys.Num5: keypadTag = Tags.Keypad5Tag; break;
				case EKeypadKeys.Num6: keypadTag = Tags.Keypad6Tag; break;
				case EKeypadKeys.Num7: keypadTag = Tags.Keypad7Tag; break;
				case EKeypadKeys.Num8: keypadTag = Tags.Keypad8Tag; break;
				case EKeypadKeys.Num9: keypadTag = Tags.Keypad9Tag; break;
				case EKeypadKeys.Pound: keypadTag = Tags.KeypadPoundTag; break;
				case EKeypadKeys.Star: keypadTag = Tags.KeypadStarTag; break;
				case EKeypadKeys.Backspace: keypadTag = Tags.KeypadBackspaceTag; break;
				case EKeypadKeys.Clear: keypadTag = Tags.KeypadClearTag; break;
			}

			if (keypadTag != null)
			{
				Parent.SendControlTrigger(keypadTag);
				PollKeypad();
			}
		}

		/// <summary>
		/// Sends the subscription command using the provided named control
		/// Registers the tag with the parent's QRC change group.
		/// </summary>
		/// <param name="instanceTag">Named control/Instance tag</param>
		public void SendSubscriptionCommand(string instanceTag)
		{
			Parent.AddControlToChangeGroup(instanceTag);
		}

		/// <summary>
		/// Toggles the hook state
		/// </summary>
		public void Dial()
		{
			Parent.SendControlTrigger(!this.OffHook ? Tags.ConnectTag : Tags.DisconnectTag);
			Thread.Sleep(50);
			Parent.SendControlGet(Tags.CallStatusTag);
		}

		/// <summary>
		/// Dial overload — sets the dial string then triggers connect
		/// </summary>
		/// <param name="number">Number to dial</param>
		public void Dial(string number)
		{
			if (string.IsNullOrEmpty(number))
				return;

			if (OffHook) EndAllCalls();

			Parent.SendControlSetString(Tags.DialStringTag, number);
			Parent.SendControlTrigger(Tags.ConnectTag);
			Thread.Sleep(50);
			Parent.SendControlGet(Tags.CallStatusTag);
		}

		/// <summary>
		/// Ends the current call
		/// </summary>
		/// <param name="item">not used</param>
		public void EndCall(CodecActiveCallItem item)
		{
			Parent.SendControlTrigger(Tags.DisconnectTag);
		}

		/// <summary>
		/// Ends all connected calls
		/// </summary>
		public void EndAllCalls()
		{
			Parent.SendControlTrigger(Tags.DisconnectTag);
		}

		/// <summary>
		/// Accepts incoming call
		/// </summary>
		public void AcceptCall()
		{
			this.IncomingCall = false;
			Parent.SendControlTrigger(Tags.ConnectTag);
			Thread.Sleep(50);
			Parent.SendControlGet(Tags.HookStatusTag);
		}

		/// <summary>
		/// Accepts the incoming call overload
		/// </summary>
		/// <param name="item">not used</param>
		public void AcceptCall(CodecActiveCallItem item)
		{
			this.IncomingCall = false;
			Parent.SendControlTrigger(Tags.ConnectTag);
			Thread.Sleep(50);
			Parent.SendControlGet(Tags.HookStatusTag);
		}

		/// <summary>
		/// Rejects the incoming call
		/// </summary>
		public void RejectCall()
		{
			this.IncomingCall = false;
			Parent.SendControlTrigger(Tags.DisconnectTag);
			Thread.Sleep(50);
			Parent.SendControlGet(Tags.HookStatusTag);
		}

		/// <summary>
		/// Rejects the incoming call overload
		/// </summary>
		/// <param name="item">not used</param>
		public void RejectCall(CodecActiveCallItem item)
		{
			this.IncomingCall = false;
			Parent.SendControlTrigger(Tags.DisconnectTag);
			Thread.Sleep(50);
			Parent.SendControlGet(Tags.HookStatusTag);
		}

		/// <summary>
		/// Sends the DTMF tone of the keypad digit pressed
		/// </summary>
		/// <param name="digit">keypad digit pressed as a string</param>
		public void SendDtmf(string digit)
		{
			var keypadTag = EKeypadKeys.Clear;
			// Debug.Console(2, "DIaler {0} SendKeypad {1}", this.ke);
			switch (digit)
			{
				case "0":
					keypadTag = EKeypadKeys.Num0;
					break;
				case "1":
					keypadTag = EKeypadKeys.Num1;
					break;
				case "2":
					keypadTag = EKeypadKeys.Num2;
					break;
				case "3":
					keypadTag = EKeypadKeys.Num3;
					break;
				case "4":
					keypadTag = EKeypadKeys.Num4;
					break;
				case "5":
					keypadTag = EKeypadKeys.Num5;
					break;
				case "6":
					keypadTag = EKeypadKeys.Num6;
					break;
				case "7":
					keypadTag = EKeypadKeys.Num7;
					break;
				case "8":
					keypadTag = EKeypadKeys.Num8;
					break;
				case "9":
					keypadTag = EKeypadKeys.Num9;
					break;
				case "#":
					keypadTag = EKeypadKeys.Pound;
					break;
				case "*":
					keypadTag = EKeypadKeys.Star;
					break;
			}

			if (keypadTag == EKeypadKeys.Clear) return;

			SendKeypad(keypadTag);
		}

		/// <summary>
		/// Keypad digits pressed enum
		/// </summary>
		public enum EKeypadKeys
		{
			Num1,
			Num2,
			Num3,
			Num4,
			Num5,
			Num6,
			Num7,
			Num8,
			Num9,
			Num0,
			Star,
			Pound,
			Clear,
			Backspace
		}
	}
}