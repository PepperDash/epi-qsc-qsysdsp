using PepperDash.Essentials.Devices.Common.DSP;

namespace PepperDash.Essentials.Plugins.Qsc.Qsys
{
	public abstract class QsysControlPoint : DspControlPoint
	{
		public string LevelInstanceTag { get; set; }
		public string MuteInstanceTag { get; set; }
		public IQsys Parent { get; private set; }

		public bool IsSubscribed { get; protected set; }

		/// <summary>
        /// Constructor
        /// </summary>
        /// <param name="key">level key</param>
        /// <param name="levelInstanceTag">level named control/instance tag</param>
        /// <param name="muteInstanceTag">mute named control/instance tag</param>
        /// <param name="parent">parent DSP instance</param>
        protected QsysControlPoint(string key, string levelInstanceTag, string muteInstanceTag, IQsys parent)
            : base(key)
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
            switch (cmd.Trim())
            {
                case "csv":
                    Parent.SendControlValue(instance, value);
                    break;
                case "csp":
                    Parent.SendControlPosition(instance, value);
                    break;
                case "css":
                    if (value == "++" || value == "--")
                        Parent.SendControlRelative(instance, value == "++");
                    else
                        Parent.SendControlString(instance, value);
                    break;
                case "ct":
                    Parent.TriggerControl(instance);
                    break;
            }
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
		public virtual void SendSubscriptionCommand(string instanceTag)
		{
			Parent.SubscribeControl(instanceTag);
		}
	}
}