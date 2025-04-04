﻿using System;
using Crestron.SimplSharp;
using PepperDash.Core;
using PepperDash.Essentials.Core;

namespace QscQsysDspPlugin
{
	public class QscDspLevelControl : QscDspControlPoint, IBasicVolumeWithFeedback, IKeyName
	{
		bool _isMuted;
		ushort _volumeLevel;

        const ushort _rampResetTime = 100;

		public BoolFeedback MuteFeedback { get; private set; }

		public IntFeedback VolumeLevelFeedback { get; private set; }

		public bool Enabled { get; set; }
		public bool UseAbsoluteValue { get; set; }

		public ePdtLevelTypes Type;
		CTimer _volumeUpRepeatTimer;
		CTimer _volumeDownRepeatTimer;
        CTimer _volumeRampDelay;
	    private readonly QscDsp _parent;

        bool _volumeRampTracker;

		/// <summary>
		/// Used for to identify level subscription values
		/// </summary>
		public string LevelCustomName { get; private set; }

		/// <summary>
		/// Used for to identify mute subscription values
		/// </summary>
		public string MuteCustomName { get; private set; }

		/// <summary>
		/// Minimum fader level
		/// </summary>
		//double MinLevel;

		/// <summary>
		/// Maximum fader level
		/// </summary>
		//double MaxLevel;

		/// <summary>
		/// Checks if a valid subscription string has been recieved for all subscriptions
		/// </summary>
		public bool IsSubsribed
		{
			get
			{
				//bool isSubscribed = false;

				//if (HasMute && MuteIsSubscribed)
				//    isSubscribed = true;

				//if (HasLevel && LevelIsSubscribed)
				//    isSubscribed = true;

				//return isSubscribed;

				var isSubscribed = HasMute && _muteIsSubscribed || HasLevel && _levelIsSubscribed;
				return isSubscribed;
			}
		}

		public bool AutomaticUnmuteOnVolumeUp { get; private set; }

		public bool HasMute { get; private set; }
		public bool HasLevel { get; private set; }

        public string Name => Key;

        bool _muteIsSubscribed;
		bool _levelIsSubscribed;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="key">instance key</param>
        /// <param name="config">level control block configuration object</param>
        /// <param name="parent">dsp parent isntance</param>
        public QscDspLevelControl(string key, QscDspLevelControlBlockConfig config, QscDsp parent)
            : base(key, config.LevelInstanceTag, config.MuteInstanceTag, parent)
        {
            _parent = parent;
            if (config.Disabled)
                return;

            parent.CommunicationMonitor.IsOnlineFeedback.OutputChange += (sender, args) =>
            {
                if (!args.BoolValue)
                    return;

                CrestronInvoke.BeginInvoke(o =>
                {
                    if (!String.IsNullOrEmpty(config.LevelInstanceTag) && config.HasLevel)
                        _parent.SendLine(String.Format("cg \"{0}\"", config.LevelInstanceTag));

                    if (!String.IsNullOrEmpty(config.MuteInstanceTag) && config.HasMute)
                        _parent.SendLine(String.Format("cg \"{0}\"", config.MuteInstanceTag));
                });
            };

            Initialize(config);
        }

        /// <summary>
        /// Initializes this attribute based on config values and generates subscriptions commands and adds commands to the parent's queue.
        /// </summary>
        /// <param name="key">instance key</param>
        /// <param name="config">level control block configuration object</param>
        public void Initialize(QscDspLevelControlBlockConfig config)
        {            
            Enabled = true;
            DeviceManager.AddDevice(this);
            Type = config.IsMic ? ePdtLevelTypes.Microphone : ePdtLevelTypes.Speaker;

            Debug.Console(2, this, "Adding LevelControl '{0}'", Key);

            this.IsSubscribed = false;

            MuteFeedback = new BoolFeedback(() => _isMuted);

            VolumeLevelFeedback = new IntFeedback(() => _volumeLevel);

            _volumeUpRepeatTimer = new CTimer(VolumeUpRepeat, Timeout.Infinite);
            _volumeDownRepeatTimer = new CTimer(VolumeDownRepeat, Timeout.Infinite);

            _volumeRampDelay = new CTimer(VolumeRampStop, Timeout.Infinite);
            LevelCustomName = config.Label;
            HasMute = config.HasMute;
            HasLevel = config.HasLevel;
            UseAbsoluteValue = config.UseAbsoluteValue;
        }

        /// <summary>
        /// Subscribes this level control object as configured
        /// </summary>
        public void Subscribe()
		{
			// Do subscriptions and blah blah
			// Subscribe to mute
			if (this.HasMute)
			{
				SendSubscriptionCommand(this.MuteInstanceTag);
			}

			// Subscribe to level
			if (this.HasLevel)			{

				SendSubscriptionCommand(this.LevelInstanceTag);
			}
		}


		/// <summary>
		/// Parses the response from the DspBase
		/// </summary>
		/// <param name="customName"></param>
		/// <param name="value"></param>
		/// <param name="absoluteValue"></param>
		public void ParseSubscriptionMessage(string customName, string value, string absoluteValue)
		{
			// Check for valid subscription response
			Debug.Console(1, this, "Level {0} Response: '{1}'", customName, value);
			if (
                !String.IsNullOrEmpty(MuteInstanceTag) 
                && customName.Equals(MuteInstanceTag, StringComparison.OrdinalIgnoreCase))
			{
			    switch (value)
			    {
			        case "true":
			        case "muted":
			            _isMuted = true;
			            _muteIsSubscribed = true;
			            break;
			        case "false":
			        case "unmuted":
			            _isMuted = false;
			            _muteIsSubscribed = true;
			            break;
			    }

			    MuteFeedback.FireUpdate();
			}
            else if (
                !String.IsNullOrEmpty(LevelInstanceTag) 
                && customName.Equals(LevelInstanceTag, StringComparison.OrdinalIgnoreCase) 
                && !UseAbsoluteValue)
			{
				var parsedValue = Double.Parse(value);

                _volumeLevel = (ushort)(parsedValue * 65535);
				Debug.Console(1, this, "Level {0} VolumeLevel: '{1}'", customName, _volumeLevel);
				_levelIsSubscribed = true;

				VolumeLevelFeedback.FireUpdate();
			}
			else if (
                !String.IsNullOrEmpty(LevelInstanceTag)
                && customName.Equals(LevelInstanceTag, StringComparison.OrdinalIgnoreCase) 
                && UseAbsoluteValue)
			{

				_volumeLevel = ushort.Parse(absoluteValue);
				Debug.Console(1, this, "Level {0} VolumeLevel: '{1}'", customName, _volumeLevel);
				_levelIsSubscribed = true;

				VolumeLevelFeedback.FireUpdate();
			}
		}

		/// <summary>
		/// Turns the mute off
		/// </summary>
		public void MuteOff()
		{
			SendFullCommand("csv", this.MuteInstanceTag, "0");
		}

		/// <summary>
		/// Turns the mute on
		/// </summary>
		public void MuteOn()
		{
			SendFullCommand("csv", this.MuteInstanceTag, "1");
		}

		/// <summary>
		/// Sets the volume to a specified level
		/// </summary>
		/// <param name="level"></param>
		public void SetVolume(ushort level)
		{
			Debug.Console(1, this, "volume: {0}", level);
			// Unmute volume if new level is higher than existing
			if (AutomaticUnmuteOnVolumeUp && _isMuted)
			{
				MuteOff();
			}
			if (!UseAbsoluteValue)
			{
				var newLevel = Scale(level);
				Debug.Console(1, this, "newVolume: {0}", newLevel);
				SendFullCommand("csp", this.LevelInstanceTag, string.Format("{0}", newLevel));
			}
			else
			{
				SendFullCommand("csv", this.LevelInstanceTag, string.Format("{0}", level));
			}
		}

		/// <summary>
		/// Toggles mute status
		/// </summary>
		public void MuteToggle()
		{
			SendFullCommand("csv", this.MuteInstanceTag, _isMuted ? "0" : "1");
		}

		/// <summary>
		/// Increments volume level
		/// </summary>
		/// <param name="callbackObject"></param>
		public void VolumeUpRepeat(object callbackObject)
		{
			this.VolumeUp(true);
		}

		/// <summary>
		/// Decrements volume level
		/// </summary>
		/// <param name="callbackObject"></param>
		public void VolumeDownRepeat(object callbackObject)
		{
			this.VolumeDown(true);
		}

		/// <summary>
		/// Decrements volume level
		/// </summary>
		/// <param name="press"></param>
		public void VolumeDown(bool press)
		{
			if (press)
			{
                _volumeRampTracker = true;
                _volumeUpRepeatTimer.Stop();

                _volumeDownRepeatTimer.Reset(_rampResetTime);
				SendFullCommand("css ", this.LevelInstanceTag, "--");
			}
			else
			{
                _volumeRampTracker = false;
				_volumeDownRepeatTimer.Stop();
                _volumeRampDelay.Reset(200);
				// VolumeDownRepeatTimer.Dispose();
			}
		}

		/// <summary>
		/// Increments volume level
		/// </summary>
		/// <param name="press"></param>
		public void VolumeUp(bool press)
		{
			if (press)
			{
                _volumeRampTracker = true;
                _volumeDownRepeatTimer.Stop();

                _volumeUpRepeatTimer.Reset(_rampResetTime);
				SendFullCommand("css ", this.LevelInstanceTag, "++");

				if (AutomaticUnmuteOnVolumeUp && !_isMuted) MuteOff();
			}
			else
			{
                _volumeRampTracker = false;
				_volumeUpRepeatTimer.Stop();
                _volumeRampDelay.Reset(500);
			}
		}

        public void VolumeRampStop(object callbackObject)
        {
            if (_volumeRampTracker == true) return;

            _volumeUpRepeatTimer.Stop();
            _volumeDownRepeatTimer.Stop();
        }
		
		/// <summary>
		/// Scales the input provided
		/// </summary>
		/// <param name="input"></param>
		/// <returns></returns>
		double Scale(double input)
		{
			Debug.Console(1, this, "Scaling (double) input '{0}'", input);

			var output = (input / 65535);

			Debug.Console(1, this, "Scaled output '{0}'", output);

			return output;
		}
	}

	/// <summary>
	/// Level type enum
	/// </summary>
	public enum ePdtLevelTypes
	{
		Speaker = 0,
		Microphone = 1
	}
}