using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace QscQsysDspPlugin
{
    /// <summary>
    /// JSON-RPC 2.0 request envelope for QRC protocol (TCP 1710)
    /// </summary>
    public class QrcRequest
    {
        [JsonProperty("jsonrpc")]
        public string JsonRpc { get; set; } = "2.0";

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("method")]
        public string Method { get; set; }

        [JsonProperty("params")]
        public object Params { get; set; }
    }

    /// <summary>
    /// JSON-RPC 2.0 response/notification envelope for QRC protocol
    /// </summary>
    public class QrcResponse
    {
        [JsonProperty("jsonrpc")]
        public string JsonRpc { get; set; }

        /// <summary>Null for unsolicited notifications (EngineStatus, ChangeGroup.Poll)</summary>
        [JsonProperty("id")]
        public int? Id { get; set; }

        /// <summary>Present on notifications (no id)</summary>
        [JsonProperty("method")]
        public string Method { get; set; }

        [JsonProperty("result")]
        public JToken Result { get; set; }

        [JsonProperty("error")]
        public QrcError Error { get; set; }

        /// <summary>Present on notifications</summary>
        [JsonProperty("params")]
        public JToken Params { get; set; }
    }

    public class QrcError
    {
        [JsonProperty("code")]
        public int Code { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }
    }

    /// <summary>
    /// A single named-control set item used in Control.Set params
    /// </summary>
    public class QrcControlSetItem
    {
        [JsonProperty("Name")]
        public string Name { get; set; }

        [JsonProperty("Value", NullValueHandling = NullValueHandling.Ignore)]
        public double? Value { get; set; }

        [JsonProperty("Position", NullValueHandling = NullValueHandling.Ignore)]
        public double? Position { get; set; }

        [JsonProperty("String", NullValueHandling = NullValueHandling.Ignore)]
        public string StringValue { get; set; }

        [JsonProperty("Ramp", NullValueHandling = NullValueHandling.Ignore)]
        public double? Ramp { get; set; }
    }

    /// <summary>
    /// A single component control item used in Component.Set params
    /// </summary>
    public class QrcComponentControlSetItem
    {
        [JsonProperty("Name")]
        public string Name { get; set; }

        [JsonProperty("Value", NullValueHandling = NullValueHandling.Ignore)]
        public double? Value { get; set; }

        [JsonProperty("String", NullValueHandling = NullValueHandling.Ignore)]
        public string StringValue { get; set; }

        [JsonProperty("Ramp", NullValueHandling = NullValueHandling.Ignore)]
        public double? Ramp { get; set; }
    }

    /// <summary>
    /// A change value entry in a ChangeGroup.Poll notification
    /// </summary>
    public class QrcChangeValue
    {
        /// <summary>The control name (Named Control or component control name)</summary>
        [JsonProperty("Name")]
        public string Name { get; set; }

        /// <summary>Present for component-control changes; null for Named Control changes</summary>
        [JsonProperty("Component")]
        public string Component { get; set; }

        /// <summary>Absolute value (dB for audio, integer for selects, 1/0 for boolean)</summary>
        [JsonProperty("Value")]
        public double Value { get; set; }

        /// <summary>Display string (e.g. "-10.0 dB", "true", "3")</summary>
        [JsonProperty("String")]
        public string StringValue { get; set; }

        /// <summary>Normalized position 0.0-1.0</summary>
        [JsonProperty("Position")]
        public double Position { get; set; }
    }

    /// <summary>
    /// Params object of a ChangeGroup.Poll notification
    /// </summary>
    public class QrcChangeGroupPollParams
    {
        [JsonProperty("Id")]
        public string Id { get; set; }

        [JsonProperty("Changes")]
        public List<QrcChangeValue> Changes { get; set; }
    }

    /// <summary>
    /// Params object of an EngineStatus notification or StatusGet result
    /// </summary>
    public class QrcEngineStatusParams
    {
        [JsonProperty("Platform")]
        public string Platform { get; set; }

        /// <summary>"Active", "Standby", "Emulating", "Acquiring", "Initializing", "Idle"</summary>
        [JsonProperty("State")]
        public string State { get; set; }

        [JsonProperty("IsRedundant")]
        public bool IsRedundant { get; set; }

        [JsonProperty("IsEmulator")]
        public bool IsEmulator { get; set; }

        [JsonProperty("DesignName")]
        public string DesignName { get; set; }
    }
}
