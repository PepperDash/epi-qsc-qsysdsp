using System.Collections.Generic;
using Newtonsoft.Json;

namespace PepperDash.Essentials.Plugins.Qsc.Qsys
{
    /// <summary>
    /// A single Properties entry on a Component.GetComponents result
    /// </summary>
    public class QsysComponentProperty
    {
        [JsonProperty("Name")]
        public string Name { get; set; }

        [JsonProperty("Value")]
        public string Value { get; set; }
    }

    /// <summary>
    /// A component as returned by Component.GetComponents, without its controls
    /// </summary>
    public class QsysComponent
    {
        [JsonProperty("Name")]
        public string Name { get; set; }

        [JsonProperty("Type")]
        public string Type { get; set; }

        [JsonProperty("Properties")]
        public List<QsysComponentProperty> Properties { get; set; }
    }

    /// <summary>
    /// A single control as returned by Component.GetControls
    /// </summary>
    public class QsysControlInfo
    {
        [JsonProperty("Name")]
        public string Name { get; set; }

        [JsonProperty("Type")]
        public string Type { get; set; }

        [JsonProperty("Value")]
        public object Value { get; set; }

        [JsonProperty("String")]
        public string String { get; set; }

        [JsonProperty("Position")]
        public double? Position { get; set; }

        [JsonProperty("Direction")]
        public string Direction { get; set; }

        [JsonProperty("ValueMin")]
        public double? ValueMin { get; set; }

        [JsonProperty("ValueMax")]
        public double? ValueMax { get; set; }

        [JsonProperty("StringMin")]
        public string StringMin { get; set; }

        [JsonProperty("StringMax")]
        public string StringMax { get; set; }

        /// <summary>
        /// "ComponentName#ControlName", ready to paste into a levelInstanceTag/muteInstanceTag config field
        /// </summary>
        [JsonProperty("SuggestedTag")]
        public string SuggestedTag { get; set; }
    }

    /// <summary>
    /// A component and its full set of controls, as written to the discovery output file
    /// </summary>
    public class QsysComponentWithControls
    {
        [JsonProperty("Name")]
        public string Name { get; set; }

        [JsonProperty("Type")]
        public string Type { get; set; }

        [JsonProperty("Controls")]
        public List<QsysControlInfo> Controls { get; set; }
    }
}
