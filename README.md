![PepperDash Logo](/images/essentials-plugin-blue.png)

# QSC Q-Sys DSP Essentials Plugin for 4-Series (c) 2025

## License

Provided under MIT license

## Notes

Please refer to QSC Q-Sys plugin developer for questions and issues or use the "Issues" tab above.

## Device Specific Information

Update the below readme as needed to support documentation of the plugin

### Communication Settings

Update the communication settings as needed for the plugin being developed.

| Setting      | Value |
| ------------ | ----- |
| Delimiter    | "\n"  |
| Default IP   | NA    |
| Default Port | 1702  |
| Username     | NA    |
| Password     | NA    |

#### Plugin Valid Communication methods

Reference PepperDash Core eControlMethods Enum for valid values, (currently listed below). Update to reflect the valid values for the plugin device being developed.

```c#
Tcpip
```

### Plugin Configuration Object

Update the configuration object as needed for the plugin being developed.

```json
{
  "devices": [
    {
      "key": "dsp-1",
      "name": "QSC Q-Sys Essentials Plugin",
      "type": "qscdsp",
      "group": "pluginDevices",
      "properties": {
        "control": {
          "method": "tcpIp",
          "endOfLineString": "\n",
          "deviceReadyResponsePattern": "",
          "tcpSshProperties": {
            "address": "172.22.0.101",
            "port": 1702,
            "username": "",
            "password": "",
            "autoReconnect": true,
            "autoReconnectIntervalMs": 5000
          }
        },
        "prefix": "",
        "levelControlBlocks": {},
        "presets": {},
        "dialerControlBlock": {},
        "cameraControlBlocks": {}
      }
    }
  ]
}
```

### Plugin Level Control Configuration Object

```json
"properties": {
	"levelControlBlocks": {
		"fader-room": {
			"label": "Room",
			"levelInstanceTag": "ROOM_VOL",
			"muteInstanceTag": "ROOM_MUTE",
			"disabled": false,
			"hasLevel": true,
			"hasMute": true,
			"isMic": false,
			"useAbsoluteValue": false,
			"unmuteOnVolChange": true
		},
		"fader-program": {
			"label": "Program",
			"levelInstanceTag": "PGM_VOL",
			"muteInstanceTag": "PGM_MUTE",
			"disabled": false,
			"hasLevel": true,
			"hasMute": true,
			"isMic": false,
			"useAbsoluteValue": false,
			"unmuteOnVolChange": true
		},
		"fader-speech": {
			"label": "Speech",
			"levelInstanceTag": "SPEECH_VOL",
			"muteInstanceTag": "SPEECH_MUTE",
			"disabled": false,
			"hasLevel": true,
			"hasMute": true,
			"isMic": false,
			"useAbsoluteValue": false,
			"unmuteOnVolChange": true
		},
		"fader-phone-call": {
			"label": "Phone Call",
			"levelInstanceTag": "AC_RX_VOL",
			"muteInstanceTag": "AC_RX_MUTE",
			"disabled": false,
			"hasLevel": true,
			"hasMute": true,
			"isMic": false,
			"useAbsoluteValue": false,
			"unmuteOnVolChange": true
		},
		"fader-video-call": {
			"label": "Video Call",
			"levelInstanceTag": "VC_RX_VOL",
			"muteInstanceTag": "VC_RX_MUTE",
			"disabled": false,
			"hasLevel": true,
			"hasMute": true,
			"isMic": false,
			"useAbsoluteValue": false,
			"unmuteOnVolChange": true
		},
		"fader-privacy": {
			"label": "Privacy",
			"levelInstanceTag": "PRIVACY_VOL",
			"muteInstanceTag": "PRIVACY_MUTE",
			"disabled": false,
			"hasLevel": false,
			"hasMute": true,
			"isMic": true,
			"useAbsoluteValue": false,
			"unmuteOnVolChange": true
		},
		"fader-wireless-mics": {
			"label": "Wireless Mics",
			"levelInstanceTag": "WLESS_VOL",
			"muteInstanceTag": "WLESS_MUTE",
			"disabled": false,
			"hasLevel": true,
			"hasMute": true,
			"isMic": true,
			"useAbsoluteValue": false,
			"unmuteOnVolChange": true
		},
		"fader-8": {
			"label": "Fader 8",
			"disabled": true
		},
		"fader-9": {
			"label": "Fader 9",
			"disabled": true
		},
		"fader-10": {
			"label": "Fader 10",
			"disabled": true
		},
		"sourceControl-1": {
			"comment": "When using a fader for source selection you must include useAbsoluteValue:true",
			"label": "Room A Audio",
			"levelInstanceTag": "SOURCE_SELECT",
			"disabled": false,
			"hasLevel": false,
			"hasMute": false,
			"isMic": false,
			"useAbsoluteValue": true,
			"unmuteOnVolChange": false
		}
	}
}
```

### Plugin Preset Configuration Object

Presets can be handled two ways:

1. Point directly to the snapshot name using the first method below. This will not provide feedback.

```json
"properties": {
	"presets": {
		"preset-1": {
			"label": "System On",
			"preset": "PRESETS 1 0"
		},
		"preset-2": {
			"label": "System Off",
			"preset": "PRESETS 2 0"
		},
		"preset-3": {
			"label": "Default Levels",
			"preset": "PRESETS 3 0"
		}
	}
}
```

2. Another option is to give each preset a named control in the DSP. This is done on the QSC side by opening the snapshot component and giving the "Preset Recall" button a named control. This allows for true and half-state feedback. Half-state feedback is when the preset was the last recalled preset in the grouping but the DSP state no longer matches the preset state. To define presets in this manner use the following config object. Note it is placed with the level control blocks.

To control from SIMPL, use the analog level input and output on the object. Triggering any analog value above 0 will active the preset. Feedback is 0=inactive, 1=half-state, 2=active.

```json
"levelControlBlocks":  {
	"preset-1":	{
	    "label": "Default Levels",
	    "levelInstanceTag": "DEFAULT",
	    "disabled": false,
	    "hasLevel": true,
	    "useAbsoluteValue": true
	}
}
```

### Plugin Dialer Control Blocks

```json
"properties": {
	"dialerControlBlocks": {
		"dialer-1": {
			"ClearOnHangup": true,
			"incomingCallRingerTag": "VOIP_RINGING",
			"dialStringTag": "VOIP_DIALSTRING",
			"disconnectTag": "VOIP_DISCONNECT",
			"connectTag": "VOIP_CONNECT",
			"callStatusTag": "VOIP_STATUS",
			"hookStatusTag": "VOIP_OFFHOOK",
			"doNotDisturbTag": "VOIP_DND",
			"autoAnswerTag": "VOIP_AUTO_ANSWER",
			"keypadBackspaceTag": "VOIP_DIALSTRING_DEL",
			"keypadClearTag": "VOIP_DIALSTRING_CLEAR",
			"keypad1Tag": "VOIP_DTMF_1",
			"keypad2Tag": "VOIP_DTMF_2",
			"keypad3Tag": "VOIP_DTMF_3",
			"keypad4Tag": "VOIP_DTMF_4",
			"keypad5Tag": "VOIP_DTMF_5",
			"keypad6Tag": "VOIP_DTMF_6",
			"keypad7Tag": "VOIP_DTMF_7",
			"keypad8Tag": "VOIP_DTMF_8",
			"keypad9Tag": "VOIP_DTMF_9",
			"keypad0Tag": "VOIP_DTMF_0",
			"keypadStarTag": "VOIP_DTMF_*",
			"keypadPoundTag": "VOIP_DTMF_#"
		}
	}
}
```

### Plugin Camera Control Blocks

```json
"properties": {
	"cameraControlBlocks": {
		"camera-1": {
			"panLeftTag": "CAM01_LEFT",
			"panRightTag": "CAM01_RIGHT",
			"tiltUpTag": "CAM01_UP",
			"tiltDownTag": "CAM01_DOWN",
			"zoomInTag": "CAM01_ZOOMIN",
			"zoomOutTag": "CAM01_ZOOMOUT",
			"privacy": "CAM01_PRIVACY",
			"onlineStatus": "CAM01_STATUS",
			"presets": {
				"preset01": {
					"label": "Default",
					"bank": "CAM01_PRESETS",
					"number": 1
				},
				"preset02": {
					"label": "Tight",
					"bank": "CAM01_PRESETS",
					"number": 2
				},
				"preset03": {
					"label": "Wide",
					"bank": "CAM01_PRESETS",
					"number": 3
				},
				"preset04": {
					"label": "User",
					"bank": "CAM01_PRESETS",
					"number": 4
				}
			}
		}
	}
}
```

### Plugin Bridge Configuration Object

Update the bridge configuration object as needed for the plugin being developed.

```json
{
  "devices": [
    {
      "key": "dsp-1-bridge",
      "name": "QSC Q-Sys Essentials Plugin Bridge",
      "group": "api",
      "type": "eiscApi",
      "properties": {
        "control": {
          "ipid": "1A",
          "tcpSshProperties": {
            "address": "127.0.0.2",
            "port": 0
          }
        },
        "devices": [
          {
            "deviceKey": "dsp-1",
            "joinStart": 1
          }
        ]
      }
    }
  ]
}
```

### SiMPL EISC Bridge Map

The selection below documents the digital, analog, and serial joins used by the SiMPL EISC. Update the bridge join maps as needed for the plugin being developed.

When instantiating multiple dialers joins start @ 3100 and use digital/analog/serial joins in blocks of 50. For example, Dialer 2 would start @ 3150.

#### **Digitals**

| dig-o (Input/Triggers)         | I/O         | dig-i (Feedback)                        |
| ------------------------------ | ----------- | --------------------------------------- |
|                                | 1           | Is Online Feedback                      |
| Run Preset by Number           | 100 - 199   |                                         |
|                                | 200 - 399   | Fader [n] Visible Feedback              |
| Fader [n] Mute Toggle          | 400 - 599   | Fader [n] Mute Toggle Feedback          |
| Fader [n] Mute On              | 600 - 799   | Fader [n] Mute On Feedback              |
| Fader [n] Mute Off             | 800 - 999   | Fader [n] Mute Off Feddback             |
| Fader [n] Level Increment      | 1000 - 1199 |                                         |
| Fader [n] Level Decrement      | 1200 - 1399 |                                         |
|                                | 3100        | Dialer Incoming Call Feedback           |
| Dialer 1 End Call              | 3107        |                                         |
| Dialer 1 Keypad 0              | 3110        |                                         |
| Dialer 1 Keypad 1              | 3111        |                                         |
| Dialer 1 Keypad 2              | 3112        |                                         |
| Dialer 1 Keypad 3              | 3113        |                                         |
| Dialer 1 Keypad 4              | 3114        |                                         |
| Dialer 1 Keypad 5              | 3115        |                                         |
| Dialer 1 Keypad 6              | 3116        |                                         |
| Dialer 1 Keypad 7              | 3117        |                                         |
| Dialer 1 Keypad 8              | 3118        |                                         |
| Dialer 1 Keypad 9              | 3119        |                                         |
| Dialer 1 Keypad \* (Start)     | 3120        |                                         |
| Dialer 1 Keypad # (Pound)      | 3121        |                                         |
| Dialer 1 Keypad Clear          | 3122        |                                         |
| Dialer 1 Keypad Backspace      | 3123        |                                         |
| Dialer 1 Dial/End Call         | 3124        | Dialer 1 Dial Feedback                  |
| Dialer 1 Auto Answer On        | 3125        | Dialer 1 Auto Answer On Feedback        |
| Dialer 1 Auto Answer Off       | 3126        | Dialer 1 Auto Answer Off Feedback       |
| Dialer 1 Auto Answer Toggle    | 3127        | Dialer 1 Auto Answer Toggle Feedback    |
| Dialer 1 On Hook               | 3129        | Dialer 1 On Hook Feedback               |
| Dialer 1 Off Hook              | 3130        | Dialer 1 Off Hook Feedback              |
| Dialer 1 Do Not Disturb Toggle | 3132        | Dialer 1 Do Not Distrub Toggle Feedback |
| Dialer 1 Do Not Disturb On     | 3133        | Dialer 1 Do Not Distrub On Feedback     |
| Dialer 1 Do Not Disturb Off    | 3134        | Dialer 1 Do Not Distrub Off Feedback    |

#### **Analogs**

| an_o (Input/Triggers) | I/O       | an_i (Feedback)          |
| --------------------- | --------- | ------------------------ |
| Fader [n] Level Set   | 200 - 399 | Fader [n] Level Feedback |
|                       | 400 - 599 | Fader [n] Type Feedback  |

#### **Serials**

| serial-o (Input/Triggers) | I/O       | serial-i (Feedback)                |
| ------------------------- | --------- | ---------------------------------- |
| DSP IP Address            | 1         |                                    |
| DSP Prefix                | 2         |                                    |
| Run Preset by Name        | 100       |                                    |
|                           | 100 - 199 | Preset [n] Name Feedback           |
|                           | 200 - 399 | Fader [n] Name Feedback            |
|                           | 3100      | Dialer 1 Dial String Feedback      |
|                           | 3104      | Dialer 1 Caller ID Number Feedback |
<!-- START Minimum Essentials Framework Versions -->
### Minimum Essentials Framework Versions

- 2.0.0
<!-- END Minimum Essentials Framework Versions -->
<!-- START Config Example -->
### Config Example

```json
{
    "key": "GeneratedKey",
    "uid": 1,
    "name": "GeneratedName",
    "type": "QscDialer",
    "group": "Group",
    "properties": {
        "ClearOnHangup": true,
        "incomingCallRingerTag": "SampleString",
        "dialStringTag": "SampleString",
        "disconnectTag": "SampleString",
        "connectTag": "SampleString",
        "callStatusTag": "SampleString",
        "hookStatusTag": "SampleString",
        "doNotDisturbTag": "SampleString",
        "autoAnswerTag": "SampleString",
        "keypadBackspaceTag": "SampleString",
        "keypadClearTag": "SampleString",
        "keypad1Tag": "SampleString",
        "keypad2Tag": "SampleString",
        "keypad3Tag": "SampleString",
        "keypad4Tag": "SampleString",
        "keypad5Tag": "SampleString",
        "keypad6Tag": "SampleString",
        "keypad7Tag": "SampleString",
        "keypad8Tag": "SampleString",
        "keypad9Tag": "SampleString",
        "keypad0Tag": "SampleString",
        "keypadPoundTag": "SampleString",
        "keypadStarTag": "SampleString"
    }
}
```
<!-- END Config Example -->
<!-- START Supported Types -->

<!-- END Supported Types -->
<!-- START Join Maps -->

<!-- END Join Maps -->
<!-- START Interfaces Implemented -->
### Interfaces Implemented

- IHasDialer
- IBridgeAdvanced
- IOnline
- ICommunicationMonitor
- IBasicVolumeWithFeedback
- IKeyName
<!-- END Interfaces Implemented -->
<!-- START Base Classes -->
### Base Classes

- ReconfigurableDevice
- Device
- JoinMapBase
- DspControlPoint
- QscDspControlPoint
<!-- END Base Classes -->
<!-- START Public Methods -->
### Public Methods

- public void OnCallStatusChange(CodecCallStatusItemChangeEventArgs args)
- public void Subscribe()
- public void ParseSubscriptionMessage(string customName, string value)
- public void DoNotDisturbToggle()
- public void DoNotDisturbOn()
- public void DoNotDisturbOff()
- public void AutoAnswerToggle()
- public void AutoAnswerOn()
- public void AutoAnswerOff()
- public void SendKeypad(EKeypadKeys button)
- public void SendSubscriptionCommand(string instanceTag)
- public void Dial()
- public void Dial(string number)
- public void EndCall(CodecActiveCallItem item)
- public void EndAllCalls()
- public void AcceptCall()
- public void AcceptCall(CodecActiveCallItem item)
- public void RejectCall()
- public void RejectCall(CodecActiveCallItem item)
- public void SendDtmf(string digit)
- public void CreateDspObjects()
- public void SetIpAddress(string hostname)
- public void SetPrefix(string prefix)
- public void StatusGet(bool enable)
- public void WriteConfig()
- public void ProcessSimulatedRx(string s)
- public void SendLine(string s)
- public void EnqueueCommand(QueuedCommand commandToEnqueue)
- public void EnqueueCommand(string command)
- public void AddPreset(QscDspPresets s)
- public void RunPresetNumber(ushort n)
- public void RunPreset(string name)
- public void SavePresetNumber(ushort n)
- public void SavePreset(string name)
- public void LinkToApi(BasicTriList trilist, uint joinStart, string joinMapKey, EiscApiAdvanced bridge)
- public void MoveCamera(eCameraPtzControls button)
- public void PrivacyOn()
- public void PrivacyOff()
- public void RecallPreset(ushort presetNumber)
- public void SavePreset(ushort presetNumber)
- public void WritePresetName(string newLabel, ushort presetNumber)
- public void Subscribe()
- public void ParseSubscriptionMessage(string customName, string value, string absoluteValue)
- public void LinkToApi(BasicTriList trilist, uint joinStart, string joinMapKey, EiscApiAdvanced bridge)
- public void Initialize()
- public void ParseGetMessage(string attributeCode, string message)
- public void Initialize(QscDspLevelControlBlockConfig config)
- public void Subscribe()
- public void ParseSubscriptionMessage(string customName, string value, string absoluteValue)
- public void MuteOff()
- public void MuteOn()
- public void SetVolume(ushort level)
- public void MuteToggle()
- public void VolumeUpRepeat(object callbackObject)
- public void VolumeDownRepeat(object callbackObject)
- public void VolumeDown(bool press)
- public void VolumeUp(bool press)
- public void VolumeRampStop(object callbackObject)
<!-- END Public Methods -->
<!-- START Bool Feedbacks -->
### Bool Feedbacks

- OffHookFeedback
- AutoAnswerFeedback
- DoNotDisturbFeedback
- IncomingCallFeedback
- IsPrimaryFeedback
- IsActiveFeedback
- IsOnline
- IsOnline
- MuteFeedback
<!-- END Bool Feedbacks -->
<!-- START Int Feedbacks -->
### Int Feedbacks

- VolumeLevelFeedback
<!-- END Int Feedbacks -->
<!-- START String Feedbacks -->
### String Feedbacks

- DialStringFeedback
- CallerIdNumberFeedback
- LabelFeedback
<!-- END String Feedbacks -->
