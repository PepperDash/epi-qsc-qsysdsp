using PepperDash.Core;

namespace PepperDash.Essentials.Plugins.Qsc.Qsys
{
	/// <summary>
	/// Wraps a QsysPresets config entry with a Key, for compatibility with IDspPresets and Mobile Control/Room Plugin frameworks
	/// </summary>
	public class QsysPreset : QsysPresets, IKeyName
	{
		public string Key { get; private set; }
		public string Name => base.Label;

		public QsysPreset(string key) : base()
		{
			Key = key;
		}
	}
}
