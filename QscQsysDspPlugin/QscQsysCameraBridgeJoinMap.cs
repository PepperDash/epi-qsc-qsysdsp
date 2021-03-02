using PepperDash.Essentials.Core;

namespace QscQsysDspPlugin
{
	/// <summary>
	/// Plugin device Bridge Join Map
	/// </summary>
	public class QscQsysCameraBridgeJoinMap : JoinMapBaseAdvanced
	{		
		#region Digital - Camera Joins

		/// <summary>
		/// Camera Up
		/// </summary>
		[JoinName("Up")]
		public JoinDataComplete Up = new JoinDataComplete(
			new JoinData
			{
				JoinNumber = 1,
				JoinSpan = 1
			},
			new JoinMetadata
			{
				Description = "Camera Up",
				JoinCapabilities = eJoinCapabilities.FromSIMPL,
				JoinType = eJoinType.Digital
			});

		/// <summary>
		/// Camera Down
		/// </summary>
		[JoinName("Down")]
		public JoinDataComplete Down = new JoinDataComplete(
			new JoinData
			{
				JoinNumber = 2,
				JoinSpan = 1
			},
			new JoinMetadata
			{
				Description = "Camera Down",
				JoinCapabilities = eJoinCapabilities.FromSIMPL,
				JoinType = eJoinType.Digital
			});

		/// <summary>
		/// Camera Left
		/// </summary>
		[JoinName("Left")]
		public JoinDataComplete Left = new JoinDataComplete(
			new JoinData
			{
				JoinNumber = 3,
				JoinSpan = 1
			},
			new JoinMetadata
			{
				Description = "Camera Left",
				JoinCapabilities = eJoinCapabilities.FromSIMPL,
				JoinType = eJoinType.Digital
			});

		/// <summary>
		/// Camera Right
		/// </summary>
		[JoinName("Right")]
		public JoinDataComplete Right = new JoinDataComplete(
			new JoinData
			{
				JoinNumber = 4,
				JoinSpan = 1
			},
			new JoinMetadata
			{
				Description = "Camera Right",
				JoinCapabilities = eJoinCapabilities.FromSIMPL,
				JoinType = eJoinType.Digital
			});

		/// <summary>
		/// Camera Zoom In
		/// </summary>
		[JoinName("ZoomIn")]
		public JoinDataComplete ZoomIn = new JoinDataComplete(
			new JoinData
			{
				JoinNumber = 5,
				JoinSpan = 1
			},
			new JoinMetadata
			{
				Description = "Camera Zoom In",
				JoinCapabilities = eJoinCapabilities.FromSIMPL,
				JoinType = eJoinType.Digital
			});

		/// <summary>
		/// Camera Zoom Out
		/// </summary>
		[JoinName("ZoomOut")]
		public JoinDataComplete ZoomOut = new JoinDataComplete(
			new JoinData
			{
				JoinNumber = 6,
				JoinSpan = 1
			},
			new JoinMetadata
			{
				Description = "Camera Zoom Out",
				JoinCapabilities = eJoinCapabilities.FromSIMPL,
				JoinType = eJoinType.Digital
			});

		/// <summary>
		/// Camera Online
		/// </summary>
		[JoinName("Online")]
		public JoinDataComplete Onlline = new JoinDataComplete(
			new JoinData
			{
				JoinNumber = 9,
				JoinSpan = 1
			},
			new JoinMetadata
			{
				Description = "Camera Online",
				JoinCapabilities = eJoinCapabilities.FromSIMPL,
				JoinType = eJoinType.Digital
			});

		/// <summary>
		/// Camera Preset Recall Start
		/// </summary>
		[JoinName("PresetRecallStart")]
		public JoinDataComplete PresetRecallStart = new JoinDataComplete(
			new JoinData
			{
				JoinNumber = 10,
				JoinSpan = 16
			},
			new JoinMetadata
			{
				Description = "Camera Preset Recall Start",
				JoinCapabilities = eJoinCapabilities.FromSIMPL,
				JoinType = eJoinType.Digital
			});

		/// <summary>
		/// Camera Preset Store Start
		/// </summary>
		[JoinName("PresetStoreStart")]
		public JoinDataComplete PresetStoreStart = new JoinDataComplete(
			new JoinData
			{
				JoinNumber = 30,
				JoinSpan = 16
			},
			new JoinMetadata
			{
				Description = "Camera Preset Store Start",
				JoinCapabilities = eJoinCapabilities.FromSIMPL,
				JoinType = eJoinType.Digital
			});

		/// <summary>
		/// Camera Privacy On
		/// </summary>
		[JoinName("PrivacyOn")]
		public JoinDataComplete PrivacyOn = new JoinDataComplete(
			new JoinData
			{
				JoinNumber = 48,
				JoinSpan = 1
			},
			new JoinMetadata
			{
				Description = "Camera Privacy On",
				JoinCapabilities = eJoinCapabilities.FromSIMPL,
				JoinType = eJoinType.Digital
			});

		/// <summary>
		/// Camera Privacy Off
		/// </summary>
		[JoinName("PrivacyOff")]
		public JoinDataComplete PrivacyOff = new JoinDataComplete(
			new JoinData
			{
				JoinNumber = 49,
				JoinSpan = 1
			},
			new JoinMetadata
			{
				Description = "Camera Privacy Off",
				JoinCapabilities = eJoinCapabilities.FromSIMPL,
				JoinType = eJoinType.Digital
			});

		#endregion


		#region Analog - Camera Joins



		#endregion


		#region Serial - Camera Joins

		/// <summary>
		/// Camera Preset Name Start
		/// </summary>
		[JoinName("PresetNameStart")]
		public JoinDataComplete PresetNameStart = new JoinDataComplete(
			new JoinData
			{
				JoinNumber = 2,
				JoinSpan = 16
			},
			new JoinMetadata
			{
				Description = "Camera Preset Name Start",
				JoinCapabilities = eJoinCapabilities.ToSIMPL,
				JoinType = eJoinType.Serial
			});

		#endregion

		/// <summary>
		/// Plugin device BridgeJoinMap constructor
		/// </summary>
		/// <param name="joinStart">This will be the join it starts on the EISC bridge</param>
		public QscQsysCameraBridgeJoinMap(uint joinStart)
			: base(joinStart, typeof(QscQsysCameraBridgeJoinMap))
		{
		}
	}
}