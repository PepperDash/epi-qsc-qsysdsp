using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Devices.Common.DSP;

namespace QscQsysDspPlugin
{
    /// <summary>
    /// Child control point for direct Q-Sys component-by-name control over QRC JSON-RPC.
    /// Maps to a specific named component and control within that component in the Q-Sys design.
    /// Example: Component "Router", Control "select.1" → drives router output 1 input selection.
    /// </summary>
    public class QscDspComponentControl : DspControlPoint
    {
        /// <summary>Q-Sys component "Code Name" as defined in the Q-Sys design</summary>
        public string ComponentName { get; private set; }

        /// <summary>Control name within the component (e.g. "select.1", "gain", "mute")</summary>
        public string ControlName { get; private set; }

        public bool HasFeedback { get; private set; }

        /// <summary>"integer" | "string"</summary>
        public string ValueType { get; private set; }

        /// <summary>Integer value feedback (used for router selects and numeric controls)</summary>
        public IntFeedback IntValueFeedback { get; private set; }

        /// <summary>String value feedback (display string returned by the Core)</summary>
        public StringFeedback StringValueFeedback { get; private set; }

        private int _intValue;
        private string _stringValue = string.Empty;

        private readonly QscDsp _parent;
        private readonly string _label;

        public string Name => _label;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="key">unique key for this control point</param>
        /// <param name="config">component control block configuration</param>
        /// <param name="parent">parent QscDsp device</param>
        public QscDspComponentControl(string key, QscDspComponentControlBlockConfig config, QscDsp parent)
            : base(key)
        {
            _parent = parent;
            _label = config.Label ?? key;
            ComponentName = config.ComponentName;
            ControlName = config.ControlName;
            HasFeedback = config.HasFeedback;
            ValueType = config.ValueType ?? "integer";

            IntValueFeedback = new IntFeedback(() => _intValue);
            StringValueFeedback = new StringFeedback(() => _stringValue);

            DeviceManager.AddDevice(this);
        }

        /// <summary>
        /// Sets the component control to an integer value (e.g. router input select)
        /// </summary>
        public void SetValue(int value)
        {
            _parent.SendComponentSet(ComponentName, ControlName, (double)value, 0.0);
        }

        /// <summary>
        /// Sets the component control to a string value
        /// </summary>
        public void SetStringValue(string value)
        {
            _parent.SendComponentSet(ComponentName, ControlName, value);
        }

        /// <summary>
        /// Called by QscDsp when a ChangeGroup.Poll update arrives for this component/control pair
        /// </summary>
        /// <param name="value">numeric value from the Core</param>
        /// <param name="str">display string from the Core</param>
        public void ParseFeedback(double value, string str)
        {
            _intValue = (int)value;
            _stringValue = str ?? string.Empty;
            IntValueFeedback.FireUpdate();
            StringValueFeedback.FireUpdate();
        }
    }
}
