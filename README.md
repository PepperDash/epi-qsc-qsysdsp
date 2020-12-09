# QSC Q-Sys DSP Essentials Plugin (c) 2020

## License

Provided under MIT license

## Notes

This is a direct port from BB Cloud repo, which has since been archived.  The following updates were made in the port:
1. Removed customer reference from all filenames
2. Changed named space from CUSTOMER_REF.QSC.DSP to QscQsysDsp
2. Added .github actions
3. Added .nuspec file

Link to archived BB Cloud repo:
https://bitbucket.org/Pepperdash_Products/archive-kpmg.qsc.dsp.epi/src/master/

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
					"tcpSshProperties": {
						"address": "172.22.0.101",
						"port": 1702,
						"username": "",
						"password": "",
						"autoReconnect": true,
						"autoReconnectIntervalMs": 5000
					}
				}
			}
		}		
	]
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
| dig-o (Input/Triggers)                | I/O | dig-i (Feedback) |
|---------------------------------------|-----|------------------|
|                                       | 1   | Is Online        |
|                                       | 2   |                  |
|                                       | 3   |                  |
|                                       | 4   |                  |
|                                       | 5   |                  |
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

