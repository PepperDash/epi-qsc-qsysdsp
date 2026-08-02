using System.Collections.Generic;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Config;

namespace PepperDash.Essentials.Plugins.Qsc.Qsys.RemoteControlProtocol
{
    public class QsysQrcFactory : EssentialsPluginDeviceFactory<QsysQrcController>
    {
        public QsysQrcFactory()
        {
            MinimumEssentialsFrameworkVersion = "2.0.0";
            TypeNames = new List<string> { "qscDspQrc" };
        }

        public override EssentialsDevice BuildDevice(DeviceConfig dc)
        {
            Debug.LogMessage(Serilog.Events.LogEventLevel.Information, "Factory Attempting to create new QsysQrcController Device");

            var comms = CommFactory.CreateCommForDevice(dc);
            if (comms != null) return new QsysQrcController(dc.Key, dc.Name, comms, dc);

            Debug.LogMessage(Serilog.Events.LogEventLevel.Error, "Factory Failed to create new QsysQrcController Device");
            return null;
        }
    }
}
