using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using PepperDash.Essentials.Devices.Common.DSP;

namespace QscQsysDspPlugin
{
	/// <summary>
	/// Control Point Class
	/// </summary>
	public abstract class QscQsysDspControlPoint : DspControlPoint
	{
		/// <summary>
		/// Control point key name
		/// </summary>
		public string Key { get; protected set; }

		/// <summary>
		/// Control point level instance tag
		/// </summary>
		public string LevelInstanceTag { get; set; }

		/// <summary>
		/// Control point mute instance tag
		/// </summary>
		public string MuteInstanceTag { get; set; }

		/// <summary>
		/// Control point parent device
		/// </summary>
		public QscQsysDsp Parent { get; private set; }

		/// <summary>
		/// Control point IsSusbscribed flag
		/// </summary>
		public bool IsSubscribed { get; protected set; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="levelInstanceTag">level named control/instance tag</param>
		/// <param name="muteInstanceTag">mute named control/instance tag</param>
		/// <param name="parent">parent DSP instance</param>
		protected QscQsysDspControlPoint(string levelInstanceTag, string muteInstanceTag, QscQsysDsp parent)
		{
			LevelInstanceTag = levelInstanceTag;
			MuteInstanceTag = muteInstanceTag;
			Parent = parent;
		}

		/// <summary>
		/// Initializes the plugin
		/// </summary>
		public virtual void Initialize()
		{
		}

		/// <summary>
		/// Sends command to the DSP
		/// </summary>
		/// <param name="cmd">command</param>
		/// <param name="instance">named control/instance tag</param>
		/// <param name="value">value (use "" if not applicable)</param>
		public virtual void SendFullCommand(string cmd, string instance, string value)
		{
			var cmdToSend = string.Format("{0} {1} {2}", cmd, instance, value);
			Parent.SendText(cmdToSend);
		}

		/// <summary>
		/// Parses get message return
		/// </summary>
		/// <param name="attributeCode">attribute code</param>
		/// <param name="message">message</param>
		public virtual void ParseGetMessage(string attributeCode, string message)
		{
		}

		/// <summary>
		/// Sends the subscription command of the instance tag for the provided change group
		/// </summary>
		/// <param name="instanceTag">named control/instance tag</param>
		/// <param name="changeGroup">change group</param>
		public virtual void SendSubscriptionCommand(string instanceTag, string changeGroup)
		{
			// Subscription string format: InstanceTag subscribe attributeCode Index 1 customName responseRate
			// Ex: "RoomLevel subscribe level 1 MyRoomLevel 500"

			var cmdToSend = string.Format("cga {0} {1}", changeGroup, instanceTag);
			Parent.SendText(cmdToSend);
		}
	}
}