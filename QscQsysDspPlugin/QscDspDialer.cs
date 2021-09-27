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
		public QscDialerConfig Tags;
		public bool IsInCall { get; private set; }
		public QscDsp Parent { get; private set; }
		public string DialString { get; private set; }
		public bool OffHook { get; private set; }
		public bool AutoAnswerState { get; private set; }
		public bool DoNotDisturbState { get; private set; }

		private string _callerIdNumber;
		public string CallerIdNumber
		{
			get
			{
				return _callerIdNumber;
			}
			set
			{
				_callerIdNumber = value;
				CallerIdNumberFb.FireUpdate();
			}
		}

		private bool _incomingCall;
		public bool IncomingCall
		{
			get { return _incomingCall; }
			set
			{
				_incomingCall = value;
				IncomingCallFeedback.FireUpdate();
			}
		}

		public BoolFeedback IncomingCallFeedback;
		public BoolFeedback OffHookFeedback;
		public BoolFeedback AutoAnswerFeedback;
		public BoolFeedback DoNotDisturbFeedback;
		public StringFeedback DialStringFeedback;
		public StringFeedback CallerIdNumberFb;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="Config">configuration object</param>
		/// <param name="parent">parent dsp instance</param>
		public QscDspDialer(QscDialerConfig Config, QscDsp parent)
		{
			Tags = Config;
			Parent = parent;

			IncomingCallFeedback = new BoolFeedback(() => { return IncomingCall; });
			DialStringFeedback = new StringFeedback(() => { return DialString; });
			OffHookFeedback = new BoolFeedback(() => { return OffHook; });
			AutoAnswerFeedback = new BoolFeedback(() => { return AutoAnswerState; });
			DoNotDisturbFeedback = new BoolFeedback(() => { return DoNotDisturbState; });
			CallerIdNumberFb = new StringFeedback(() => { return CallerIdNumber; });
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
		void OnCallStatusChange(CodecCallStatusItemChangeEventArgs args)
		{
			var handler = CallStatusChange;
			if (handler != null)
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
				PropertyInfo[] properties = Tags.GetType().GetCType().GetProperties();
				//GetPropertyValues(Tags);

				Debug.Console(2, "QscDspDialer Subscribe");
				foreach (var prop in properties)
				{
					//var val = prop.GetValue(obj, null);
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
			Debug.Console(1, "QscDialerTag {0} Response: '{1}'", customName, value);
			if (customName == Tags.DialStringTag)
			{
				Debug.Console(2, "QscDialerTag DialStringChanged ", value);
				this.DialString = value;
				this.DialStringFeedback.FireUpdate();
			}
			else if (customName == Tags.DoNotDisturbTag)
			{
				if (value == "on")
				{
					this.DoNotDisturbState = true;
				}
				else if (value == "off")
				{
					this.DoNotDisturbState = false;
				}
				DoNotDisturbFeedback.FireUpdate();
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
					this.OffHook = true;
					var splitString = value.Split(' ');

					if (splitString.Count() >= 2)
					{
						CallerIdNumber = splitString[1];
					}
				}
				else if (value == "Disconnected")
				{
					this.IncomingCall = false;
					this.OffHook = false;
					CallerIdNumber = "";
					if (Tags.ClearOnHangup)
					{
						this.SendKeypad(EKeypadKeys.Clear);
					}
				}
				else if (value == "Idle")
				{
					this.IncomingCall = false;
					this.OffHook = false;
					CallerIdNumber = "";
					if (Tags.ClearOnHangup)
					{
						this.SendKeypad(EKeypadKeys.Clear);
					}
				}
				this.OffHookFeedback.FireUpdate();
			}
			else if (customName == Tags.AutoAnswerTag)
			{
				if (value == "on")
				{
					this.AutoAnswerState = true;
				}
				else if (value == "off")
				{
					this.AutoAnswerState = false;
				}
				AutoAnswerFeedback.FireUpdate();
			}
			else if (customName == Tags.HookStatusTag)
			{
				if (value == "true")
				{
					this.OffHook = true;
				}
				else if (value == "false")
				{
					this.OffHook = false;
				}
				this.OffHookFeedback.FireUpdate();
			}
		}
		
		/// <summary>
		/// Toggles the do not disturb state
		/// </summary>
		public void DoNotDisturbToggle()
		{
			int dndStateInt = !DoNotDisturbState ? 1 : 0;
			Parent.SendLine(string.Format("csv \"{0}\" {1}", Tags.DoNotDisturbTag, dndStateInt));
		}

		/// <summary>
		/// Sets the do not disturb state on
		/// </summary>
		public void DoNotDisturbOn()
		{
            Parent.SendLine(string.Format("csv \"{0}\" 1", Tags.DoNotDisturbTag));
		}

		/// <summary>
		/// Sets the do not disturb state off
		/// </summary>
		public void DoNotDisturbOff()
		{
            Parent.SendLine(string.Format("csv \"{0}\" 0", Tags.DoNotDisturbTag));
		}

		/// <summary>
		/// Toggles the auto answer state
		/// </summary>
		public void AutoAnswerToggle()
		{
			int autoAnswerStateInt = !AutoAnswerState ? 1 : 0;
            Parent.SendLine(string.Format("csv \"{0}\" {1}", Tags.AutoAnswerTag, autoAnswerStateInt));
		}

		/// <summary>
		/// Sets the auto answer state on
		/// </summary>
		public void AutoAnswerOn()
		{
            Parent.SendLine(string.Format("csv \"{0}\" 1", Tags.AutoAnswerTag));
		}

		/// <summary>
		/// Sets the auto answer state off
		/// </summary>
		public void AutoAnswerOff()
		{
            Parent.SendLine(string.Format("csv \"{0}\" 0", Tags.AutoAnswerTag));
		}

		private void PollKeypad()
		{
			Thread.Sleep(50);
            Parent.SendLine(string.Format("cg \"{0}\"", Tags.DialStringTag));
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
                var cmdToSend = string.Format("ct \"{0}\"", keypadTag);
				Parent.SendLine(cmdToSend);
				PollKeypad();
			}
		}

		/// <summary>
		/// Sends the subscription command using the provided named control and change group
		/// </summary>
		/// <param name="instanceTag">Named control/Instance tag</param>
		/// <param name="changeGroup">Change group ID</param>
		public void SendSubscriptionCommand(string instanceTag)
		{
			// Subscription string format: InstanceTag subscribe attributeCode Index1 customName responseRate
			// Ex: "RoomLevel subscribe level 1 MyRoomLevel 500"

            var cmd = string.Format("cga 1 \"{0}\"", instanceTag);
			Parent.SendLine(cmd);
		}

		/// <summary>
		/// Toggles the hook state
		/// </summary>
		public void Dial()
		{
			Parent.SendLine(!this.OffHook
                ? string.Format("ct \"{0}\"", Tags.ConnectTag)		// !this.OffHook
                : string.Format("ct \"{0}\"", Tags.DisconnectTag));	// this.OffHook
			Thread.Sleep(50);
            Parent.SendLine(string.Format("cg \"{0}\"", Tags.CallStatusTag));
		}

		/// <summary>
		/// Dial overload
		/// Dials the number provided
		/// </summary>
		/// <param name="number">Number to dial</param>
		public void Dial(string number)
		{
            if (string.IsNullOrEmpty(number))
                return;

            SendKeypad(EKeypadKeys.Clear);
		    foreach (var digit in number)
		    {
		        switch (digit)
		        {
		            case '0':
		            {
		                SendKeypad(EKeypadKeys.Num0);
		                break;
		            }
                    case '1':
                    {
                        SendKeypad(EKeypadKeys.Num1);
                        break;
                    }
                    case '2':
                    {
                        SendKeypad(EKeypadKeys.Num2);
                        break;
                    }
                    case '3':
                    {
                        SendKeypad(EKeypadKeys.Num3);
                        break;
                    }
                    case '4':
                    {
                        SendKeypad(EKeypadKeys.Num4);
                        break;
                    }
                    case '5':
                    {
                        SendKeypad(EKeypadKeys.Num5);
                        break;
                    }
                    case '6':
                    {
                        SendKeypad(EKeypadKeys.Num6);
                        break;
                    }
                    case '7':
                    {
                        SendKeypad(EKeypadKeys.Num7);
                        break;
                    }
                    case '8':
                    {
                        SendKeypad(EKeypadKeys.Num8);
                        break;
                    }
                    case '9':
                    {
                        SendKeypad(EKeypadKeys.Num9);
                        break;
                    }
                    case '#':
                    {
                        SendKeypad(EKeypadKeys.Pound);
                        break;
                    }
                    case '*':
                    {
                        SendKeypad(EKeypadKeys.Star);
                        break;
                    }
		        }
		    }
		}

		/// <summary>
		/// Ends the current call with the provided Id
		/// </summary>		
		/// <param name="item">Use "", use of CodecActiveCallItem is not implemented</param>
		public void EndCall(CodecActiveCallItem item)
		{
            Parent.SendLine(string.Format("ct \"{0}\"", Tags.DisconnectTag));
		}

		/// <summary>
		/// Ends all connectted calls
		/// </summary>
		public void EndAllCalls()
		{
            Parent.SendLine(string.Format("ct \"{0}\"", Tags.DisconnectTag));
		}

		/// <summary>
		/// Accepts incoming call
		/// </summary>
		public void AcceptCall()
		{
			this.IncomingCall = false;
            Parent.SendLine(string.Format("ct \"{0}\"", Tags.ConnectTag));
			Thread.Sleep(50);
            Parent.SendLine(string.Format("cg \"{0}\"", Tags.HookStatusTag));
		}

		/// <summary>
		/// Accepts the incoming call overload
		/// </summary>
		/// <param name="item">Use "", use of CodecActiveCallItem is not implemented</param>
		public void AcceptCall(CodecActiveCallItem item)
		{
			this.IncomingCall = false;
            Parent.SendLine(string.Format("ct \"{0}\"", Tags.ConnectTag));
			Thread.Sleep(50);
            Parent.SendLine(string.Format("cg \"{0}\"", Tags.HookStatusTag));
		}

		/// <summary>
		/// Rejects the incoming call
		/// </summary>
		public void RejectCall()
		{
			this.IncomingCall = false;
            Parent.SendLine(string.Format("ct \"{0}\"", Tags.DisconnectTag));
			Thread.Sleep(50);
            Parent.SendLine(string.Format("cg \"{0}\"", Tags.HookStatusTag));
		}

		/// <summary>
		/// Rejects the incoming call overload
		/// </summary>
		/// <param name="item"></param>
		public void RejectCall(CodecActiveCallItem item)
		{
			this.IncomingCall = false;
            Parent.SendLine(string.Format("ct \"{0}\"", Tags.DisconnectTag));
			Thread.Sleep(50);
            Parent.SendLine(string.Format("cg \"{0}\"", Tags.HookStatusTag));
		}

		/// <summary>
		/// Sends the DTMF tone of the keypad digit pressed
		/// </summary>
		/// <param name="digit">keypad digit pressed as a string</param>
		public void SendDtmf(string digit)
		{
			throw new NotImplementedException();
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