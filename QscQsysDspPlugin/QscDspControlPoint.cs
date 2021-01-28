using System;
using Crestron.SimplSharp;
using PepperDash.Essentials.Devices.Common.DSP;

namespace QscQsysDspPlugin
{
	public abstract class QscDspControlPoint : DspControlPoint
	{
		public string Key { get; protected set; }

		public string LevelInstanceTag { get; set; }
		public string MuteInstanceTag { get; set; }
		public QscDsp Parent { get; private set; }

		public bool IsSubscribed { get; protected set; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="levelInstanceTag">level named control/instance tag</param>
		/// <param name="muteInstanceTag">mute named control/instance tag</param>
		/// <param name="parent">parent DSP instance</param>
		protected QscDspControlPoint(string levelInstanceTag, string muteInstanceTag, QscDsp parent)
		{
			LevelInstanceTag = levelInstanceTag;
			MuteInstanceTag = muteInstanceTag;
			Parent = parent; 
		}

		/// <summary>
		/// Initializes the plugin
		/// </summary>
		virtual public void Initialize()
		{
		}

		/// <summary>
		/// Sends a command to the DSP
		/// </summary>
		/// <param name="cmd">command</param>
		/// <param name="instance">named control/instance tag</param>
		/// <param name="value">value (use "" if not applicable)</param>
		public virtual void SendFullCommand(string cmd, string instance, string value)
		{

			var cmdToSemd = string.Format("{0} {1} {2}", cmd, instance, value);

			Parent.SendLine(cmdToSemd);

		}

		/// <summary>
		/// Parses get messgae return
		/// </summary>
		/// <param name="attributeCode">attributte code</param>
		/// <param name="message">message</param>
		virtual public void ParseGetMessage(string attributeCode, string message)
		{
		}


		/// <summary>
		/// Sends the subscription command of the instance tag for the provided change group
		/// </summary>
		/// <param name="instanceTag">named control/instance tag</param>
		/// <param name="changeGroup">change group</param>
		public virtual void SendSubscriptionCommand(string instanceTag, string changeGroup)
		{
			// Subscription string format: InstanceTag subscribe attributeCode Index1 customName responseRate
			// Ex: "RoomLevel subscribe level 1 MyRoomLevel 500"

			string cmd;

			cmd = string.Format("cga {0} {1}", changeGroup, instanceTag);

			Parent.SendLine(cmd);
		}
	}
}