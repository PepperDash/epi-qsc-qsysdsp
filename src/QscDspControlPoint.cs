using PepperDash.Essentials.Core;

namespace PepperDash.Essentials.Plugins
{
	/// <summary>
	/// Base class for the Q-SYS control points (level controls and so on). It is an Essentials device, like the control
	/// points of the other DSP plugins, so Essentials registers the default Mobile Control messengers for it (a level
	/// control gets the volume messenger at /device/{key}) when the device activates.
	/// </summary>
	public abstract class QscDspControlPoint : EssentialsDevice
	{
		public string LevelInstanceTag { get; set; }
		public string MuteInstanceTag { get; set; }
		public QscDsp Parent { get; private set; }

		public bool IsSubscribed { get; protected set; }

		/// <summary>
        /// Constructor
        /// </summary>
        /// <param name="key">level key</param>
        /// <param name="name">name shown for the control point (the config label)</param>
        /// <param name="levelInstanceTag">level named control/instance tag</param>
        /// <param name="muteInstanceTag">mute named control/instance tag</param>
        /// <param name="parent">parent DSP instance</param>
        protected QscDspControlPoint(string key, string name, string levelInstanceTag, string muteInstanceTag, QscDsp parent)
            : base(key, name)
        {            
            LevelInstanceTag = levelInstanceTag;
            MuteInstanceTag = muteInstanceTag;
            Parent = parent;
        }

        /// <summary>
        /// Initializes the plugin
        /// </summary>
        new virtual public void Initialize()
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

            var cmdToSemd = string.Format("{0} \"{1}\" {2}", cmd, instance, value);

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
		public virtual void SendSubscriptionCommand(string instanceTag)
		{
			// Subscription string format: InstanceTag subscribe attributeCode Index1 customName responseRate
			// Ex: "RoomLevel subscribe level 1 MyRoomLevel 500"

			string cmd;

            cmd = string.Format("cga 1 \"{0}\"", instanceTag);

			Parent.SendLine(cmd);
		}
	}
}