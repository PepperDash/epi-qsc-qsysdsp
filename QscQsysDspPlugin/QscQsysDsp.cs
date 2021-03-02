using System.Collections.Generic;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro.DeviceSupport;
using PepperDash.Core;
using PepperDash.Core.JsonStandardObjects;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Bridges;

namespace QscQsysDspPlugin
{
	/// <summary>
	/// Plugin device
	/// </summary>
	public class QscQsysDsp : EssentialsBridgeableDevice
	{
		/// <summary>
		/// It is often desirable to store the config
		/// </summary>
		private QscQsysDspPropertiesConfig _config;

		#region IBasicCommunication Properties and Constructor

		private long _pollTimeMs = 20000;
		private long _warningTimeoutMs = 120000;
		private long _errorTimeoutMs = 300000;

		private readonly IBasicCommunication _comms;
		private readonly CommunicationGather _commsGather;
		private readonly GenericCommunicationMonitor _commsMonitor;

		/// <summary>
		/// Set this value to that of the delimiter used by the API (if applicable)
		/// </summary>
		private const string CommsDelimiter = "\x0A";


		private CrestronQueue _commsQueue;
		private bool _commsQueueActive = false;
		private uint HeartbeatTracker = 0;


		/// <summary>
		/// Flag used to determine if the response will be shown in HEX
		/// </summary>
		public bool ShowHexResponse { get; set; }

		/// <summary>
		/// Dictionary of defined level controls
		/// </summary>
		public Dictionary<string, QscQsysDspLevelControl> LevelControlPoints { get; private set; } 

		/// <summary>
		/// List of defined presets
		/// </summary>
		public List<QscQsysDspPresets> PresetList = new List<QscQsysDspPresets>(); 

		/// <summary>
		/// Dictionary of defined dialers
		/// </summary>
		public Dictionary<string, QscQsysDspDialer> Dialers { get; set; }

		/// <summary>
		/// Dictionary of defined cameras
		/// </summary>
		public Dictionary<string,QscQsysCamera> Cameras { get; set; }

		/// <summary>
		/// Reports connect feedback through the bridge
		/// </summary>
		public BoolFeedback ConnectFeedback { get; private set; }

		/// <summary>
		/// Reports online feedback through the bridge
		/// </summary>
		public BoolFeedback OnlineFeedback { get; private set; }

		/// <summary>
		/// Reports socket status feedback through the bridge
		/// </summary>
		public IntFeedback SocketStatusFeedback { get; private set; }

		/// <summary>
		/// Reports monitor status feedback through the bridge
		/// Typically used for Fusion status reporting and system status LED's
		/// </summary>
		public IntFeedback MonitorStatusFeedback { get; private set; }

		/// <summary>
		/// Plugin device constructor
		/// </summary>
		/// <param name="key">device key</param>
		/// <param name="name">device name</param>		
		/// <param name="comms">device communication as IBasicCommunication</param>
		/// <param name="dc"></param>
		public QscQsysDsp(string key, string name, IBasicCommunication comms, QscQsysDspPropertiesConfig dc)
			: base(key, name)
		{
			Debug.Console(0, this, "Constructing new {0} instance", name);

			_config = dc;

			ConnectFeedback = new BoolFeedback(() => _comms.IsConnected);
			OnlineFeedback = new BoolFeedback(() => _commsMonitor.IsOnline);
			MonitorStatusFeedback = new IntFeedback(() => (int)_commsMonitor.Status);

			_commsQueue = new CrestronQueue(100);
			_comms = comms;						

			var socket = _comms as ISocketStatus;
			if (socket != null)
			{
				// device comms is IP **ELSE** device comms is RS232
				socket.ConnectionChange += socket_ConnectionChange;
				SocketStatusFeedback = new IntFeedback(() => (int)socket.ClientStatus);
			}

			// _comms gather is commonly used for ASCII based API's that have a defined delimiter
			_commsGather = new CommunicationGather(_comms, CommsDelimiter);			
			// Event fires when the defined delimter is found
			_commsGather.LineReceived += Handle_LineRecieved;
			// create comms monitor
			_commsMonitor = new GenericCommunicationMonitor(this, _comms, _pollTimeMs, _warningTimeoutMs, _errorTimeoutMs, Poll);

			LevelControlPoints = new Dictionary<string, QscQsysDspLevelControl>();			
			Dialers = new Dictionary<string, QscQsysDspDialer>();
			Cameras = new Dictionary<string, QscQsysCamera>();

			Debug.Console(0, this, "Constructing new {0} instance complete", name);
			Debug.Console(0, new string('*', 80));
			Debug.Console(0, new string('*', 80));
		}

		/// <summary>
		/// Use the custom activiate to connect the device and start the comms monitor.
		/// This method will be called when the device is built.
		/// </summary>
		/// <returns></returns>
		public override bool CustomActivate()
		{
			// Essentials will handle the connect method to the device                       
			_comms.Connect();
			// Essentialss will handle starting the comms monitor
			_commsMonitor.Start();

			return base.CustomActivate();
		}

		private void socket_ConnectionChange(object sender, GenericSocketStatusChageEventArgs args)
		{
			if (ConnectFeedback != null)
				ConnectFeedback.FireUpdate();

			if (SocketStatusFeedback != null)
				SocketStatusFeedback.FireUpdate();
		}

		// TODO [ ] Delete the properties below if using a HEX/byte based API
		// commonly used with ASCII based API's with a defined delimiter				
		private void Handle_LineRecieved(object sender, GenericCommMethodReceiveTextArgs args)
		{
			// TODO [ ] Implement method 
			throw new System.NotImplementedException();
		}

		// TODO [ ] Delete the properties below if using a HEX/byte based API
		// commonly used with ASCII based API's without a delimiter
		void Handle_TextReceived(object sender, GenericCommMethodReceiveTextArgs e)
		{
			// TODO [ ] Implement method 
			throw new System.NotImplementedException();
		}

		// TODO [ ] Delete the properties below if using a HEX/byte based API
		/// <summary>
		/// Sends text to the device plugin comms
		/// </summary>
		/// <remarks>
		/// Can be used to test commands with the device plugin using the DEVPROPS and DEVJSON console commands
		/// </remarks>
		/// <param name="text">Command to be sent</param>		
		public void SendText(string text)
		{
			if (string.IsNullOrEmpty(text)) return;

			_comms.SendText(string.Format("{0}{1}", text, CommsDelimiter));
		}

		/// <summary>
		/// Polls the device
		/// </summary>
		/// <remarks>
		/// Poll method is used by the communication monitor.  Update the poll method as needed for the plugin being developed
		/// </remarks>
		public void Poll()
		{
			// TODO [ ] Update Poll method as needed for the plugin being developed
			// Example: SendText("getStatus");
			throw new System.NotImplementedException();
		}

		#endregion IBasicCommunication Properties and Constructor.  Remove if not needed.


		#region Overrides of EssentialsBridgeableDevice

		/// <summary>
		/// Links the plugin device to the EISC bridge
		/// </summary>
		/// <param name="trilist"></param>
		/// <param name="joinStart"></param>
		/// <param name="joinMapKey"></param>
		/// <param name="bridge"></param>
		public override void LinkToApi(BasicTriList trilist, uint joinStart, string joinMapKey, EiscApiAdvanced bridge)
		{
			var joinMap = new QscQsysDspBridgeJoinMap(joinStart);

			// This adds the join map to the collection on the bridge
			if (bridge != null)
			{
				bridge.AddJoinMap(Key, joinMap);
			}

			var customJoins = JoinMapHelper.TryGetJoinMapAdvancedForDevice(joinMapKey);

			if (customJoins != null)
			{
				joinMap.SetCustomJoinData(customJoins);
			}

			Debug.Console(1, "Linking to Trilist '{0}'", trilist.ID.ToString("X"));
			Debug.Console(0, "Linking to Bridge Type {0}", GetType().Name);

			// TODO [ ] Implement bridge links as needed

			// links to bridge
			trilist.SetString(joinMap.DeviceName.JoinNumber, Name);
			
			// TODO [ ] If connection state is managed by Essentials, delete the following.  If connection is managed by SiMPL, uncomment the following
			//trilist.SetBoolSigAction(joinMap.Connect.JoinNumber, sig => Connect = sig);
			//ConnectFeedback.LinkInputSig(trilist.BooleanInput[joinMap.Connect.JoinNumber]);

			SocketStatusFeedback.LinkInputSig(trilist.UShortInput[joinMap.SocketStatus.JoinNumber]);
			OnlineFeedback.LinkInputSig(trilist.BooleanInput[joinMap.IsOnline.JoinNumber]);

			UpdateFeedbacks();

			trilist.OnlineStatusChange += (o, a) =>
			{
				if (!a.DeviceOnLine) return;

				trilist.SetString(joinMap.DeviceName.JoinNumber, Name);
				UpdateFeedbacks();
			};
		}

		private void UpdateFeedbacks()
		{
			// TODO [ ] Update as needed for the plugin being developed
			ConnectFeedback.FireUpdate();
			OnlineFeedback.FireUpdate();
			SocketStatusFeedback.FireUpdate();
		}

		#endregion Overrides of EssentialsBridgeableDevice
	}
}

