using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
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

namespace QscQsysDspPlugin
{
    public class QscDsp : ReconfigurableDevice, IDspPresets, IBridgeAdvanced, IOnline, ICommunicationMonitor
    {
        public IBasicCommunication Communication { get; private set; }
        public CommunicationGather PortGather { get; private set; }
        public StatusMonitorBase CommunicationMonitor { get; private set; }

        public Dictionary<string, QscDspLevelControl> LevelControlPoints { get; private set; }
        public Dictionary<string, QscDspDialer> Dialers { get; set; }
        public Dictionary<string, QscDspCamera> Cameras { get; set; }
        public Dictionary<string, QscDspComponentControl> ComponentControlPoints { get; private set; }
        public List<QscDspPresets> PresetList = new List<QscDspPresets>();
        public Dictionary<string, IKeyName> Presets { get; set; }

        public BoolFeedback IsPrimaryFeedback;
        public BoolFeedback IsActiveFeedback;

        private DeviceConfig _Dc;

        private const string _changeGroupId = "essentials-cg";
        private readonly List<string> _changeGroupControls = new List<string>();
        private int _requestIdCounter = 0;
        private int _statusGetId = -1;
        private bool _heartbeatReceived = false;

        private bool _IsPrimary;
        public bool IsPrimary
        {
            get { return _IsPrimary; }
            private set { _IsPrimary = value; IsPrimaryFeedback.FireUpdate(); }
        }

        private bool _IsActive;
        public bool IsActive
        {
            get { return _IsActive; }
            private set { _IsActive = value; IsActiveFeedback.FireUpdate(); }
        }

        private uint HeartbeatTracker = 0;
        public bool ShowHexResponse { get; set; }
        private string _username;
        private string _password;
        public string DspName { get; private set; }
        public string AutoTrackingKey { get; set; }

        public QscDsp(string key, string name, IBasicCommunication comm, DeviceConfig dc)
            : base(dc)
        {
            _Dc = dc;
            var props = JsonConvert.DeserializeObject<QscDspPropertiesConfig>(dc.Properties.ToString());
            Debug.Console(2, this, "Made it to device constructor");

            Communication = comm;
            DspName = name;

            var socket = comm as ISocketStatus;
            if (socket != null)
                socket.ConnectionChange += socket_ConnectionChange;

            PortGather = new CommunicationGather(Communication, "\x00");
            PortGather.LineReceived += this.Qrc_MessageReceived;

            CommunicationMonitor = new GenericCommunicationMonitor(this, Communication, 20000, 120000, 300000,
                CheckSubscriptions);

            IsPrimaryFeedback = new BoolFeedback(() => IsPrimary);
            IsActiveFeedback = new BoolFeedback(() => IsActive);

            LevelControlPoints = new Dictionary<string, QscDspLevelControl>();
            Dialers = new Dictionary<string, QscDspDialer>();
            Cameras = new Dictionary<string, QscDspCamera>();
            ComponentControlPoints = new Dictionary<string, QscDspComponentControl>();
            Presets = new Dictionary<string, IKeyName>();
            CreateDspObjects();

            DeviceManager.AllDevicesActivated += (sender, args) =>
            {
                if (comm != null) comm.Connect();
            };
        }

        public override bool CustomActivate()
        {
            CrestronConsole.AddNewConsoleCommand(SendLine, "send" + Key, "", ConsoleAccessLevelEnum.AccessOperator);
            CrestronConsole.AddNewConsoleCommand(s => Communication.Connect(), "con" + Key, "", ConsoleAccessLevelEnum.AccessOperator);
            return true;
        }

        private void socket_ConnectionChange(object sender, GenericSocketStatusChageEventArgs e)
        {
            if (e.Client.IsConnected)
            {
                CrestronInvoke.BeginInvoke(o =>
                {
                    if (!string.IsNullOrEmpty(_username) && !string.IsNullOrEmpty(_password))
                    {
                        SendQrc("Logon", new { User = _username, Password = _password });
                        CrestronEnvironment.Sleep(200);
                    }
                    SubscribeToAttributes();
                });
            }
            else
            {
                _changeGroupControls.Clear();
            }
        }

        private string FormatTag(string prefix, string tag)
        {
            if (prefix == null) prefix = "";
            if (tag == null) return null;
            return string.Format("{0}{1}", prefix, tag);
        }

        public void CreateDspObjects()
        {
            var props = JsonConvert.DeserializeObject<QscDspPropertiesConfig>(_Dc.Properties.ToString());

            _username = props.Control.TcpSshProperties.Username;
            _password = props.Control.TcpSshProperties.Password;

            LevelControlPoints.Clear();
            PresetList.Clear();
            Dialers.Clear();
            Cameras.Clear();

            string prefix = "";
            if (props.Prefix != null) prefix = props.Prefix;

            AutoTrackingKey = string.Format("{0}-{1}", Key, "Auto-Tracking");
            LevelControlPoints.Add(AutoTrackingKey, new QscDspLevelControl(AutoTrackingKey, new QscDspLevelControlBlockConfig
            {
                HasMute = true,
                Label = AutoTrackingKey,
                MuteInstanceTag = "CAM_TRACK"
            }, this));

            if (props.LevelControlBlocks != null)
            {
                foreach (KeyValuePair<string, QscDspLevelControlBlockConfig> block in props.LevelControlBlocks)
                {
                    string key = string.Format("{0}-{1}{2}", Key, prefix, block.Key);
                    var value = block.Value;
                    value.LevelInstanceTag = FormatTag(prefix, value.LevelInstanceTag);
                    value.MuteInstanceTag = FormatTag(prefix, value.MuteInstanceTag);
                    this.LevelControlPoints.Add(key, new QscDspLevelControl(key, value, this));
                    Debug.Console(2, this, "Added LevelControlPoint {0} LevelTag: {1} MuteTag: {2}", key, value.LevelInstanceTag, value.MuteInstanceTag);
                }
            }
            if (props.Presets != null)
            {
                foreach (KeyValuePair<string, QscDspPresets> preset in props.Presets)
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
                    this.AddPreset(value);
                    Presets.Add(preset.Key, qsysPreset);
                    Debug.Console(2, this, "Added Preset {0} {1}", value.Label, value.Preset);
                }
            }
            if (props.CameraControlBlocks != null)
            {
                foreach (KeyValuePair<string, QscDspCameraConfig> camera in props.CameraControlBlocks)
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
                        value.Presets[preset.Key].Bank = FormatTag(prefix, value.Presets[preset.Key].Bank);
                    Cameras.Add(key, new QscDspCamera(this, key, key, value));
                    Debug.Console(2, this, "Added Camera {0}\n {1}", key, value);
                }
            }
            if (props.DialerControlBlocks != null)
            {
                foreach (KeyValuePair<string, QscDialerConfig> dialerConfig in props.DialerControlBlocks)
                {
                    var value = dialerConfig.Value;
                    var key = dialerConfig.Key;
                    key = string.Format("{0}{1}", prefix, key);
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
                    this.Dialers.Add(key, new QscDspDialer(value, this));
                    Debug.Console(2, this, "Added Dialer {0}\n {1}", key, value);
                }
            }

            ComponentControlPoints.Clear();
            if (props.ComponentControlBlocks != null)
            {
                foreach (var block in props.ComponentControlBlocks)
                {
                    var value = block.Value;
                    if (value.Disabled) continue;
                    var key = string.Format("{0}-{1}", Key, block.Key);
                    ComponentControlPoints.Add(key, new QscDspComponentControl(key, value, this));
                    Debug.Console(2, this, "Added ComponentControlPoint {0} Component: {1} Control: {2}", key, value.ComponentName, value.ControlName);
                }
            }

            SubscribeToAttributes();
        }

        protected override void CustomSetConfig(DeviceConfig config)
        {
            ConfigWriter.UpdateDeviceConfig(config);
        }

        public void SetIpAddress(string hostname)
        {
            try
            {
                if (hostname.Length > 2 &
                    _Dc.Properties["control"]["tcpSshProperties"]["address"].ToString() != hostname)
                {
                    Debug.Console(2, this, "Changing IPAddress: {0}", hostname);
                    Communication.Disconnect();
                    (Communication as GenericTcpIpClient).Hostname = hostname;
                    _Dc.Properties["control"]["tcpSshProperties"]["address"] = hostname;
                    CustomSetConfig(_Dc);
                    Communication.Connect();
                }
            }
            catch (Exception e)
            {
                if (Debug.Level == 2)
                    Debug.Console(2, this, "Exception Message: {0}", e.Message);
            }
        }

        public void SetPrefix(string prefix)
        {
            if (_Dc.Properties["prefix"].ToString() != prefix && prefix.Length > 0)
            {
                _Dc.Properties["prefix"] = prefix;
                CustomSetConfig(_Dc);
                Debug.ConsoleWithLog(0, this, "The Dsp Prefix has changed to {0} the program will automaticly restart in 60 seconds", prefix);
                string notUsed = "";
                CTimer restart = new CTimer(
                    (object notused) =>
                    {
                        CrestronConsole.SendControlSystemCommand(
                            string.Format("progres -p:{0}", Global.ControlSystem.ProgramNumber), ref notUsed);
                    }, 60000);
            }
        }

        public void StatusGet(bool enable)
        {
            if (enable)
            {
                _statusGetId = ++_requestIdCounter;
                var request = new QrcRequest { Id = _statusGetId, Method = "StatusGet", Params = 0 };
                Communication.SendText(JsonConvert.SerializeObject(request) + "\x00");
            }
        }

        public void WriteConfig()
        {
            CustomSetConfig(_Dc);
        }

        private void CheckSubscriptions()
        {
            SendQrc("NoOp", new object());
            CrestronEnvironment.Sleep(1000);

            if (!_heartbeatReceived)
            {
                HeartbeatTracker++;
                Debug.Console(1, this, "QRC heartbeat missed, count {0}", HeartbeatTracker);
                if (HeartbeatTracker % 5 == 0)
                {
                    Debug.Console(1, this, "QRC heartbeat missed 5 times, resubscribing");
                    if (HeartbeatTracker == 5)
                        Debug.LogError(Debug.ErrorLogLevel.Warning, "QRC heartbeat missed 5 times - attempting resubscribe.");
                    SubscribeToAttributes();
                }
            }
            else
            {
                HeartbeatTracker = 0;
                Debug.Console(2, this, "QRC heartbeat okay");
            }
            _heartbeatReceived = false;
        }

        private void SubscribeToAttributes()
        {
            SendQrc("ChangeGroup.Destroy", new { Id = _changeGroupId });
            _changeGroupControls.Clear();

            foreach (var level in LevelControlPoints) level.Value.Subscribe();
            foreach (var dialer in Dialers) dialer.Value.Subscribe();
            foreach (var camera in Cameras) camera.Value.Subscribe();

            if (_changeGroupControls.Count > 0)
            {
                SendQrc("ChangeGroup.AddControl", new
                {
                    Id = _changeGroupId,
                    Controls = _changeGroupControls.ToArray()
                });
            }

            foreach (var comp in ComponentControlPoints)
            {
                if (!comp.Value.HasFeedback) continue;
                SendQrc("ChangeGroup.AddComponentControl", new
                {
                    Id = _changeGroupId,
                    Component = new
                    {
                        Name = comp.Value.ComponentName,
                        Controls = new[] { new { Name = comp.Value.ControlName } }
                    }
                });
            }

            SendQrc("ChangeGroup.AutoPoll", new { Id = _changeGroupId, Rate = 1.0 });

            _statusGetId = ++_requestIdCounter;
            var statusReq = new QrcRequest { Id = _statusGetId, Method = "StatusGet", Params = 0 };
            Communication.SendText(JsonConvert.SerializeObject(statusReq) + "\x00");

            if (CommunicationMonitor != null) CommunicationMonitor.Start();
        }

        private void Qrc_MessageReceived(object dev, GenericCommMethodReceiveTextArgs args)
        {
            if (string.IsNullOrEmpty(args.Text)) return;
            try
            {
                var msg = JsonConvert.DeserializeObject<QrcResponse>(args.Text);
                if (msg == null) return;

                _heartbeatReceived = true;

                if (string.Equals(msg.Method, "EngineStatus", StringComparison.OrdinalIgnoreCase))
                {
                    if (msg.Params != null)
                    {
                        var p = msg.Params.ToObject<QrcEngineStatusParams>();
                        UpdateEngineStatus(p.State, p.IsRedundant);
                    }
                    return;
                }

                if (string.Equals(msg.Method, "AccessDenied", StringComparison.OrdinalIgnoreCase))
                {
                    if (!string.IsNullOrEmpty(_username) && !string.IsNullOrEmpty(_password))
                        SendQrc("Logon", new { User = _username, Password = _password });
                    return;
                }

                if (string.Equals(msg.Method, "ChangeGroup.Poll", StringComparison.OrdinalIgnoreCase))
                {
                    if (msg.Params != null)
                    {
                        var p = msg.Params.ToObject<QrcChangeGroupPollParams>();
                        if (p != null && p.Changes != null) RouteChangeGroupUpdates(p.Changes);
                    }
                    return;
                }

                if (msg.Error != null)
                {
                    Debug.Console(1, this, "QRC error code {0}: {1}", msg.Error.Code, msg.Error.Message);
                    var errMsg = msg.Error.Message ?? string.Empty;
                    if (errMsg.IndexOf("login", StringComparison.OrdinalIgnoreCase) >= 0
                        || errMsg.IndexOf("unauthorized", StringComparison.OrdinalIgnoreCase) >= 0
                        || errMsg.IndexOf("access", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        if (!string.IsNullOrEmpty(_username) && !string.IsNullOrEmpty(_password))
                            SendQrc("Logon", new { User = _username, Password = _password });
                    }
                    return;
                }

                if (msg.Result != null && msg.Id.HasValue && msg.Id.Value == _statusGetId)
                {
                    try
                    {
                        var result = msg.Result.ToObject<QrcEngineStatusParams>();
                        UpdateEngineStatus(result.State, result.IsRedundant);
                    }
                    catch (Exception ex)
                    {
                        Debug.Console(2, this, "StatusGet parse error: {0}", ex.Message);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.Console(2, this, "Qrc_MessageReceived Exception: '{0}'\n{1}", args.Text, e);
            }
        }

        public void ProcessSimulatedRx(string s)
        {
            var args = new GenericCommMethodReceiveTextArgs(s);
            Qrc_MessageReceived(this, args);
        }

        public void SendLine(string s)
        {
            Communication.SendText(s + "\x00");
        }

        public void EnqueueCommand(QueuedCommand commandToEnqueue)
        {
            // QRC is stateless fire-and-forget; queuing is not required
        }

        public void EnqueueCommand(string command)
        {
            // QRC is stateless fire-and-forget; queuing is not required
        }

        public void AddPreset(QscDspPresets s)
        {
            PresetList.Add(s);
        }

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

        public void RunPreset(string name)
        {
            var parts = name.Trim().Split(new[] { ' ' }, 3);
            int slotNumber;
            if (parts.Length >= 2 && int.TryParse(parts[1], out slotNumber))
                SendQrc("Snapshot.Load", new { Name = parts[0], Bank = slotNumber });
            else
                SendQrc("Snapshot.Load", new { Name = name, Bank = 1 });
        }

        public void RecallPreset(string key)
        {
            if (!Presets.ContainsKey(key)) return;
            var preset = Presets[key] as QsysPreset;
            this.LogInformation("Running preset {0}", preset.Label);
            if (preset == null) return;
            if (string.IsNullOrEmpty(preset.Preset))
            {
                this.LogInformation("Preset {0} is not valid", preset.Label);
                return;
            }
            RunPreset(preset.Preset);
        }

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

        public void SavePreset(string name)
        {
            var parts = name.Trim().Split(new[] { ' ' }, 2);
            int slotNumber;
            if (parts.Length >= 2 && int.TryParse(parts[1], out slotNumber))
                SendQrc("Snapshot.Save", new { Name = parts[0], Bank = slotNumber });
            else
                SendQrc("Snapshot.Save", new { Name = name, Bank = 1 });
        }

        public class QueuedCommand
        {
            public string Command { get; set; }
            public string AttributeCode { get; set; }
            public QscDspControlPoint ControlPoint { get; set; }
        }

        public BoolFeedback IsOnline
        {
            get { return CommunicationMonitor.IsOnlineFeedback; }
        }

        #region IBridgeAdvanced Members
        public void LinkToApi(BasicTriList trilist, uint joinStart, string joinMapKey, EiscApiAdvanced bridge)
        {
            this.LinkToApiExt(trilist, joinStart, joinMapKey, bridge);
        }
        #endregion

        public class QsysPreset : QscDspPresets, IKeyName
        {
            public string Key { get; private set; }
            public string Name => base.Label;
            public QsysPreset(string key) : base() { Key = key; }
        }

        // QRC JSON-RPC helper methods

        public void SendQrc(string method, object @params)
        {
            var request = new QrcRequest
            {
                Id = ++_requestIdCounter,
                Method = method,
                Params = @params
            };
            Communication.SendText(JsonConvert.SerializeObject(request) + "\x00");
        }

        public void AddControlToChangeGroup(string instanceTag)
        {
            if (!string.IsNullOrEmpty(instanceTag) && !_changeGroupControls.Contains(instanceTag))
                _changeGroupControls.Add(instanceTag);
        }

        public void SendControlSetValue(string tag, double value, double ramp = 0.0)
        {
            SendQrc("Control.Set", new { Controls = new[] { new QrcControlSetItem { Name = tag, Value = value, Ramp = ramp } } });
        }

        public void SendControlSetPosition(string tag, double position, double ramp = 0.0)
        {
            SendQrc("Control.Set", new { Controls = new[] { new QrcControlSetItem { Name = tag, Position = position, Ramp = ramp } } });
        }

        public void SendControlSetString(string tag, string value)
        {
            SendQrc("Control.Set", new { Controls = new[] { new QrcControlSetItem { Name = tag, StringValue = value } } });
        }

        public void SendControlTrigger(string tag)
        {
            SendQrc("Control.Set", new { Controls = new[] { new QrcControlSetItem { Name = tag, Value = 1.0 } } });
        }

        public void SendControlGet(string tag)
        {
            SendQrc("Control.Get", new { Controls = new[] { tag } });
        }

        public void SendComponentSet(string componentName, string controlName, double value, double ramp = 0.0)
        {
            SendQrc("Component.Set", new
            {
                Name = componentName,
                Controls = new[] { new QrcComponentControlSetItem { Name = controlName, Value = value, Ramp = ramp } }
            });
        }

        public void SendComponentSet(string componentName, string controlName, string value)
        {
            SendQrc("Component.Set", new
            {
                Name = componentName,
                Controls = new[] { new QrcComponentControlSetItem { Name = controlName, StringValue = value } }
            });
        }

        private void UpdateEngineStatus(string state, bool isRedundant)
        {
            state = state ?? string.Empty;
            IsPrimary = !string.Equals(state, "Standby", StringComparison.OrdinalIgnoreCase);
            IsActive  = string.Equals(state, "Active",  StringComparison.OrdinalIgnoreCase);
            Debug.Console(1, this, "EngineStatus: State={0} IsRedundant={1} -> IsPrimary={2} IsActive={3}",
                state, isRedundant, IsPrimary, IsActive);
        }
        public void SendSnapshotLoad(string bank, int number)
        {
            SendQrc("Snapshot.Load", new { Name = bank, Bank = number });
        }

        public void SendSnapshotSave(string bank, int number)
        {
            SendQrc("Snapshot.Save", new { Name = bank, Bank = number });
        }

        private void RouteChangeGroupUpdates(List<QrcChangeValue> changes)
        {
            foreach (var change in changes)
            {
                if (!string.IsNullOrEmpty(change.Component))
                {
                    foreach (var comp in ComponentControlPoints)
                    {
                        if (string.Equals(comp.Value.ComponentName, change.Component, StringComparison.OrdinalIgnoreCase)
                            && string.Equals(comp.Value.ControlName, change.Name, StringComparison.OrdinalIgnoreCase))
                        {
                            comp.Value.ParseFeedback(change.Value, change.StringValue);
                            break;
                        }
                    }
                    continue;
                }

                var controlName = change.Name;
                bool found = false;

                foreach (var lcp in LevelControlPoints)
                {
                    if (string.Equals(controlName, lcp.Value.LevelInstanceTag, StringComparison.OrdinalIgnoreCase))
                    {
                        lcp.Value.ParseSubscriptionMessage(controlName, change.Position.ToString("R"), ((int)change.Value).ToString());
                        found = true;
                        break;
                    }
                    if (string.Equals(controlName, lcp.Value.MuteInstanceTag, StringComparison.OrdinalIgnoreCase))
                    {
                        var muteStr = !string.IsNullOrEmpty(change.StringValue)
                            ? change.StringValue
                            : (change.Value > 0.5 ? "true" : "false");
                        lcp.Value.ParseSubscriptionMessage(controlName, muteStr, null);
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    foreach (var dialer in Dialers)
                    {
                        var properties = dialer.Value.Tags.GetType().GetCType().GetProperties();
                        foreach (var prop in properties)
                        {
                            var propValue = prop.GetValue(dialer.Value.Tags, null) as string;
                            if (string.Equals(controlName, propValue, StringComparison.OrdinalIgnoreCase))
                            {
                                var val = !string.IsNullOrEmpty(change.StringValue) ? change.StringValue : change.Value.ToString("G");
                                dialer.Value.ParseSubscriptionMessage(controlName, val);
                                found = true;
                                break;
                            }
                        }
                        if (found) break;
                    }
                }

                if (!found)
                {
                    foreach (var cam in Cameras)
                    {
                        if (string.Equals(controlName, cam.Value.Config.OnlineStatus, StringComparison.OrdinalIgnoreCase))
                        {
                            cam.Value.ParseSubscriptionMessage(controlName, change.Value > 0.5 ? "true" : "false", null);
                            break;
                        }
                    }
                }
            }
        }
    }
}
