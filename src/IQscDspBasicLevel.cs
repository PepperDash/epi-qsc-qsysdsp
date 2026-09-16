using PepperDash.Essentials.Core;

namespace PepperDash.Essentials.Plugins
{
	/// <summary>
	/// DSP Basic Level Interface
	/// QSC: NamedControls
	/// Biamp: InstanceTags
	/// Polycom: 
	/// </summary>
	public interface IQscDspBasicLevel : IBasicVolumeWithFeedback
	{
		string LevelInstanceTag { get; set; }
		string MuteInstanceTag { get; set; }
		bool HasMute { get; }
		bool HasLevel { get; }
		bool AutomaticUnmuteOnVolumeUp { get; }
	}
}