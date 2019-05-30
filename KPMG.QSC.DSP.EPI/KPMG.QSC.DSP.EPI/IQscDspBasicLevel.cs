using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

namespace KPMG.QSC.DSP.EPI
{
    public interface IQscDspBasicLevel : IBasicVolumeWithFeedback
    {
        /// <summary>
        /// In BiAmp: Instance Tag, QSC: Named Control, Polycom: 
        /// </summary>
        string LevelInstanceTag { get; set; }
        string MuteInstanceTag { get; set; }
        bool HasMute { get; }
        bool HasLevel { get; }
        bool AutomaticUnmuteOnVolumeUp { get; }
    }
}