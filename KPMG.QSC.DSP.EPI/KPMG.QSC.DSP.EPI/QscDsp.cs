using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Devices;
using PepperDash.Essentials.Devices.Common.Codec;
using PepperDash.Essentials.Devices.Common.DSP;
using System.Text.RegularExpressions;
using Crestron.SimplSharp.Reflection;
using Newtonsoft.Json;
using PepperDash.Essentials.Core.Config;
using PepperDash.Essentials.Bridges;
using Crestron.SimplSharpPro.DeviceSupport;
using Crestron.SimplSharpPro.Diagnostics;
namespace QSC.DSP.EPI
{


	// QUESTIONS:
	// 
	// When subscribing, just use the Instance ID for Custom Name?
	
	// Verbose on subscriptions?

    // Example subscription feedback responses
	// ! "publishToken":"name" "value":-77.0
	// ! "myLevelName" -77

	public class QscDsp : ReconfigurableDevice , IBridge
    {


		public static void LoadPlugin()
		{
			DeviceFactory.AddFactoryForType("qscdsp", QscDsp.BuildDevice);
		}

		public static QscDsp BuildDevice(DeviceConfig dc)
		{
			Debug.Console(2, "QscDsp config is null: {0}", dc == null);
			var comm = CommFactory.CreateCommForDevice(dc);
			Debug.Console(2, "QscDsp comm is null: {0}", comm == null);
			var newMe = new QscDsp(dc.Key, dc.Name, comm, dc);

			return newMe;
		}


        public IBasicCommunication Communication { get; private set; }
        public CommunicationGather PortGather { get; private set; }
		public GenericCommunicationMonitor CommunicationMonitor { get; private set; }

        public Dictionary<string, QscDspLevelControl> LevelControlPoints { get; private set; }
		public Dictionary<string, QscDspDialer> Dialers { get; set; }
		public Dictionary<string, QscDspCamera> Cameras { get; set; }
		public List<QscDspPresets> PresetList = new List<QscDspPresets>();

		DeviceConfig _Dc;

        // public bool isSubscribed;

        CrestronQueue CommandQueue;

        bool CommandQueueInProgress = false;
        public bool ShowHexResponse { get; set; }
        public QscDsp(string key, string name, IBasicCommunication comm, DeviceConfig dc) : base(dc)
        {
			_Dc = dc;
			QscDspPropertiesConfig props = JsonConvert.DeserializeObject<QscDspPropertiesConfig>(dc.Properties.ToString());
            Debug.Console(0, this, "Made it to device constructor");
            CommandQueue = new CrestronQueue(100);
            Communication = comm;
            var socket = comm as ISocketStatus;
            if (socket != null)
            {
                // This instance uses IP control
                socket.ConnectionChange += new EventHandler<GenericSocketStatusChageEventArgs>(socket_ConnectionChange);
            }
            else
            {
                // This instance uses RS-232 control
            }
            PortGather = new CommunicationGather(Communication, "\x0a");
            PortGather.LineReceived += this.Port_LineReceived;

			if (props.CommunicationMonitorProperties != null)
			{
				CommunicationMonitor = new GenericCommunicationMonitor(this, Communication, props.CommunicationMonitorProperties);
			}
			else
			{
				CommunicationMonitor = new GenericCommunicationMonitor(this, Communication, 20000, 120000, 300000, "cgp 1\x0D\x0A");
			}


			LevelControlPoints = new Dictionary<string, QscDspLevelControl>();
			Dialers = new Dictionary<string, QscDspDialer>();
			Cameras = new Dictionary<string, QscDspCamera>();
			CreateDspObjects();
        }
        public override bool CustomActivate()
        {
            Communication.Connect();
			CommunicationMonitor.StatusChange += (o, a) => { Debug.Console(2, this, "Communication monitor state: {0}", CommunicationMonitor.Status); };
			CommunicationMonitor.Start();
			

            CrestronConsole.AddNewConsoleCommand(SendLine, "send" + Key, "", ConsoleAccessLevelEnum.AccessOperator);
            CrestronConsole.AddNewConsoleCommand(s => Communication.Connect(), "con" + Key, "", ConsoleAccessLevelEnum.AccessOperator);
            return true;
        }
        void socket_ConnectionChange(object sender, GenericSocketStatusChageEventArgs e)
        {
            Debug.Console(2, this, "Socket Status Change: {0}", e.Client.ClientStatus.ToString());

            if (e.Client.IsConnected)
            {
				SubscribeToAttributes();
            }
            else
            {
                // Cleanup items from this session
                CommandQueue.Clear();
                CommandQueueInProgress = false;
            }
        }

		public void CreateDspObjects()
		{
			
			QscDspPropertiesConfig props = JsonConvert.DeserializeObject<QscDspPropertiesConfig>(_Dc.Properties.ToString());

			LevelControlPoints.Clear();
			PresetList.Clear();
			Dialers.Clear();
			Cameras.Clear(); 

			// Check for prefix
			string prefix = "";
			if (props.Prefix != null) {prefix = props.Prefix;}

			if (props.LevelControlBlocks != null)
			{
				foreach (KeyValuePair<string, QscDspLevelControlBlockConfig> block in props.LevelControlBlocks)
				{
						string key = string.Format("{0}{1}", prefix, block.Key);
						var value = block.Value;
						value.LevelInstanceTag = string.Format("{0}{1}", prefix, value.LevelInstanceTag);
						value.MuteInstanceTag = string.Format("{0}{1}", prefix, value.MuteInstanceTag);

						this.LevelControlPoints.Add(key, new QscDspLevelControl(key, value, this));
						Debug.Console(2, this, "Added LevelControlPoint {0} LevelTag: {1} MuteTag: {2}", key, value.LevelInstanceTag, value.MuteInstanceTag);
				}
			}
			if (props.presets != null)
			{
				foreach (KeyValuePair<string, QscDspPresets> preset in props.presets)
				{
					var value = preset.Value;
					value.preset = string.Format("{0}{1}", prefix, value.preset);
					this.addPreset(value);
					Debug.Console(2, this, "Added Preset {0} {1}", value.label, value.preset);
				}
			}
			if (props.CameraControlBlocks != null)
			{
				foreach (KeyValuePair<string, QscDspCameraConfig> camera in props.CameraControlBlocks)
				{
					var value = camera.Value;
					var key = camera.Key;
					if (prefix.Length > 0)
					{
						value.PanLeftTag = string.Format("{0}{1}", prefix, value.PanLeftTag);
						value.PanRightTag = string.Format("{0}{1}", prefix, value.PanRightTag);
						value.TiltUpTag = string.Format("{0}{1}", prefix, value.TiltUpTag);
						value.TiltDownTag = string.Format("{0}{1}", prefix, value.TiltDownTag);
						value.ZoomInTag = string.Format("{0}{1}", prefix, value.ZoomInTag);
						value.ZoomOutTag = string.Format("{0}{1}", prefix, value.ZoomOutTag);
						value.PresetBankTag = string.Format("{0}{1}", prefix, value.PresetBankTag);
						foreach (var preset in value.Presets)
						{
							value.Presets[preset.Key].Bank = string.Format("{0}{1}", prefix, value.Presets[preset.Key].Bank);
						}

					}
					Cameras.Add(key, new QscDspCamera(this, key, key, value));
					Debug.Console(2, this, "Added Camera {0}\n {1}", key, value);

				}
			}
			if (props.dialerControlBlocks != null)
			{
				foreach (KeyValuePair<string, QscDialerConfig> dialerConfig in props.dialerControlBlocks)
				{
					var value = dialerConfig.Value;
					var key = dialerConfig.Key;
					if (prefix.Length > 0)
					{
						key = string.Format("{0}{1}", prefix, key);
						value.autoAnswerTag = string.Format("{0}{1}", prefix, value.autoAnswerTag);
						value.callStatusTag = string.Format("{0}{1}", prefix, value.callStatusTag);
						value.connectTag = string.Format("{0}{1}", prefix, value.connectTag);
						value.dialStringTag = string.Format("{0}{1}", prefix, value.dialStringTag);
						value.disconnectTag = string.Format("{0}{1}", prefix, value.disconnectTag);
						value.doNotDisturbTag = string.Format("{0}{1}", prefix, value.doNotDisturbTag);
						value.hookStatusTag = string.Format("{0}{1}", prefix, value.hookStatusTag);
						value.incomingCallRingerTag = string.Format("{0}{1}", prefix, value.incomingCallRingerTag);
						value.keypad0Tag = string.Format("{0}{1}", prefix, value.keypad0Tag);
						value.keypad1Tag = string.Format("{0}{1}", prefix, value.keypad1Tag);
						value.keypad2Tag = string.Format("{0}{1}", prefix, value.keypad2Tag);
						value.keypad3Tag = string.Format("{0}{1}", prefix, value.keypad3Tag);
						value.keypad4Tag = string.Format("{0}{1}", prefix, value.keypad4Tag);
						value.keypad5Tag = string.Format("{0}{1}", prefix, value.keypad5Tag);
						value.keypad6Tag = string.Format("{0}{1}", prefix, value.keypad6Tag);
						value.keypad7Tag = string.Format("{0}{1}", prefix, value.keypad7Tag);
						value.keypad8Tag = string.Format("{0}{1}", prefix, value.keypad8Tag);
						value.keypad9Tag = string.Format("{0}{1}", prefix, value.keypad9Tag);
						value.keypadBackspaceTag = string.Format("{0}{1}", prefix, value.keypadBackspaceTag);
						value.keypadClearTag = string.Format("{0}{1}", prefix, value.keypadClearTag);
						value.keypadPoundTag = string.Format("{0}{1}", prefix, value.keypadPoundTag);
						value.keypadStarTag = string.Format("{0}{1}", prefix, value.keypadStarTag);



					}
					this.Dialers.Add(key, new QscDspDialer(value, this));
					Debug.Console(2, this, "Added Dialer {0}\n {1}", key, value);

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
				if (hostname.Length > 2 & _Dc.Properties["control"]["tcpSshProperties"]["address"].ToString() != hostname)
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
					Debug.Console(2, this, "Error SetIpAddress: '{0}'", e);
			}
		}
		public void SetPrefix(string prefix)
		{
			if (_Dc.Properties["prefix"].ToString() != prefix)
			{
				_Dc.Properties["prefix"] = prefix;
				CustomSetConfig(_Dc);
				// CreateDspObjects();
				Debug.ConsoleWithLog(0, this, "The Dsp Prefix has changed to {0} the program will automaticly restart in 60 seconds", prefix);
				string notUsed = ""; 
				CTimer restart = new CTimer((object notused) =>
				{
					CrestronConsole.SendControlSystemCommand(string.Format("progres -p:{0}", Global.ControlSystem.ProgramNumber), ref notUsed);
				}, 60000);

			}
		}
		public void WriteConfig()
		{
			CustomSetConfig(_Dc);
		}
        /// <summary>
        /// Initiates the subscription process to the DSP
        /// </summary>
        void SubscribeToAttributes()
        {
			// Change Group destroy
			SendLine("cgd 1");

			// Change Group create
            SendLine("cgc 1");
			
            foreach (KeyValuePair<string, QscDspLevelControl> level in LevelControlPoints)
            {
                level.Value.Subscribe();
            }

			foreach (var dialer in Dialers)
			{
				dialer.Value.Subscribe();
			}

			if (CommunicationMonitor != null)
			{

				CommunicationMonitor.Start();
			}
			CommunicationMonitor.StatusChange += (o, a) => { Debug.Console(2, this, "Communication monitor state: {0}", CommunicationMonitor.Status); };
            if (!CommandQueueInProgress)
                SendNextQueuedCommand();
        }



        /// <summary>
        /// Handles a response message from the DSP
        /// </summary>
        /// <param name="dev"></param>
        /// <param name="args"></param>
        void Port_LineReceived(object dev, GenericCommMethodReceiveTextArgs args)
        {
            Debug.Console(2, this, "RX: '{0}'", args.Text);
            try
            {
                if (args.Text.IndexOf("sr ") > -1)
                {
                }
                else if (args.Text.IndexOf("cv") > -1)
                {
                    
				var changeMessage = args.Text.Split(null);

				string changedInstance = changeMessage[1].Replace("\"", "");
				Debug.Console(1, this, "cv parse Instance: {0}", changedInstance);
				bool foundItFlag = false;
                foreach (KeyValuePair<string, QscDspLevelControl> controlPoint in LevelControlPoints)
                {
					if (changedInstance == controlPoint.Value.LevelInstanceTag)
                    {
						controlPoint.Value.ParseSubscriptionMessage(changedInstance, changeMessage[4], changeMessage[3]);
						foundItFlag = true;
                        return;
                    }

					else if (changedInstance == controlPoint.Value.MuteInstanceTag)
					{
						controlPoint.Value.ParseSubscriptionMessage(changedInstance, changeMessage[2].Replace("\"", ""), null);
						foundItFlag = true;
						return;
					}

                }
				if (!foundItFlag)
				{
					foreach (var dialer in Dialers)
					{
						PropertyInfo[] properties = dialer.Value.Tags.GetType().GetCType().GetProperties();
						//GetPropertyValues(Tags);
						foreach (var prop in properties)
						{
							var propValue = prop.GetValue(dialer.Value.Tags, null) as string;
							if (changedInstance == propValue)
							{
								dialer.Value.ParseSubscriptionMessage(changedInstance, changeMessage[2].Replace("\"", ""));
								foundItFlag = true;
								return;
							}
						}
						if (foundItFlag)
						{
							return;
						}
					}
				}
                   
                }

                
            }
            catch (Exception e)
            {
                if (Debug.Level == 2)
                    Debug.Console(2, this, "Error parsing response: '{0}'\n{1}", args.Text, e);
            }

        }

        /// <summary>
        /// Sends a command to the DSP (with delimiter appended)
        /// </summary>
        /// <param name="s">Command to send</param>
        public void SendLine(string s)
        {
            Debug.Console(1, this, "TX: '{0}'", s);
            Communication.SendText(s + "\x0a");
        }

        /// <summary>
        /// Adds a command from a child module to the queue
        /// </summary>
        /// <param name="command">Command object from child module</param>
        public void EnqueueCommand(QueuedCommand commandToEnqueue)
        {
            CommandQueue.Enqueue(commandToEnqueue);
            //Debug.Console(1, this, "Command (QueuedCommand) Enqueued '{0}'.  CommandQueue has '{1}' Elements.", commandToEnqueue.Command, CommandQueue.Count);

            if(!CommandQueueInProgress)
                SendNextQueuedCommand();
        }

        /// <summary>
        /// Adds a raw string command to the queue
        /// </summary>
        /// <param name="command"></param>
        public void EnqueueCommand(string command)
        {
            CommandQueue.Enqueue(command);
            //Debug.Console(1, this, "Command (string) Enqueued '{0}'.  CommandQueue has '{1}' Elements.", command, CommandQueue.Count);

            if (!CommandQueueInProgress)
                SendNextQueuedCommand();
        }

        /// <summary>
        /// Sends the next queued command to the DSP
        /// </summary>
        void SendNextQueuedCommand()
        {
                if (Communication.IsConnected && !CommandQueue.IsEmpty)
                {
                    CommandQueueInProgress = true;

                    if (CommandQueue.Peek() is QueuedCommand)
                    {
                        QueuedCommand nextCommand = new QueuedCommand();

                        nextCommand = (QueuedCommand)CommandQueue.Peek();

                        SendLine(nextCommand.Command);
                    }
                    else
                    {
                        string nextCommand = (string)CommandQueue.Peek();

                        SendLine(nextCommand);
                    }
                }
            
        }

		public void RunPresetNumber(ushort n)
		{
			RunPreset(PresetList[n].preset);
		}

		public void addPreset(QscDspPresets s)
		{
			PresetList.Add(s);
		}
        /// <summary>
        /// Sends a command to execute a preset
        /// </summary>
        /// <param name="name">Preset Name</param>
        public void RunPreset(string name)
        {
            SendLine(string.Format("ssl {0}", name));
			SendLine("cgp 1");
        }

        public class QueuedCommand
        {
            public string Command { get; set; }
            public string AttributeCode { get; set; }
            public QscDspControlPoint ControlPoint { get; set; }
        }

        #region IBridge Members

        public void LinkToApi(BasicTriList trilist, uint joinStart, string joinMapKey)
        {
            this.LinkToApiExt(trilist, joinStart, joinMapKey);
        }

        #endregion
    }
}