using PepperDash.Essentials.Core;

namespace QscQsysDspPlugin
{
	/// <summary>
	/// Plugin device Bridge Join Map
	/// </summary>
	public class QscQsysDspBridgeJoinMap : JoinMapBaseAdvanced
	{
		#region Digital - Channel Joins

		/// <summary>
		/// Online bridge join
		/// </summary>
		[JoinName("IsOnline")]
		public JoinDataComplete IsOnline = new JoinDataComplete(
			new JoinData
			{
				JoinNumber = 1,
				JoinSpan = 1
			},
			new JoinMetadata
			{
				Description = "Is Online",
				JoinCapabilities = eJoinCapabilities.ToSIMPL,
				JoinType = eJoinType.Digital
			});

		/// <summary>
		/// Preset Recall array bridge joins
		/// </summary>
		[JoinName("Presets")]
		public JoinDataComplete Presets = new JoinDataComplete(
			new JoinData
			{
				JoinNumber = 100,
				JoinSpan = 50
			},
			new JoinMetadata
			{
				Description = "Preset Recall",
				JoinCapabilities = eJoinCapabilities.FromSIMPL,
				JoinType = eJoinType.Digital
			});

		/// <summary>
		/// Preset Save array bridge joins
		/// </summary>
		[JoinName("PresetsSave")]
		public JoinDataComplete PresetsSave = new JoinDataComplete(
			new JoinData
			{
				JoinNumber = 150,
				JoinSpan = 50
			},
			new JoinMetadata
			{
				Description = "Preset Save",
				JoinCapabilities = eJoinCapabilities.FromSIMPL,
				JoinType = eJoinType.Digital
			});

		/// <summary>
		/// Channel Visible array bridge joins
		/// </summary>
		[JoinName("ChannelVisible")]
		public JoinDataComplete ChannelVisible = new JoinDataComplete(
			new JoinData
			{
				JoinNumber = 200,
				JoinSpan = 200
			},
			new JoinMetadata
			{
				Description = "Channel Visible",
				JoinCapabilities = eJoinCapabilities.ToSIMPL,
				JoinType = eJoinType.Digital
			});

		/// <summary>
		/// Channel Mute Toggle array bridge joins
		/// </summary>
		[JoinName("ChannelMuteToggle")]
		public JoinDataComplete ChannelMuteToggle = new JoinDataComplete(
			new JoinData
			{
				JoinNumber = 400,
				JoinSpan = 200
			},
			new JoinMetadata
			{
				Description = "Channel Mute Toggle",
				JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
				JoinType = eJoinType.Digital
			});

		/// <summary>
		/// Channel Mute On array bridge joins
		/// </summary>
		[JoinName("ChannelMuteOn")]
		public JoinDataComplete ChannelMuteOn = new JoinDataComplete(
			new JoinData
			{
				JoinNumber = 600,
				JoinSpan = 200
			},
			new JoinMetadata
			{
				Description = "Channel Mute On",
				JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
				JoinType = eJoinType.Digital
			});

		/// <summary>
		/// Channel Mute Off array bridge joins
		/// </summary>
		[JoinName("ChannelMuteOff")]
		public JoinDataComplete ChannelMuteOff = new JoinDataComplete(
			new JoinData
			{
				JoinNumber = 800,
				JoinSpan = 200
			},
			new JoinMetadata
			{
				Description = "Channel Mute Off",
				JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
				JoinType = eJoinType.Digital
			});

		/// <summary>
		/// Channel Volume Up array bridge joins
		/// </summary>
		[JoinName("ChannelVolumeUp")]
		public JoinDataComplete ChannelVolumeUp = new JoinDataComplete(
			new JoinData
			{
				JoinNumber = 1000,
				JoinSpan = 200
			},
			new JoinMetadata
			{
				Description = "Channel Volume Up",
				JoinCapabilities = eJoinCapabilities.FromSIMPL,
				JoinType = eJoinType.Digital
			});

		/// <summary>
		/// Channel Volume Down array bridge joins
		/// </summary>
		[JoinName("ChannelVolumeDown")]
		public JoinDataComplete ChannelVolumeDown = new JoinDataComplete(
			new JoinData
			{
				JoinNumber = 1200,
				JoinSpan = 200
			},
			new JoinMetadata
			{
				Description = "Channel Volume Down",
				JoinCapabilities = eJoinCapabilities.FromSIMPL,
				JoinType = eJoinType.Digital
			});

		#endregion


		#region Analog - Channel Joins

		// TODO [ ] Add analog joins below plugin being developed

		/// <summary>
		/// Plugin socket status join map
		/// </summary>
		[JoinName("SocketStatus")]
		public JoinDataComplete SocketStatus = new JoinDataComplete(
			new JoinData
			{
				JoinNumber = 1,
				JoinSpan = 1
			},
			new JoinMetadata
			{
				Description = "Socket Status",
				JoinCapabilities = eJoinCapabilities.ToSIMPL,
				JoinType = eJoinType.Analog
			});

		/// <summary>
		/// Plugin monitor status join map
		/// </summary>
		[JoinName("MonitorStatus")]
		public JoinDataComplete MonitorStatus = new JoinDataComplete(
			new JoinData
			{
				JoinNumber = 2,
				JoinSpan = 1
			},
			new JoinMetadata
			{
				Description = "Monitor Status",
				JoinCapabilities = eJoinCapabilities.ToSIMPL,
				JoinType = eJoinType.Analog
			});

		/// <summary>
		/// Channel Volume Set/Feedback array bridge joins
		/// </summary>
		[JoinName("ChannelVolume")]
		public JoinDataComplete ChannelVolume = new JoinDataComplete(
			new JoinData
			{
				JoinNumber = 200,
				JoinSpan = 200
			},
			new JoinMetadata
			{
				Description = "Channel Volume Set/Feedback",
				JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
				JoinType = eJoinType.Analog
			});

		/// <summary>
		/// Channel Type Feedback array bridge joins
		/// </summary>
		[JoinName("ChannelTypee")]
		public JoinDataComplete ChannelType = new JoinDataComplete(
			new JoinData
			{
				JoinNumber = 400,
				JoinSpan = 200
			},
			new JoinMetadata
			{
				Description = "Channel Type, (level/mic)",
				JoinCapabilities = eJoinCapabilities.ToSIMPL,
				JoinType = eJoinType.Analog
			});

		#endregion


		#region Serial - Channel Joins

		/// <summary>
		/// Device IP Address
		/// </summary>
		public JoinDataComplete Address = new JoinDataComplete(
			new JoinData
			{
				JoinNumber = 1,
				JoinSpan = 1
			},
			new JoinMetadata
			{
				Description = "IP Address",
				JoinCapabilities = eJoinCapabilities.FromSIMPL,
				JoinType = eJoinType.Serial
			});

		/// <summary>
		/// DSP Prefix
		/// </summary>
		public JoinDataComplete Prefix = new JoinDataComplete(
			new JoinData
			{
				JoinNumber = 2,
				JoinSpan = 1
			},
			new JoinMetadata
			{
				Description = "DSP Prefix",
				JoinCapabilities = eJoinCapabilities.FromSIMPL,
				JoinType = eJoinType.Serial
			});



		/// <summary>
		/// Plugin device name
		/// </summary>
		public JoinDataComplete DeviceName = new JoinDataComplete(
			new JoinData
			{
				JoinNumber = 3,
				JoinSpan = 1
			},
			new JoinMetadata
			{
				Description = "Device Name",
				JoinCapabilities = eJoinCapabilities.ToSIMPL,
				JoinType = eJoinType.Serial
			});

		/// <summary>
		/// Channel Name
		/// </summary>
		public JoinDataComplete ChannelName = new JoinDataComplete(
			new JoinData
			{
				JoinNumber = 200,
				JoinSpan = 200
			},
			new JoinMetadata
			{
				Description = "Channel Name",
				JoinCapabilities = eJoinCapabilities.ToSIMPL,
				JoinType = eJoinType.Serial
			});

		#endregion


		#region Digital - Dialer Joins

		/// <summary>
		/// Dialer Incoming Call
		/// </summary>
		[JoinName("IncomingCall")]
		public JoinDataComplete IncomingCall = new JoinDataComplete(
			new JoinData
			{
				JoinNumber = 3100,
				JoinSpan = 1
			},
			new JoinMetadata
			{
				Description = "Incoming Call",
				JoinCapabilities = eJoinCapabilities.ToSIMPL,
				JoinType = eJoinType.Digital
			});

		/// <summary>
		/// Dialer End Call
		/// </summary>
		[JoinName("EndCall")]
		public JoinDataComplete EndCall = new JoinDataComplete(
			new JoinData
			{
				JoinNumber = 3107,
				JoinSpan = 1
			},
			new JoinMetadata
			{
				Description = "End Call",
				JoinCapabilities = eJoinCapabilities.FromSIMPL,
				JoinType = eJoinType.Digital
			});

		/// <summary>
		/// Dialer Keypad 0
		/// </summary>
		[JoinName("Keypad0")]
		public JoinDataComplete Keypad0 = new JoinDataComplete(
			new JoinData
			{
				JoinNumber = 3110,
				JoinSpan = 1
			},
			new JoinMetadata
			{
				Description = "Keypad 0",
				JoinCapabilities = eJoinCapabilities.FromSIMPL,
				JoinType = eJoinType.Digital
			});

		/// <summary>
		/// Dialer Keypad 1
		/// </summary>
		[JoinName("Keypad1")]
		public JoinDataComplete Keypad1 = new JoinDataComplete(
			new JoinData
			{
				JoinNumber = 3111,
				JoinSpan = 1
			},
			new JoinMetadata
			{
				Description = "Keypad 1",
				JoinCapabilities = eJoinCapabilities.FromSIMPL,
				JoinType = eJoinType.Digital
			});

		/// <summary>
		/// Dialer Keypad 2
		/// </summary>
		[JoinName("Keypad2")]
		public JoinDataComplete Keypad2 = new JoinDataComplete(
			new JoinData
			{
				JoinNumber = 3112,
				JoinSpan = 1
			},
			new JoinMetadata
			{
				Description = "Keypad 2",
				JoinCapabilities = eJoinCapabilities.FromSIMPL,
				JoinType = eJoinType.Digital
			});

		/// <summary>
		/// Dialer Keypad 3
		/// </summary>
		[JoinName("Keypad3")]
		public JoinDataComplete Keypad3 = new JoinDataComplete(
			new JoinData
			{
				JoinNumber = 3113,
				JoinSpan = 1
			},
			new JoinMetadata
			{
				Description = "Keypad 3",
				JoinCapabilities = eJoinCapabilities.FromSIMPL,
				JoinType = eJoinType.Digital
			});

		/// <summary>
		/// Dialer Keypad 4
		/// </summary>
		[JoinName("Keypad4")]
		public JoinDataComplete Keypad4 = new JoinDataComplete(
			new JoinData
			{
				JoinNumber = 3114,
				JoinSpan = 1
			},
			new JoinMetadata
			{
				Description = "Keypad 4",
				JoinCapabilities = eJoinCapabilities.FromSIMPL,
				JoinType = eJoinType.Digital
			});

		/// <summary>
		/// Dialer Keypad 5
		/// </summary>
		[JoinName("Keypad5")]
		public JoinDataComplete Keypad5 = new JoinDataComplete(
			new JoinData
			{
				JoinNumber = 3115,
				JoinSpan = 1
			},
			new JoinMetadata
			{
				Description = "Keypad 5",
				JoinCapabilities = eJoinCapabilities.FromSIMPL,
				JoinType = eJoinType.Digital
			});

		/// <summary>
		/// Dialer Keypad 6
		/// </summary>
		[JoinName("Keypad6")]
		public JoinDataComplete Keypad6 = new JoinDataComplete(
			new JoinData
			{
				JoinNumber = 3116,
				JoinSpan = 1
			},
			new JoinMetadata
			{
				Description = "Keypad6",
				JoinCapabilities = eJoinCapabilities.FromSIMPL,
				JoinType = eJoinType.Digital
			});

		/// <summary>
		/// Dialer Keypad 7
		/// </summary>
		[JoinName("Keypad7")]
		public JoinDataComplete Keypad7 = new JoinDataComplete(
			new JoinData
			{
				JoinNumber = 3117,
				JoinSpan = 1
			},
			new JoinMetadata
			{
				Description = "Keypad 7",
				JoinCapabilities = eJoinCapabilities.FromSIMPL,
				JoinType = eJoinType.Digital
			});

		/// <summary>
		/// Dialer Keypad 8
		/// </summary>
		[JoinName("Keypad8")]
		public JoinDataComplete Keypad8 = new JoinDataComplete(
			new JoinData
			{
				JoinNumber = 3118,
				JoinSpan = 1
			},
			new JoinMetadata
			{
				Description = "Keypad 8",
				JoinCapabilities = eJoinCapabilities.FromSIMPL,
				JoinType = eJoinType.Digital
			});

		/// <summary>
		/// Dialer Keypad 9
		/// </summary>
		[JoinName("Keypad9")]
		public JoinDataComplete Keypad9 = new JoinDataComplete(
			new JoinData
			{
				JoinNumber = 3119,
				JoinSpan = 1
			},
			new JoinMetadata
			{
				Description = "Keypad 9",
				JoinCapabilities = eJoinCapabilities.FromSIMPL,
				JoinType = eJoinType.Digital
			});

		/// <summary>
		/// Dialer Keypad * (star)
		/// </summary>
		[JoinName("KeypadStar")]
		public JoinDataComplete KeypadStar = new JoinDataComplete(
			new JoinData
			{
				JoinNumber = 3120,
				JoinSpan = 1
			},
			new JoinMetadata
			{
				Description = "Keypad * (star)",
				JoinCapabilities = eJoinCapabilities.FromSIMPL,
				JoinType = eJoinType.Digital
			});

		/// <summary>
		/// Dialer Keypad # (pound)
		/// </summary>
		[JoinName("KeypadPound")]
		public JoinDataComplete KeypadPound = new JoinDataComplete(
			new JoinData
			{
				JoinNumber = 3121,
				JoinSpan = 1
			},
			new JoinMetadata
			{
				Description = "Keypad # (pound)",
				JoinCapabilities = eJoinCapabilities.FromSIMPL,
				JoinType = eJoinType.Digital
			});

		/// <summary>
		/// Dialer Keypad Clear
		/// </summary>
		[JoinName("KeypadCear")]
		public JoinDataComplete KeypadClear = new JoinDataComplete(
			new JoinData
			{
				JoinNumber = 3122,
				JoinSpan = 1
			},
			new JoinMetadata
			{
				Description = "Keypad Clear",
				JoinCapabilities = eJoinCapabilities.FromSIMPL,
				JoinType = eJoinType.Digital
			});

		/// <summary>
		/// Dialer Keypad Bacspace
		/// </summary>
		[JoinName("KeypadBackspace")]
		public JoinDataComplete KeypadBackspace = new JoinDataComplete(
			new JoinData
			{
				JoinNumber = 3123,
				JoinSpan = 1
			},
			new JoinMetadata
			{
				Description = "Keypad Backspace",
				JoinCapabilities = eJoinCapabilities.FromSIMPL,
				JoinType = eJoinType.Digital
			});

		/// <summary>
		/// Dialer Dial
		/// </summary>
		[JoinName("Dial")]
		public JoinDataComplete Dial = new JoinDataComplete(
			new JoinData
			{
				JoinNumber = 3124,
				JoinSpan = 1
			},
			new JoinMetadata
			{
				Description = "Dial",
				JoinCapabilities = eJoinCapabilities.FromSIMPL,
				JoinType = eJoinType.Digital
			});

		/// <summary>
		/// Dialer Auto Answer On
		/// </summary>
		[JoinName("AutoAnswerOn")]
		public JoinDataComplete AutoAnswerOn = new JoinDataComplete(
			new JoinData
			{
				JoinNumber = 3125,
				JoinSpan = 1
			},
			new JoinMetadata
			{
				Description = "Auto Answer On Set/Feedback",
				JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
				JoinType = eJoinType.Digital
			});

		/// <summary>
		/// Dialer Auto Answer Off
		/// </summary>
		[JoinName("AutoAnswerOff")]
		public JoinDataComplete AutoAnswerOff = new JoinDataComplete(
			new JoinData
			{
				JoinNumber = 3126,
				JoinSpan = 1
			},
			new JoinMetadata
			{
				Description = "Auto Answer Off Set/Feedback",
				JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
				JoinType = eJoinType.Digital
			});

		/// <summary>
		/// Dialer Auto Answer Toggle
		/// </summary>
		[JoinName("AutoAnswerToggle")]
		public JoinDataComplete AutoAnswerToggle = new JoinDataComplete(
			new JoinData
			{
				JoinNumber = 3127,
				JoinSpan = 1
			},
			new JoinMetadata
			{
				Description = "Auto Answer Toggle Set/Feedback",
				JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
				JoinType = eJoinType.Digital
			});

		/// <summary>
		/// Dialer Do Not Disturb On
		/// </summary>
		[JoinName("DoNotDisturbOn")]
		public JoinDataComplete DoNotDisturbOn = new JoinDataComplete(
			new JoinData
			{
				JoinNumber = 3133,
				JoinSpan = 1
			},
			new JoinMetadata
			{
				Description = "Do Not Disturb On Set/Feedback",
				JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
				JoinType = eJoinType.Digital
			});

		/// <summary>
		/// Dialer Do Not Distrub Off
		/// </summary>
		[JoinName("DoNotDisturbOff")]
		public JoinDataComplete DoNotDisturbOff = new JoinDataComplete(
			new JoinData
			{
				JoinNumber = 3134,
				JoinSpan = 1
			},
			new JoinMetadata
			{
				Description = "Do Not Disturb Off Set/Feedback",
				JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
				JoinType = eJoinType.Digital
			});

		/// <summary>
		/// Dialer Do Not Distrub Toggle
		/// </summary>
		[JoinName("DoNotDisturbToggle")]
		public JoinDataComplete DoNotDisturbToggle = new JoinDataComplete(
			new JoinData
			{
				JoinNumber = 3132,
				JoinSpan = 1
			},
			new JoinMetadata
			{
				Description = "Do Not Disturb Toggle Set/Feedback",
				JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
				JoinType = eJoinType.Digital
			});

		/// <summary>
		/// Dialer On Hook
		/// </summary>
		[JoinName("OnHook")]
		public JoinDataComplete OnHook = new JoinDataComplete(
			new JoinData
			{
				JoinNumber = 3129,
				JoinSpan = 1
			},
			new JoinMetadata
			{
				Description = "On Hook Set/Feedback",
				JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
				JoinType = eJoinType.Digital
			});

		/// <summary>
		/// Dialer Off Hook
		/// </summary>
		[JoinName("OffHook")]
		public JoinDataComplete OffHook = new JoinDataComplete(
			new JoinData
			{
				JoinNumber = 3130,
				JoinSpan = 1
			},
			new JoinMetadata
			{
				Description = "Off Hook Set/Feedback",
				JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
				JoinType = eJoinType.Digital
			});

		#endregion


		#region Analog - Dialer Joins



		#endregion


		#region Serial - Dialer Joins

		/// <summary>
		/// Dialer Dial String
		/// </summary>
		[JoinName("DialStringCmd")]
		public JoinDataComplete DialStringCmd = new JoinDataComplete(
			new JoinData
			{
				JoinNumber = 3100,
				JoinSpan = 1
			},
			new JoinMetadata
			{
				Description = "Dial String Set/Feedback",
				JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
				JoinType = eJoinType.Serial
			});

		#endregion

	
		/// <summary>
		/// Plugin device BridgeJoinMap constructor
		/// </summary>
		/// <param name="joinStart">This will be the join it starts on the EISC bridge</param>
		public QscQsysDspBridgeJoinMap(uint joinStart)
			: base(joinStart, typeof(QscQsysCameraBridgeJoinMap))
		{
		}
	}
}