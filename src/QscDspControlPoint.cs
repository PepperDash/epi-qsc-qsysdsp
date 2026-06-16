using PepperDash.Essentials.Devices.Common.DSP;

namespace QscQsysDspPlugin
{
	public abstract class QscDspControlPoint : DspControlPoint
	{
		public string LevelInstanceTag { get; set; }
		public string MuteInstanceTag { get; set; }
		public QscDsp Parent { get; private set; }

		public bool IsSubscribed { get; protected set; }

		/// <summary>
        /// Constructor
        /// </summary>
        /// <param name="key">level key</param>
        /// <param name="levelInstanceTag">level named control/instance tag</param>
        /// <param name="muteInstanceTag">mute named control/instance tag</param>
        /// <param name="parent">parent DSP instance</param>
        protected QscDspControlPoint(string key, string levelInstanceTag, string muteInstanceTag, QscDsp parent)
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
		/// Sends a control command to the DSP via QRC JSON-RPC.
		/// Supported ECP-style cmd tokens: csv (set value), csp (set position), css (set string/ramp), ct (trigger), cg (get)
		/// </summary>
		/// <param name="cmd">legacy ECP command token (csv/csp/css/ct/cg)</param>
		/// <param name="instance">named control tag</param>
		/// <param name="value">value string</param>
		public virtual void SendFullCommand(string cmd, string instance, string value)
		{
			if (string.IsNullOrEmpty(instance)) return;

			double numVal;
			switch (cmd.Trim())
			{
				case "csv":
					// Set by absolute value: csv "tag" 0|1|-10.5
					if (double.TryParse(value, out numVal))
						Parent.SendControlSetValue(instance, numVal);
					break;

				case "csp":
					// Set by position (0.0-1.0): csp "tag" 0.7
					if (double.TryParse(value, out numVal))
						Parent.SendControlSetPosition(instance, numVal);
					break;

				case "css":
					// String set or step-ramp token: css "tag" "dialstring" | ++ | --
					// ++ and -- are handled by callers that compute position steps directly;
					// plain string values (e.g. dial number) are forwarded as QRC String set
					if (value != "++" && value != "--")
						Parent.SendControlSetString(instance, value);
					break;

				case "ct":
					// Trigger (momentary pulse): ct "tag"
					Parent.SendControlTrigger(instance);
					break;

				case "cg":
					// Get current value: cg "tag"
					Parent.SendControlGet(instance);
					break;
			}
		}

		/// <summary>
		/// Parses get message return (override in subclasses as needed)
		/// </summary>
		/// <param name="attributeCode">attribute code</param>
		/// <param name="message">message</param>
		virtual public void ParseGetMessage(string attributeCode, string message)
		{
		}

		/// <summary>
		/// Registers this named control tag with the parent's QRC change group so it receives feedback via ChangeGroup.Poll.
		/// The parent will batch all registered tags into a single ChangeGroup.AddControl call.
		/// </summary>
		/// <param name="instanceTag">named control tag to subscribe</param>
		public virtual void SendSubscriptionCommand(string instanceTag)
		{
			Parent.AddControlToChangeGroup(instanceTag);
		}
	}
}