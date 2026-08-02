using System;
using System.Collections.Generic;
using Crestron.SimplSharp;
using Crestron.SimplSharp.Reflection;
using Crestron.SimplSharpPro.DeviceSupport;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PepperDash.Core;
using PepperDash.Core.Logging;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Bridges;
using PepperDash.Essentials.Core.Config;
using PepperDash.Essentials.Core.Devices;

namespace PepperDash.Essentials.Plugins.Qsc.Qsys.RemoteControlProtocol
{
    /// <summary>
    /// Q-SYS Remote Control Protocol (QRC) DSP Device
    /// </summary>
    /// <remarks>
    /// QRC is JSON-RPC 2.0 over TCP (default port 1710); each message is a null-terminated JSON object.
    /// This implementation follows the documented QRC method shapes (Control.Get/Set, Component.Get/Set,
    /// ChangeGroup.*, Snapshot.Load/Save, Logon, NoOp, StatusGet, EngineStatus). The exact shape of the
    /// unsolicited ChangeGroup.Poll push and EngineStatus fields should be validated against a live
    /// Core/QRC trace before production use.
    ///
    /// Tags support two addressing modes, split on '#':
    /// - Named Control: "MyGainControl"
    /// - Component Control: "MyComponent#MyControl"
    /// </remarks>
    public class QsysQrcController : ReconfigurableDevice, IQsys, IDspPresets, IBridgeAdvanced, IOnline, ICommunicationMonitor
    {
        /// <summary>
        /// Communication object
        /// </summary>
        public IBasicCommunication Communication { get; private set; }

        /// <summary>
        /// Gather object
        /// </summary>
        public CommunicationGather PortGather { get; private set; }

        /// <summary>
        /// Communication monitor object
        /// </summary>
        public StatusMonitorBase CommunicationMonitor { get; private set; }

        public Dictionary<string, QsysLevelControl> LevelControlPoints { get; private set; }
        public Dictionary<string, QsysDialer> Dialers { get; set; }
        public Dictionary<string, QsysCamera> Cameras { get; set; }
        public List<QsysPresets> PresetList { get; } = new List<QsysPresets>();

        public Dictionary<string, IKeyName> Presets { get; set; }

        public BoolFeedback IsPrimaryFeedback { get; private set; }
        public BoolFeedback IsActiveFeedback { get; private set; }

        private DeviceConfig _Dc;

        private bool _IsPrimary;
        public bool IsPrimary
        {
            get { return _IsPrimary; }
            private set
            {
                _IsPrimary = value;
                IsPrimaryFeedback.FireUpdate();
            }
        }

        private bool _IsActive;
        public bool IsActive
        {
            get { return _IsActive; }
            private set
            {
                _IsActive = value;
                IsActiveFeedback.FireUpdate();
            }
        }

        private string _username;
        private string _password;
        public string DspName { get; private set; }

        public string AutoTrackingKey { get; set; }

        // Single change group is used for all subscriptions, mirroring the ECP implementation's use of a single group
        private const string ChangeGroupId = "1";
        private int _requestId;

        // Tracks the last known normalized (0-1) position per tag, used to approximate relative ramping
        private readonly Dictionary<string, double> _lastKnownPosition = new Dictionary<string, double>();

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="key">String</param>
        /// <param name="name">String</param>
        /// <param name="comm">IBasicCommunication</param>
        /// <param name="dc">DeviceConfig</param>
        public QsysQrcController(string key, string name, IBasicCommunication comm, DeviceConfig dc)
            : base(dc)
        {
            _Dc = dc;
            this.LogVerbose("Made it to device constructor");

            Communication = comm;
            DspName = name;

            var socket = comm as ISocketStatus;
            if (socket != null)
            {
                socket.ConnectionChange += socket_ConnectionChange;
            }

            // QRC messages are null-terminated JSON-RPC objects, not newline-delimited text
            PortGather = new CommunicationGather(Communication, "\x00");
            PortGather.LineReceived += this.Port_LineReceived;

            // Send a NoOp keepalive on the monitor interval; QRC has no ECP-style "cgp" heartbeat text to watch for
            CommunicationMonitor = new GenericCommunicationMonitor(this, Communication, 20000, 120000, 300000, SendNoOp);

            IsPrimaryFeedback = new BoolFeedback(Key + "-IsPrimaryFeedback", () => IsPrimary);
            IsActiveFeedback = new BoolFeedback(Key + "-IsActiveFeedback", () => IsActive);

            LevelControlPoints = new Dictionary<string, QsysLevelControl>();
            Dialers = new Dictionary<string, QsysDialer>();
            Cameras = new Dictionary<string, QsysCamera>();
            Presets = new Dictionary<string, IKeyName>();
            CreateDspObjects();

            DeviceManager.AllDevicesActivated += (sender, args) =>
            {
                if (comm != null)
                    comm.Connect();
            };
        }

        /// <summary>
        /// CustomActivate Override
        /// </summary>
        public override bool CustomActivate()
        {
            CrestronConsole.AddNewConsoleCommand(SendLine, "send" + Key, "", ConsoleAccessLevelEnum.AccessOperator);
            CrestronConsole.AddNewConsoleCommand(s => Communication.Connect(), "con" + Key, "",
                ConsoleAccessLevelEnum.AccessOperator);
            return true;
        }

        private void socket_ConnectionChange(object sender, GenericSocketStatusChageEventArgs e)
        {
            if (!e.Client.IsConnected)
                return;

            Logon();
            SubscribeToAttributes();
        }

        private string FormatTag(string prefix, string tag)
        {
            if (prefix == null)
                prefix = "";
            if (tag == null)
                return null;
            return string.Format("{0}{1}", prefix, tag);
        }

        public void CreateDspObjects()
        {
            var props = JsonConvert.DeserializeObject<QsysQrcPropertiesConfig>(_Dc.Properties.ToString());

            _username = props.Control.TcpSshProperties.Username;
            _password = props.Control.TcpSshProperties.Password;

            LevelControlPoints.Clear();
            PresetList.Clear();
            Dialers.Clear();
            Cameras.Clear();

            string prefix = props.Prefix ?? "";

            AutoTrackingKey = string.Format("{0}-{1}", Key, "Auto-Tracking");

            LevelControlPoints.Add(AutoTrackingKey, new QsysLevelControl(AutoTrackingKey, new QsysLevelControlBlockConfig
            {
                HasMute = true,
                Label = AutoTrackingKey,
                MuteInstanceTag = "CAM_TRACK" //todo make configurable
            }, this));

            if (props.LevelControlBlocks != null)
            {
                foreach (KeyValuePair<string, QsysLevelControlBlockConfig> block in props.LevelControlBlocks)
                {
                    string key = string.Format("{0}-{1}{2}", Key, prefix, block.Key);
                    var value = block.Value;
                    value.LevelInstanceTag = FormatTag(prefix, value.LevelInstanceTag);
                    value.MuteInstanceTag = FormatTag(prefix, value.MuteInstanceTag);

                    LevelControlPoints.Add(key, new QsysLevelControl(key, value, this));
                    this.LogVerbose("Added LevelControlPoint {0} LevelTag: {1} MuteTag: {2}", key,
                        value.LevelInstanceTag, value.MuteInstanceTag);
                }
            }
            if (props.Presets != null)
            {
                foreach (KeyValuePair<string, QsysPresets> preset in props.Presets)
                {
                    var value = preset.Value;
                    var qsysPreset = new QsysPreset(preset.Key)
                    {
                        Label = value.Label,
                        Bank = value.Bank,
                        Preset = value.Preset,
                        Number = value.Number,
                        LabelFeedback = value.LabelFeedback
                    };
                    value.Preset = string.Format("{0}{1}", prefix, value.Preset);
                    AddPreset(value);
                    Presets.Add(preset.Key, qsysPreset);
                    this.LogVerbose("Added Preset {0} {1}", value.Label, value.Preset);
                }
            }
            if (props.CameraControlBlocks != null)
            {
                foreach (KeyValuePair<string, QsysCameraConfig> camera in props.CameraControlBlocks)
                {
                    var value = camera.Value;
                    var key = camera.Key;

                    value.PanLeftTag = FormatTag(prefix, value.PanLeftTag);
                    value.PanRightTag = FormatTag(prefix, value.PanRightTag);
                    value.TiltUpTag = FormatTag(prefix, value.TiltUpTag);
                    value.TiltDownTag = FormatTag(prefix, value.TiltDownTag);
                    value.ZoomInTag = FormatTag(prefix, value.ZoomInTag);
                    value.ZoomOutTag = FormatTag(prefix, value.ZoomOutTag);
                    value.PresetBankTag = FormatTag(prefix, value.PresetBankTag);
                    value.Privacy = FormatTag(prefix, value.Privacy);
                    value.OnlineStatus = FormatTag(prefix, value.OnlineStatus);
                    foreach (var preset in value.Presets)
                    {
                        value.Presets[preset.Key].Bank = FormatTag(prefix, value.Presets[preset.Key].Bank);
                    }

                    Cameras.Add(key, new QsysCamera(this, key, key, value));
                    this.LogVerbose("Added Camera {0}\n {1}", key, value);
                }
            }
            if (props.DialerControlBlocks != null)
            {
                foreach (KeyValuePair<string, QscDialerConfig> dialerConfig in props.DialerControlBlocks)
                {
                    var value = dialerConfig.Value;
                    var key = string.Format("{0}{1}", prefix, dialerConfig.Key);
                    value.AutoAnswerTag = FormatTag(prefix, value.AutoAnswerTag);
                    value.CallStatusTag = FormatTag(prefix, value.CallStatusTag);
                    value.ConnectTag = FormatTag(prefix, value.ConnectTag);
                    value.DialStringTag = FormatTag(prefix, value.DialStringTag);
                    value.DisconnectTag = FormatTag(prefix, value.DisconnectTag);
                    value.DoNotDisturbTag = FormatTag(prefix, value.DoNotDisturbTag);
                    value.HookStatusTag = FormatTag(prefix, value.HookStatusTag);
                    value.IncomingCallRingerTag = FormatTag(prefix, value.IncomingCallRingerTag);
                    value.Keypad0Tag = FormatTag(prefix, value.Keypad0Tag);
                    value.Keypad1Tag = FormatTag(prefix, value.Keypad1Tag);
                    value.Keypad2Tag = FormatTag(prefix, value.Keypad2Tag);
                    value.Keypad3Tag = FormatTag(prefix, value.Keypad3Tag);
                    value.Keypad4Tag = FormatTag(prefix, value.Keypad4Tag);
                    value.Keypad5Tag = FormatTag(prefix, value.Keypad5Tag);
                    value.Keypad6Tag = FormatTag(prefix, value.Keypad6Tag);
                    value.Keypad7Tag = FormatTag(prefix, value.Keypad7Tag);
                    value.Keypad8Tag = FormatTag(prefix, value.Keypad8Tag);
                    value.Keypad9Tag = FormatTag(prefix, value.Keypad9Tag);
                    value.KeypadBackspaceTag = FormatTag(prefix, value.KeypadBackspaceTag);
                    value.KeypadClearTag = FormatTag(prefix, value.KeypadClearTag);
                    value.KeypadPoundTag = FormatTag(prefix, value.KeypadPoundTag);
                    value.KeypadStarTag = FormatTag(prefix, value.KeypadStarTag);
                    Dialers.Add(key, new QsysDialer(key, value, this));
                    this.LogVerbose("Added Dialer {0}\n {1}", key, value);
                }
            }
        }

        protected override void CustomSetConfig(DeviceConfig config)
        {
            ConfigWriter.UpdateDeviceConfig(config);
        }

        /// <summary>
        /// Sets the IP address used by the plugin
        /// </summary>
        public void SetIpAddress(string hostname)
        {
            try
            {
                if (hostname.Length > 2 &&
                    _Dc.Properties["control"]["tcpSshProperties"]["address"].ToString() != hostname)
                {
                    this.LogVerbose("Changing IPAddress: {0}", hostname);
                    Communication.Disconnect();

                    (Communication as GenericTcpIpClient).Hostname = hostname;

                    _Dc.Properties["control"]["tcpSshProperties"]["address"] = hostname;
                    CustomSetConfig(_Dc);
                    Communication.Connect();
                }
            }
            catch (Exception e)
            {
                this.LogVerbose(e, "Error SetIpAddress");
            }
        }

        /// <summary>
        /// Sets the DSP prefix
        /// </summary>
        public void SetPrefix(string prefix)
        {
            if (_Dc.Properties["prefix"].ToString() != prefix && prefix.Length > 0)
            {
                _Dc.Properties["prefix"] = prefix;
                CustomSetConfig(_Dc);
                this.LogInformation(
                    "The Dsp Prefix has changed to {0} the program will automaticly restart in 60 seconds", prefix);
                string notUsed = "";
                new CTimer(
                    (object notused) =>
                    {
                        CrestronConsole.SendControlSystemCommand(
                            string.Format("progres -p:{0}", Global.ControlSystem.ProgramNumber), ref notUsed);
                    },
                    60000);
            }
        }

        /// <summary>
        /// Issues a StatusGet request to the Core
        /// </summary>
        public void StatusGet(bool enable)
        {
            if (enable) SendRequest("StatusGet", 0);
        }

        /// <summary>
        /// Writes the config
        /// </summary>
        public void WriteConfig()
        {
            CustomSetConfig(_Dc);
        }

        private void SendNoOp()
        {
            SendRequest("NoOp", new JObject());
        }

        private void Logon()
        {
            if (string.IsNullOrEmpty(_username) && string.IsNullOrEmpty(_password))
                return;

            SendRequest("Logon", JToken.FromObject(new { User = _username, Password = _password }));
        }

        /// <summary>
        /// Initiates the subscription process to the Core
        /// </summary>
        private void SubscribeToAttributes()
        {
            foreach (var level in LevelControlPoints)
            {
                level.Value.Subscribe();
            }

            foreach (var dialer in Dialers)
            {
                dialer.Value.Subscribe();
            }

            foreach (var camera in Cameras)
            {
                camera.Value.Subscribe();
            }

            // Push updates automatically once a second instead of the client polling the change group
            SendRequest("ChangeGroup.AutoPoll", JToken.FromObject(new { Id = ChangeGroupId, Rate = 1 }));

            if (CommunicationMonitor != null)
            {
                CommunicationMonitor.Start();
            }
        }

        /// <summary>
        /// Splits a tag on '#' into Component/Control names for Component Control addressing.
        /// A tag with no '#' is treated as a flat Named Control.
        /// </summary>
        private static bool TryParseComponentTag(string tag, out string component, out string control)
        {
            var parts = tag.Split('#');
            if (parts.Length == 2)
            {
                component = parts[0];
                control = parts[1];
                return true;
            }

            component = null;
            control = tag;
            return false;
        }

        #region IQsys Members

        /// <summary>
        /// Sets a named control to an absolute value
        /// </summary>
        public void SendControlValue(string tag, string value)
        {
            SendControl(tag, "Value", ParseJsonValue(value));
        }

        /// <summary>
        /// Sets a named control to a normalized 0-1 position
        /// </summary>
        public void SendControlPosition(string tag, string value)
        {
            double position;
            if (double.TryParse(value, out position))
                _lastKnownPosition[tag] = position;

            SendControl(tag, "Position", ParseJsonValue(value));
        }

        /// <summary>
        /// Ramps a named control up or down. QRC has no native relative-ramp message, so this nudges a
        /// locally-tracked normalized position and sends it as an absolute Position update.
        /// </summary>
        public void SendControlRelative(string tag, bool increase)
        {
            double current;
            if (!_lastKnownPosition.TryGetValue(tag, out current))
                current = 0.5;

            const double step = 0.05;
            var next = increase ? Math.Min(1.0, current + step) : Math.Max(0.0, current - step);
            _lastKnownPosition[tag] = next;

            SendControlPosition(tag, next.ToString("0.####"));
        }

        /// <summary>
        /// Sets a named control to a string value
        /// </summary>
        public void SendControlString(string tag, string value)
        {
            SendControl(tag, "Value", value);
        }

        /// <summary>
        /// Triggers a momentary named control
        /// </summary>
        public void TriggerControl(string tag)
        {
            SendControl(tag, "Value", 1);
        }

        /// <summary>
        /// Requests the current value of a named control
        /// </summary>
        public void GetControl(string tag)
        {
            string component, control;
            if (TryParseComponentTag(tag, out component, out control))
            {
                SendRequest("Component.Get", JToken.FromObject(new
                {
                    Name = component,
                    Controls = new[] { new { Name = control } }
                }));
            }
            else
            {
                SendRequest("Control.Get", JToken.FromObject(new[] { tag }));
            }
        }

        /// <summary>
        /// Subscribes to change notifications for a named control
        /// </summary>
        public void SubscribeControl(string tag)
        {
            string component, control;
            if (TryParseComponentTag(tag, out component, out control))
            {
                SendRequest("ChangeGroup.AddComponentControl", JToken.FromObject(new
                {
                    Id = ChangeGroupId,
                    Component = new { Name = component, Controls = new[] { new { Name = control } } }
                }));
            }
            else
            {
                SendRequest("ChangeGroup.AddControl", JToken.FromObject(new
                {
                    Id = ChangeGroupId,
                    Controls = new[] { tag }
                }));
            }
        }

        /// <summary>
        /// Recalls a snapshot bank/number (e.g. camera presets)
        /// </summary>
        public void RecallSnapshot(string bank, string number, string rampTime)
        {
            int bankNumber;
            int.TryParse(number, out bankNumber);
            double ramp;
            double.TryParse(rampTime, out ramp);

            SendRequest("Snapshot.Load", JToken.FromObject(new { Name = bank, Bank = bankNumber, RampTime = ramp }));
        }

        /// <summary>
        /// Saves a snapshot bank/number (e.g. camera presets)
        /// </summary>
        public void SaveSnapshot(string bank, string number)
        {
            int bankNumber;
            int.TryParse(number, out bankNumber);

            SendRequest("Snapshot.Save", JToken.FromObject(new { Name = bank, Bank = bankNumber }));
        }

        #endregion

        private void SendControl(string tag, string valueField, object value)
        {
            string component, control;
            if (TryParseComponentTag(tag, out component, out control))
            {
                var controlObj = new JObject();
                controlObj["Name"] = control;
                controlObj[valueField] = JToken.FromObject(value);

                var controlsArray = new JArray();
                controlsArray.Add(controlObj);

                var componentParams = new JObject();
                componentParams["Name"] = component;
                componentParams["Controls"] = controlsArray;

                SendRequest("Component.Set", componentParams);
            }
            else
            {
                var paramsObj = new JObject();
                paramsObj["Name"] = tag;
                paramsObj[valueField] = JToken.FromObject(value);

                SendRequest("Control.Set", paramsObj);
            }
        }

        private static object ParseJsonValue(string value)
        {
            double num;
            if (double.TryParse(value, out num))
                return num;
            return value;
        }

        private void SendRequest(string method, JToken paramsToken)
        {
            var id = Crestron.SimplSharp.Interlocked.Increment(ref _requestId);
            var request = new JObject();
            request["jsonrpc"] = "2.0";
            request["method"] = method;
            request["params"] = paramsToken;
            request["id"] = id;

            SendLine(request.ToString(Formatting.None));
        }

        /// <summary>
        /// Sends a raw, already-formed JSON-RPC message to the Core (with null terminator appended)
        /// </summary>
        public void SendLine(string s)
        {
            Communication.SendText(s + "\x00");
        }

        public void ProcessSimulatedRx(string s)
        {
            var args = new GenericCommMethodReceiveTextArgs(s);
            Port_LineReceived(this, args);
        }

        /// <summary>
        /// Handles a response/notification message from the Core
        /// </summary>
        private void Port_LineReceived(object dev, GenericCommMethodReceiveTextArgs args)
        {
            try
            {
                var json = JObject.Parse(args.Text);
                var method = json["method"] != null ? json["method"].ToString() : null;

                if (method == "EngineStatus")
                {
                    ProcessEngineStatus(json["params"] as JObject);
                    return;
                }

                if (method == "ChangeGroup.Poll" || method == "ChangeGroup.Invalidate")
                {
                    ProcessChangeGroupNotification(json["params"] as JObject);
                    return;
                }

                var result = json["result"];
                if (result != null)
                {
                    ProcessResult(result);
                    return;
                }

                var error = json["error"];
                if (error != null)
                {
                    this.LogWarning("QRC error response: {0}", error.ToString(Formatting.None));
                }
            }
            catch (Exception e)
            {
                this.LogVerbose(e, "Port_LineReceived exception processing '{0}'", args.Text);
            }
        }

        private void ProcessEngineStatus(JObject status)
        {
            if (status == null) return;

            this.LogVerbose("EngineStatus: {0}", status.ToString(Formatting.None));

            var isRedundant = status["IsRedundant"];
            var state = status["State"] != null ? status["State"].ToString() : null;

            IsPrimary = isRedundant == null || !isRedundant.Value<bool>();
            IsActive = state == null || state.Equals("Active", StringComparison.OrdinalIgnoreCase);
        }

        private void ProcessChangeGroupNotification(JObject changeGroupParams)
        {
            var changes = changeGroupParams != null ? changeGroupParams["Changes"] as JArray : null;
            if (changes == null) return;

            foreach (var change in changes)
            {
                try
                {
                    // Each change already carries its own "Component" field (when applicable), e.g.
                    // {"Component":"av-tr-gain-room-stereo","Name":"gain","String":"-67.4dB","Value":-67.35912322,"Position":0.27200731}
                    ProcessControlUpdate(change, null);
                }
                catch (Exception e)
                {
                    this.LogVerbose(e, "Error processing ChangeGroup.Poll entry '{0}'", change.ToString(Formatting.None));
                }
            }
        }

        private void ProcessResult(JToken result)
        {
            var array = result as JArray;
            if (array != null)
            {
                // Flat Control.Get response: [{"Name":tag,"Value":...,"String":...,"Position":...}, ...]
                foreach (var item in array)
                {
                    try
                    {
                        ProcessControlUpdate(item, null);
                    }
                    catch (Exception e)
                    {
                        this.LogVerbose(e, "Error processing Control.Get result entry '{0}'", item.ToString(Formatting.None));
                    }
                }
                return;
            }

            var obj = result as JObject;
            if (obj == null) return;

            var controls = obj["Controls"] as JArray;
            if (controls != null)
            {
                // Component.Get response: {"Name":component,"Controls":[{"Name":ctrl,...}, ...]} - the outer
                // Name is the component; nested control entries don't repeat it, so it's passed explicitly.
                var componentName = obj["Name"] != null ? obj["Name"].ToString() : null;
                foreach (var item in controls)
                {
                    try
                    {
                        ProcessControlUpdate(item, componentName);
                    }
                    catch (Exception e)
                    {
                        this.LogVerbose(e, "Error processing Component.Get result entry '{0}'", item.ToString(Formatting.None));
                    }
                }
                return;
            }

            if (obj["Name"] != null)
                ProcessControlUpdate(obj, null);
        }

        private void ProcessControlUpdate(JToken controlToken, string componentNameOverride)
        {
            var name = controlToken["Name"] != null ? controlToken["Name"].ToString() : null;
            if (string.IsNullOrEmpty(name)) return;

            // Component Control changes report "Component" and "Name" separately; reconstruct the
            // "Component#Control" tag so it matches the configured instance tag.
            var component = componentNameOverride ??
                (controlToken["Component"] != null ? controlToken["Component"].ToString() : null);
            var customName = component != null ? component + "#" + name : name;

            var rawValue = controlToken["Value"] != null ? controlToken["Value"].ToString() : null;
            var rawString = controlToken["String"] != null ? controlToken["String"].ToString() : null;
            var position = controlToken["Position"] != null ? controlToken["Position"].ToString() : null;

            if (position != null)
            {
                double positionValue;
                if (double.TryParse(position, out positionValue))
                    _lastKnownPosition[customName] = positionValue;
            }

            DispatchControlUpdate(customName, rawString, rawValue, position);
        }

        /// <summary>
        /// Dispatches a control update to whichever level/dialer/camera owns the matching instance tag,
        /// mirroring the ECP implementation's customName matching in Port_LineReceived. Mute (Boolean)
        /// controls are text-matched from String/Value; Level (Float) controls use the normalized Position
        /// for percent-based feedback, or the raw Value for controls configured with useAbsoluteValue.
        /// </summary>
        private void DispatchControlUpdate(string customName, string stringValue, string rawValue, string position)
        {
            foreach (var controlPoint in LevelControlPoints)
            {
                if (customName == controlPoint.Value.LevelInstanceTag)
                {
                    controlPoint.Value.ParseSubscriptionMessage(customName, position ?? rawValue, rawValue);
                    return;
                }

                if (customName == controlPoint.Value.MuteInstanceTag)
                {
                    controlPoint.Value.ParseSubscriptionMessage(customName, ToMuteStateText(stringValue, rawValue), null);
                    return;
                }
            }

            foreach (var dialer in Dialers)
            {
                PropertyInfo[] properties = dialer.Value.Tags.GetType().GetCType().GetProperties();
                foreach (var prop in properties)
                {
                    var propValue = prop.GetValue(dialer.Value.Tags, null) as string;
                    if (customName == propValue)
                    {
                        dialer.Value.ParseSubscriptionMessage(customName, stringValue ?? rawValue);
                        return;
                    }
                }
            }

            foreach (var camera in Cameras)
            {
                if (customName == camera.Value.Config.OnlineStatus)
                {
                    camera.Value.ParseSubscriptionMessage(customName, stringValue ?? rawValue, null);
                    return;
                }
            }
        }

        /// <summary>
        /// Falls back to deriving a "true"/"false" state from a raw numeric Value when a control has no
        /// human-readable String representation.
        /// </summary>
        private static string ToMuteStateText(string stringValue, string rawValue)
        {
            if (!string.IsNullOrEmpty(stringValue))
                return stringValue;

            double numeric;
            if (rawValue != null && double.TryParse(rawValue, out numeric))
                return numeric != 0 ? "true" : "false";

            return rawValue;
        }

        /// <summary>
        /// Adds a preset
        /// </summary>
        public void AddPreset(QsysPresets s)
        {
            PresetList.Add(s);
        }

        /// <summary>
        /// Runs the preset with the number provided
        /// </summary>
        public void RunPresetNumber(ushort n)
        {
            var preset = PresetList[n];
            if (string.IsNullOrEmpty(preset.Preset))
            {
                this.LogError("Cannot recall preset at index {0}: preset name is not defined", n);
                return;
            }
            RunPreset(preset.Preset);
        }

        /// <summary>
        /// Recalls a preset. The preset name is expected in "BANK NUMBER" format, matching the ECP convention.
        /// </summary>
        public void RunPreset(string name)
        {
            var parts = name.Split(' ');
            if (parts.Length < 2)
            {
                this.LogError("Cannot recall preset '{0}': expected 'BANK NUMBER' format", name);
                return;
            }
            RecallSnapshot(parts[0], parts[1], "0");
        }

        public void RecallPreset(string key)
        {
            if (!Presets.ContainsKey(key))
                return;
            var preset = Presets[key] as QsysPreset;
            if (preset == null) return;

            this.LogInformation("Running preset {0}", preset.Label);
            if (string.IsNullOrEmpty(preset.Preset))
            {
                this.LogInformation("Preset {0} is not valid", preset.Label);
                return;
            }
            RunPreset(preset.Preset);
        }

        /// <summary>
        /// Saves the preset with the number provided
        /// </summary>
        public void SavePresetNumber(ushort n)
        {
            var preset = PresetList[n];
            if (string.IsNullOrEmpty(preset.Preset))
            {
                this.LogError("Cannot save preset at index {0}: preset name is not defined", n);
                return;
            }
            var cmd = preset.Preset.Split(' ');
            if (cmd.Length < 2)
            {
                this.LogError("Cannot save preset at index {0}: preset name '{1}' is not in the expected 'BANK NUMBER' format", n, preset.Preset);
                return;
            }
            SavePreset(string.Format("{0} {1}", cmd[0], cmd[1]));
        }

        /// <summary>
        /// Saves a preset. The preset name is expected in "BANK NUMBER" format, matching the ECP convention.
        /// </summary>
        public void SavePreset(string name)
        {
            var parts = name.Split(' ');
            if (parts.Length < 2)
            {
                this.LogError("Cannot save preset '{0}': expected 'BANK NUMBER' format", name);
                return;
            }
            SaveSnapshot(parts[0], parts[1]);
        }

        public BoolFeedback IsOnline
        {
            get { return CommunicationMonitor.IsOnlineFeedback; }
        }

        #region IBridgeAdvanced Members

        /// <summary>
        /// Link to API
        /// </summary>
        public void LinkToApi(BasicTriList trilist, uint joinStart, string joinMapKey, EiscApiAdvanced bridge)
        {
            this.LinkToApiExt(trilist, joinStart, joinMapKey, bridge);
        }

        #endregion
    }
}
