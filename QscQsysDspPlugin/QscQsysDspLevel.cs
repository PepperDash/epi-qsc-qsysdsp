using System;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro.DM;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Devices.Common.VideoCodec.Cisco;

namespace QscQsysDspPlugin
{
	/// <summary>
	/// Basic Level Interface
	/// </summary>
	public interface IQscQsysDspBasicLevel : IBasicVolumeWithFeedback
	{
		/// <summary>
		/// Level instance tag
		/// </summary>
		string LevelInstanceTag { get; set; }
		/// <summary>
		/// Mute instance tag
		/// </summary>
		string MuteInstanceTag { get; set; }
		/// <summary>
		/// Has Level boolean flag
		/// </summary>
		bool HasLevel { get; set; }
		/// <summary>
		/// Has Mute boolean flag
		/// </summary>
		bool HasMute { get; set; }
		/// <summary>
		/// Automatic Unmute on Volume Up boolean flag
		/// </summary>
		bool AutomaticeUnmuteOnVolumeUp { get; }
	}

	/// <summary>
	/// Level type enum
	/// </summary>
	public enum EDspLevelTypes
	{
		/// <summary>
		/// Speaker enum value
		/// </summary>
		Speaker = 0,
		/// <summary>
		/// Microphone enum value
		/// </summary>
		Microphone = 1
	}

	/// <summary>
	/// Level control class
	/// </summary>
	public class QscQsysDspLevelControl : QscQsysDspControlPoint, IBasicVolumeWithFeedback, IKeyed
	{
		/// <summary>
		/// Enabled flag
		/// </summary>
		public bool Enabled { get; set; }

		/// <summary>
		/// Use Aboslute Value flag
		/// </summary>
		public bool UseAbsoluteValue { get; set; }

		/// <summary>
		/// Level type (Speaker/Microphone) enum
		/// </summary>
		public EDspLevelTypes Type;

		/// <summary>
		/// Is subscribed flag
		/// </summary>
		public bool IsSusbscribed
		{
			get
			{
				var isSubscribed = HasMute && _muteIsSubscribed || HasLevel && _levelIsSubscribed;
				return isSubscribed;
			}
		}

		#region level

		private CTimer _volumeUpRepeatTimer;
		private CTimer _volumeDownRepeatTimer;

		private bool _levelIsSubscribed;
		private ushort _volumeLevel;

		/// <summary>
		/// Volume level property
		/// </summary>
		public ushort VolumeLevel
		{
			get { return _volumeLevel; }
			set
			{
				if (_volumeLevel == value) return;
				_volumeLevel = value;
				_levelIsSubscribed = true;
				VolumeLevelFeedback.FireUpdate();
			}
		}

		/// <summary>
		/// Has level flag
		/// </summary>
		public bool HasLevel { get; private set; }

		/// <summary>
		/// Automatic unmute on volume up flag
		/// </summary>
		public bool AutomaticUnmuteOnVolumeUp { get; private set; }

		/// <summary>
		/// Used to identify level subscription values
		/// </summary>
		public string LevelCustomName { get; private set; }

		/// <summary>
		/// Volume Level feedback
		/// </summary>
		public IntFeedback VolumeLevelFeedback { get; private set; }

		#endregion

		#region mute

		private bool _muteIsSubscribed;
		private bool _isMuted;

		/// <summary>
		/// Mute property
		/// </summary>
		public bool IsMuted
		{
			get { return _isMuted; }
			set
			{
				if (_isMuted == value) return;
				_isMuted = value;
				_muteIsSubscribed = true;
				MuteFeedback.FireUpdate();
			}
		}

		/// <summary>
		/// Has Mute flag
		/// </summary>
		public bool HasMute { get; private set; }

		/// <summary>
		/// Used to identify mute subscription values
		/// </summary>
		public string MuteCustomName { get; private set; }

		/// <summary>
		/// Volume mute feedback
		/// </summary>
		public BoolFeedback MuteFeedback { get; private set; }

		#endregion


		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="key">instance name</param>
		/// <param name="config">level control block config</param>
		/// <param name="parent">parent dsp instance</param>
		public QscQsysDspLevelControl(string key, QscQsysDspLevelControlBlockConfig config, QscQsysDsp parent)
			: base(config.LevelInstanceTag, config.MuteInstanceTag, parent)
		{
			if (config.Disabled) return;
			Initialize(key, config);
		}

		/// <summary>
		/// Initializes this attribute based on config values and generates subscription commands and adds commands to the parents queue.
		/// </summary>
		/// <param name="key">instance key</param>
		/// <param name="config">level control block configuration object</param>
		public void Initialize(string key, QscQsysDspLevelControlBlockConfig config)
		{
			Key = string.Format("{0}-{1}", Parent.Key, key);
			Enabled = true;

			DeviceManager.AddDevice(this);

			Type = config.IsMic ? EDspLevelTypes.Microphone : EDspLevelTypes.Speaker;

			Debug.Console(2, this, "Adding LevelControl {0}", Key);

			IsSubscribed = false;

			VolumeLevelFeedback = new IntFeedback(() => _volumeLevel);
			MuteFeedback = new BoolFeedback(() => _isMuted);

			_volumeUpRepeatTimer = new CTimer(VolumeUpRepeat, Timeout.Infinite);
			_volumeDownRepeatTimer = new CTimer(VolumeDownRepeat, Timeout.Infinite);

			LevelCustomName = config.Label;
			MuteCustomName = config.Label;
			HasLevel = config.HasLevel;
			HasMute = config.HasMute;
			UseAbsoluteValue = config.UseAbsoluteValue;
			AutomaticUnmuteOnVolumeUp = config.UnmuteOnVolChange;
		}

		/// <summary>
		/// Subscribes this level control object as configurec
		/// </summary>
		public void Subscribe()
		{
			// subscribe to level
			if (HasLevel)
			{
				SendSubscriptionCommand(LevelInstanceTag, "1");
			}

			// subscribe to mute
			if (HasMute)
			{
				SendSubscriptionCommand(MuteInstanceTag, "1");
			}
		}

		/// <summary>
		/// Parses response from DSP Base
		/// </summary>
		/// <param name="customName"></param>
		/// <param name="value"></param>
		/// <param name="absoluteValue"></param>
		public void ParseSubscriptionMessage(string customName, string value, string absoluteValue)
		{
			Debug.Console(1, this, "NamedControl {0} Response: {1}", customName, value);

			// level response using relative value
			if (customName == LevelInstanceTag)			
			{
				switch (UseAbsoluteValue)
				{
					case true:
					{
						VolumeLevel = ushort.Parse(absoluteValue);
						break;
					}
					case false:
					{
						var level = Double.Parse(value);
						VolumeLevel = (ushort)(level * 65535);
						break;
					}
				}

				Debug.Console(1, this, "NamedControl {0} Level: '{1}'", customName, VolumeLevel);
			}
			// mute response
			else if (customName == MuteInstanceTag)
			{
				switch (value)
				{
					case "muted":
					{
						IsMuted = true;
						break;
					}
					case "unmuted":
					{
						IsMuted = false;
						break;
					}
				}

				Debug.Console(1, this, "NamedControl {0} Mute: '{1}'", customName, IsMuted.ToString());
			}
		}

		/// <summary>
		/// Sets the volume to the specified level
		/// </summary>
		/// <param name="level"></param>
		public void SetVolume(ushort level)
		{
			if (AutomaticUnmuteOnVolumeUp && IsMuted)
				MuteOff();

			if (!UseAbsoluteValue)
			{
				var scaledLevel = ScaleLevel(level);
				SendFullCommand("csp", LevelInstanceTag, string.Format("{0}", scaledLevel));
			}
			else
			{
				SendFullCommand("csv", LevelInstanceTag, string.Format("{0}", level));
			}
		}

		// scales value to a double
		private double ScaleLevel(double value)
		{
			Debug.Console(1, this, "ScaleLevel(value: {0})", value);

			var scaledValue = (value / 65535);
			return scaledValue;
		}

		/// <summary>
		/// Increments volume level
		/// </summary>
		/// <param name="callbackObject"></param>
		public void VolumeUpRepeat(object callbackObject)
		{
			VolumeUp(true);
		}

		/// <summary>
		/// Increments volume level
		/// </summary>
		/// <param name="press"></param>
		public void VolumeUp(bool press)
		{
			if (press)
			{
				_volumeUpRepeatTimer.Reset(100);
				SendFullCommand("css ", LevelInstanceTag, "++");
			}
			else
			{
				_volumeUpRepeatTimer.Stop();
			}
		}

		/// <summary>
		/// Decrements the volume
		/// </summary>
		/// <param name="callbackObject"></param>
		public void VolumeDownRepeat(object callbackObject)
		{
			VolumeDown(true);
		}

		/// <summary>
		/// Decrements volume level
		/// </summary>
		/// <param name="press"></param>
		public void VolumeDown(bool press)
		{
			if (press)
			{
				_volumeDownRepeatTimer.Reset(100);
				SendFullCommand("css ", LevelInstanceTag, "--");
			}
			else
			{
				_volumeUpRepeatTimer.Stop();
			}
		}

		/// <summary>
		/// Sets the mute state on
		/// </summary>
		public void MuteOn()
		{
			SendFullCommand("csv", MuteInstanceTag, "1");
		}

		/// <summary>
		/// Sets the mute state off
		/// </summary>
		public void MuteOff()
		{
			SendFullCommand("csv", MuteInstanceTag, "0");
		}

		/// <summary>
		/// Toggles the mute state
		/// </summary>
		public void MuteToggle()
		{
			SendFullCommand("csv", MuteInstanceTag, IsMuted ? "0" : "1");
		}
	}
}