using Newtonsoft.Json;
using PepperDash.Essentials.Core;

namespace PepperDash.Essentials.Plugins.Qsc.Qsys
{
	/// <summary>
	/// QSC Presets Configurations
	/// This class is used for Level Control Blocks as well as Camera presets
	/// </summary>
	/// <remarks>
	/// LabelFeedback is not required in the JSON configuration.  It is used to return the defined label as a feedback on the bridge.
	/// </remarks>
	/// <code>
	/// "presets": {
	///		"preset-key": {
	///			"label": "Preset X",
	///			"preset": "PRESET TAG"
	///			"bank": "PRESET BANK",
	///			"number": "PRESET NUMBER"
	///		}
	/// }
	/// </code>
	public class QsysPresets
	{
		// backer field
		private string _label;

		[JsonProperty("label")]
		public string Label
		{
			get
			{
				return this._label;
			}
			set
			{
				this._label = value;
				LabelFeedback.FireUpdate();
			}
		}

		[JsonProperty("preset")]
		public string Preset { get; set; }

		[JsonProperty("bank")]
		public string Bank { get; set; }

		[JsonProperty("number")]
		public int Number { get; set; }

		[JsonProperty("labelFeedback")]
		public StringFeedback LabelFeedback;

		/// <summary>
		/// Constructor
		/// </summary>
		public QsysPresets()
		{
			LabelFeedback = new StringFeedback("LabelFeedback", () => { return Label; });
		}
	}
}
