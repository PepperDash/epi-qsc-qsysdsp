using System.Collections.Generic;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Config;

namespace PepperDash.Essentials.Plugins.Qsc.Qsys.ExternalControlProtocol
{
    public class QsysEcpFactory : EssentialsPluginDeviceFactory<QsysEcpController>
    {
        public QsysEcpFactory()
        {
            MinimumEssentialsFrameworkVersion = "2.0.0";
            TypeNames = new List<string> { "qscDsp" };
        }

        public override EssentialsDevice BuildDevice(DeviceConfig dc)
        {
            Debug.LogMessage(Serilog.Events.LogEventLevel.Information, "Factory Attempting to create new QsysEcpController Device");            

            var comms = CommFactory.CreateCommForDevice(dc);
            if (comms != null) return new QsysEcpController(dc.Key, dc.Name, comms, dc);

            Debug.LogMessage(Serilog.Events.LogEventLevel.Error, "Factory Failed to create new QsysEcpController Device");
            return null;
        }
    }
}
