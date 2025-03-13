using System.Collections.Generic;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Config;

namespace QscQsysDspPlugin
{
    public class QscDspFactory : EssentialsPluginDeviceFactory<QscDsp>
    {
        public QscDspFactory()
        {
            MinimumEssentialsFrameworkVersion = "2.0.0";
            TypeNames = new List<string> { "qscDsp" };
        }

        public override EssentialsDevice BuildDevice(DeviceConfig dc)
        {
            Debug.LogMessage(Serilog.Events.LogEventLevel.Information, "Factory Attempting to create new QscDsp Device");            

            var comms = CommFactory.CreateCommForDevice(dc);
            if (comms != null) return new QscDsp(dc.Key, dc.Name, comms, dc);

            Debug.LogMessage(Serilog.Events.LogEventLevel.Error, "Factory Failed to create new QscDsp Device");
            return null;
        }
    }
}
