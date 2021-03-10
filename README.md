# QSC Q-Sys DSP Essentials Plugin (c) 2021

## License

Provided under MIT license

## Notes
Please refer to QSC Q-Sys plugin developer for questions and issues or use the "Issues" tab above.

## Device Specific Information

Update the below readme as needed to support documentation of the plugin

### Communication Settings

Update the communication settings as needed for the plugin being developed.

| Setting      | Value       |
|--------------|-------------|
| Delimiter    | "\n"        |
| Default IP   | NA          |
| Default Port | 1702        |
| Username     | NA          |
| Password     | NA          |

#### Plugin Valid Communication methods

Reference PepperDash Core eControlMethods Enum for valid values, (currently listed below).  Update to reflect the valid values for the plugin device being developed.

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
		"fader-1": {
			"label": "Room",
			"levelInstanceTag": "ROOM_VOL",
			"muteInstanceTag": "ROOM_MUTE",
			"disabled": false,
			"hasLevel": true,
			"hasMute": true,
			"isMic": false
		},
		"fader-2": {
			"label": "Program",
			"levelInstanceTag": "PGM_VOL",
			"muteInstanceTag": "PGM_MUTE",
			"disabled": false,
			"hasLevel": true,
			"hasMute": true,
			"isMic": false
		},
		"fader-3": {
			"label": "Speech",
			"levelInstanceTag": "SPEECH_VOL",
			"muteInstanceTag": "SPEECH_MUTE",
			"disabled": false,
			"hasLevel": true,
			"hasMute": true,
			"isMic": false
		},
		"fader-4": {
			"label": "Phone Call",
			"levelInstanceTag": "AC_RX_VOL",
			"muteInstanceTag": "AC_RX_MUTE",
			"disabled": false,
			"hasLevel": true,
			"hasMute": true,
			"isMic": false
		},
		"fader-5": {
			"label": "Video Call",
			"levelInstanceTag": "VC_RX_VOL",
			"muteInstanceTag": "VC_RX_MUTE",
			"disabled": false,
			"hasLevel": true,
			"hasMute": true,
			"isMic": false
		},
		"fader-6": {
			"label": "Privacy",
			"levelInstanceTag": "PRIVACY_VOL",
			"muteInstanceTag": "PRIVACY_MUTE",
			"disabled": false,
			"hasLevel": false,
			"hasMute": true,
			"isMic": true
		},
		"fader-7": {
			"label": "Wireless Mics",
			"levelInstanceTag": "WLESS_VOL",
			"muteInstanceTag": "WLESS_MUTE",
			"disabled": false,
			"hasLevel": true,
			"hasMute": true,
			"isMic": true
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
			"label": "Room A Audio",
			"levelInstanceTag": "SOURCE_SELECT",
			"disabled": false,
			"hasLevel": true,
			"hasMute": false,
			"isMic": false,
			"useAbsoluteValue": true,
		}
	}
}
```


### Plugin Preset Configuration Object
Presets can be handled two ways:
1) Point directly to the snapshot name using the first method below. This will not provide feedback.

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

2) Another option is to give each preset a named control in the DSP. This is done on the QSC side by opening the snapshot component and giving the "Preset Recall" button a named control. This allows for true and half-state feedback. Half-state feedback is when the preset was the last recalled preset in the grouping but the DSP state no longer matches the preset state. To define presets in this manner use the following config object. Note it is placed with the level control blocks.

To control from SIMPL, use the analog level input and output on the object. Triggering any analog value above 0 will active the preset. Feedback is 0=inactive, 1=half-state, 2=active.

```
    "levelControlBlocks": 
    {
	"preset-1": 
	{
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

#### Digitals
| dig-o (Input/Triggers)                | I/O  | dig-i (Feedback) |
|---------------------------------------|----- |------------------|
|                                       | 1    | Is Online        |
|                                       | 2    |                  |
|                                       | 3    |                  |
|                                       | 4    |                  |
|                                       | 5    |                  |
| Incoming Call Accept                  | 3136 |                  |
| Incoming Call Reject                  | 3137 |                  |

#### Analogs
| an_o (Input/Triggers) | I/O | an_i (Feedback) |
|-----------------------|-----|-----------------|
|                       | 1   |                 |
|                       | 2   |                 |
|                       | 3   |                 |
|                       | 4   |                 |
|                       | 5   |                 |


#### Serials
| serial-o (Input/Triggers) | I/O | serial-i (Feedback) |
|---------------------------|-----|---------------------|
|                           | 1   |                     |
|                           | 2   |                     |
|                           | 3   |                     |
|                           | 4   |                     |
|                           | 5   |                     |

