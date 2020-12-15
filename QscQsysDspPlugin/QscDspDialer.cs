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
					//var val = prop.GetValue(obj, null);
					Debug.Console(2, "Property {0}, {1}, {2}\n", prop.GetType().Name, prop.Name, prop.PropertyType.FullName);

					//if (prop.Name.Contains("Tag") && !prop.Name.Contains("keypad"))
					if (!prop.Name.Contains("Tag") || prop.Name.Contains("keypad")) continue;

					var propValue = prop.GetValue(Tags, null) as string;
					Debug.Console(2, "Property {0}, {1}, {2}\n", prop.GetType().Name, prop.Name, propValue);
					SendSubscriptionCommand(propValue, "1");
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
			Debug.Console(1, "QscDialerTag {0} Response: '{1}'", customName, value);
			if (customName == Tags.DialStringTag)
			{
				Debug.Console(2, "QscDialerTag DialStringChanged ", value);
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
				//if (value.Contains(""))
				//{
				//    OffHook = true;
				//    var splitString = value.Split(' ');
				//    if (splitString.Count() >= 2)
				//    {
				//        CallerIdNumber = splitString[1];
				//    }
				//}
				if (value.Contains("Dialing") || value.Contains("Connected") || value.Contains("Ringing"))
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
					CallerIdNumber = "";
					if (Tags.ClearOnHangup)
					{
						SendKeypad(EKeypadKeys.Clear);
					}
				}
				else if (value == "Idle")
				{
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
			var dndStateInt = !DoNotDisturbState ? 1 : 0;
			Parent.SendLine(string.Format("csv {0} {1}", Tags.DoNotDisturbTag, dndStateInt));
		}

		/// <summary>
		/// Sets the do not disturb state on
		/// </summary>
		public void DoNotDisturbOn()
		{
			Parent.SendLine(string.Format("csv {0} 1", Tags.DoNotDisturbTag));
		}

		/// <summary>
		/// Sets the do not disturb state off
		/// </summary>
		public void DoNotDisturbOff()
		{
			Parent.SendLine(string.Format("csv {0} 0", Tags.DoNotDisturbTag));
		}

		/// <summary>
		/// Toggles the auto answer state
		/// </summary>
		public void AutoAnswerToggle()
		{
			var autoAnswerStateInt = !AutoAnswerState ? 1 : 0;
			Parent.SendLine(string.Format("csv {0} {1}", Tags.AutoAnswerTag, autoAnswerStateInt));
		}

		/// <summary>
		/// Sets the auto answer state on
		/// </summary>
		public void AutoAnswerOn()
		{
			Parent.SendLine(string.Format("csv {0} 1", Tags.AutoAnswerTag));
		}

		/// <summary>
		/// Sets the auto answer state off
		/// </summary>
		public void AutoAnswerOff()
		{
			Parent.SendLine(string.Format("csv {0} 0", Tags.AutoAnswerTag));
		}

		private void PollKeypad()
		{
			Thread.Sleep(50);
			Parent.SendLine(string.Format("cg {0}", Tags.DialStringTag));
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

			if (keypadTag == null) return;

			Parent.SendLine(string.Format("ct {0}", keypadTag));
			PollKeypad();
		}

		/// <summary>
		/// Sends the subscription command using the provided named control and change group
		/// </summary>
		/// <param name="instanceTag">Named control/Instance tag</param>
		/// <param name="changeGroup">Change group ID</param>
		public void SendSubscriptionCommand(string instanceTag, string changeGroup)
		{
			// Subscription string format: InstanceTag subscribe attributeCode Index1 customName responseRate
			// Ex: "RoomLevel subscribe level 1 MyRoomLevel 500"

			var cmd = string.Format("cga {0} {1}", changeGroup, instanceTag);

			Parent.SendLine(cmd);
		}

		/// <summary>
		/// Toggles the hook state
		/// </summary>
		public void Dial()
		{
			Parent.SendLine(OffHook
				? string.Format("ct {0}", Tags.DisconnectTag) // OffHook true
				: string.Format("ct {0}", Tags.ConnectTag));  // OffHook false

			Thread.Sleep(50);
			Parent.SendLine(string.Format("cg {0}", Tags.CallStatusTag));
		}

		/// <summary>
		/// Dial overload
		/// Dials the number provided
		/// </summary>
		/// <param name="number">Number to dial</param>
		public void Dial(string number)
		{
			if (OffHook) return;

			if (number.Length > 0)
			{
				Parent.SendLine(string.Format("css {0} {1}", Tags.DialStringTag, number));
			}

			Parent.SendLine(string.Format("ct {0}", Tags.ConnectTag));

			Thread.Sleep(50);
			Parent.SendLine(string.Format("cg {0}", Tags.CallStatusTag));
		}

		/// <summary>
		/// Ends the current call with the provided Id
		/// </summary>		
		/// <param name="item">Use "", use of CodecActiveCallItem is not implemented</param>
		public void EndCall(CodecActiveCallItem item)
		{
			Parent.SendLine(string.Format("ct {0}", Tags.DisconnectTag));
		}

		/// <summary>
		/// Ends all connectted calls
		/// </summary>
		public void EndAllCalls()
		{
			Parent.SendLine(string.Format("ct {0}", Tags.DisconnectTag));
		}

		/// <summary>
		/// Accepts the incoming call
		/// </summary>
		/// <param name="item">Use "", use of CodecActiveCallItem is not implemented</param>
		public void AcceptCall(CodecActiveCallItem item)
		{
			//throw new NotImplementedException();
			Dial();
		}

		/// <summary>
		/// Rejects the incoming call
		/// </summary>
		/// <param name="item"></param>
		public void RejectCall(CodecActiveCallItem item)
		{
			//throw new NotImplementedException();
			EndAllCalls();
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