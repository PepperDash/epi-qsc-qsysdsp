using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using Crestron.SimplSharp.Reflection;
using Crestron.SimplSharpPro.CrestronThread;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Devices.Common.Codec;
using PepperDash.Essentials.Devices.Common.VideoCodec.Cisco;

namespace QscQsysDspPlugin
{
	/// <summary>
	/// QSC DSP Dialer class
	/// </summary>
	public class QscQsysDspDialer : IHasDialer
	{
		/// <summary>
		/// Dialer Parent DSP instance
		/// </summary>
		public QscQsysDsp Parent { get; private set; }

		/// <summary>
		/// Dialer configuration
		/// </summary>
		public QscQsysDialerConfig Tags;

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

		/// <summary>
		/// Dialer call state
		/// </summary>
		public bool IsInCall { get; private set; }



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
		/// Incoming call feedback
		/// </summary>
		public BoolFeedback IncomingCallFeedback;



		/// <summary>
		/// Off hook feedback
		/// </summary>
		public BoolFeedback OffHookFeedback;

		/// <summary>
		/// Dialer off hook state
		/// </summary>
		public bool OffHook { get; private set; }



		/// <summary>
		/// Dialer auto answer state
		/// </summary>
		public bool AutoAnswerState { get; private set; }

		/// <summary>
		/// Auto answer feedback
		/// </summary>
		public BoolFeedback AutoAnswerFeedback;



		/// <summary>
		/// Dialer do not disturb state
		/// </summary>
		public bool DoNotDisturbState { get; private set; }

		/// <summary>
		/// Do not distrub feedback
		/// </summary>
		public BoolFeedback DoNotDisturbFeedback;



		/// <summary>
		/// Dialer dial string
		/// </summary>
		public string DialString { get; private set; }

		/// <summary>
		/// Dial string feedback
		/// </summary>
		public StringFeedback DialStringFeedback;



		private string _callerIdNumber;
		/// <summary>
		/// Caller ID number property
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
		/// Caller ID Feedback
		/// </summary>
		public StringFeedback CallerIdNumberFeedback;



		/// <summary>
		/// Dialer Call status change event handler
		/// </summary>
		public event EventHandler<CodecCallStatusItemChangeEventArgs> CallStatusChange;


		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="config">Dialer configuration object</param>
		/// <param name="parent">Dialer Parent DSP</param>
		public QscQsysDspDialer(QscQsysDialerConfig config, QscQsysDsp parent)
		{
			Tags = config;
			Parent = parent;

			IncomingCallFeedback = new BoolFeedback(() => IncomingCall);
			OffHookFeedback = new BoolFeedback(() => OffHook);
			AutoAnswerFeedback = new BoolFeedback(() => AutoAnswerState);
			DoNotDisturbFeedback = new BoolFeedback(() => DoNotDisturbState);
			DialStringFeedback = new StringFeedback(() => DialString);
			CallerIdNumberFeedback = new StringFeedback(() => CallerIdNumber);
		}

		// call status change event handler method
		private void OnCallStatusChange(CodecCallStatusItemChangeEventArgs args)
		{
			var handler = CallStatusChange;
			if (handler == null) return;
			CallStatusChange(this, args);
		}

		/// <summary>
		/// Parses sbuscription messages
		/// </summary>
		/// <param name="customName"></param>
		/// <param name="value"></param>
		public void ParseSubsciptionMessage(string customName, string value)
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
				if (value.Contains("Ringing"))
				{
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
					this.OffHook = false;
					CallerIdNumber = "";
					if (Tags.ClearOnHangup)
					{
						this.SendKeypad(EKeypadKeys.Clear);
					}
				}
				else if (value == "Idle")
				{
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
		/// Sends a subscription command using the provided named control and chagne group
		/// </summary>
		/// <param name="instanceTag">Named Control/Instance tag</param>
		/// <param name="changeGroup">Change Group ID</param>
		public void SendSubscriptionCommand(string instanceTag, string changeGroup)
		{
			// Subscription string format: InstanceTag subscribe attributeCode Index1 customName responseRate
			// Ex: "RoomLevel subscribe level 1 MyRoomLevel 500"
			var cmd = string.Format("cga {0} {1}", changeGroup, instanceTag);
			Parent.SendText(cmd);
		}

		/// <summary>
		/// Susbscription method
		/// </summary>
		public void Subscribe()
		{
			var key = Parent.Key;
			try
			{
				// Do subscription and blah blah
				// This would be better using reflection JTA 2018-08-28
				var properties = Tags.GetType().GetCType().GetProperties();

				Debug.Console(2,"[{0}] Dialer Subscribe", key);
				foreach (var property in properties)
				{
					Debug.Console(2, "[{0}] Dialer Subscribe Property: {1}, {2}, {3}\n", key, property.GetType().Name, property.Name, property.PropertyType.FullName);
					if (property.Name.Contains("Tag") && !property.Name.Contains("keypad"))
					{
						var value = property.GetValue(Tags, null) as string;
						Debug.Console(2,"[{0}] Dialer Subscribe Property: {1}, {2}, {3}\n", key, property.GetType().Name, property.Name, value);
						SendSubscriptionCommand(value, 1);
					}
				}
			}
			catch (Exception ex)
			{
				Debug.Console(2,"[{0}] Dialer Subscription Error: '{1}'\n", key, ex);	
			}
		}

		/// <summary>
		/// Toggles hook state
		/// </summary>
		public void Dial()
		{
			if (OffHook)
				Parent.SendText(string.Format("ct {0}", Tags.DisconnectTag));
			else
				Parent.SendText(string.Format("ct {0}", Tags.ConnectTag));

			Thread.Sleep(50);
			Parent.SendText(string.Format("cg {0}", Tags.CallStatusTag));
		}

		/// <summary>
		/// Dials the provided number
		/// </summary>
		/// <param name="number">number to dial string</param>
		public void Dial(string number)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Ends the call identified
		/// </summary>
		/// <param name="activeCall"></param>
		public void EndCall(CodecActiveCallItem activeCall)
		{
			Parent.SendText(string.Format("ct {0}", Tags.DisconnectTag));
		}

		/// <summary>
		/// Ends all calls
		/// </summary>
		public void EndAllCalls()
		{
			Parent.SendText(string.Format("ct {0}", Tags.DisconnectTag));
		}

		/// <summary>
		/// Accpets the incoming call
		/// </summary>
		/// <param name="item"></param>
		public void AcceptCall(CodecActiveCallItem item)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Rejects the incoming call
		/// </summary>
		/// <param name="item"></param>
		public void RejectCall(CodecActiveCallItem item)
		{
			throw new NotImplementedException();
		}

		// polls the keypad for dial string text
		private void PollKeypad()
		{
			Thread.Sleep(50);
			Parent.SendText(string.Format("cg {0}", Tags.DialStringTag));
		}

		/// <summary>
		/// Sens the pressed keypad number
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
			var cmdToSend = string.Format("ct {0}", keypadTag);
			Parent.SendText(cmdToSend);
			PollKeypad();
		}

		/// <summary>
		/// Sends DTMF tones
		/// </summary>
		/// <param name="digit"></param>
		public void SendDtmf(string digit)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Toggles the do not disturb state
		/// </summary>
		public void DoNotDisturbToggle()
		{
			int dndStateInt = !DoNotDisturbState ? 1 : 0;
			Parent.SendText(string.Format("csv {0} {1}", Tags.DoNotDisturbTag, dndStateInt));
		}

		/// <summary>
		/// Sets the do not disturb state on
		/// </summary>
		public void DoNotDisturbOn()
		{
			Parent.SendText(string.Format("csv {0} 1", Tags.DoNotDisturbTag));
		}

		/// <summary>
		/// Sets the do not disturb state off
		/// </summary>
		public void DoNotDisturbOff()
		{
			Parent.SendText(string.Format("csv {0} 0", Tags.DoNotDisturbTag));
		}

		/// <summary>
		/// Toggles the auto answer state
		/// </summary>
		public void AutoAnswerToggle()
		{
			int autoAnswerStateInt = !AutoAnswerState ? 1 : 0;
			Parent.SendText(string.Format("csv {0} {1}", Tags.AutoAnswerTag, autoAnswerStateInt));
		}

		/// <summary>
		/// Sets the auto answer state on
		/// </summary>
		public void AutoAnswerOn()
		{
			Parent.SendText(string.Format("csv {0} 1", Tags.AutoAnswerTag));
		}

		/// <summary>
		/// Sets the auto answer state off
		/// </summary>
		public void AutoAnswerOff()
		{
			Parent.SendText(string.Format("csv {0} 0", Tags.AutoAnswerTag));
		}
	}
}