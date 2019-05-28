using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using Newtonsoft.Json;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Devices.Common.DSP;
using PepperDash.Essentials.Core.Config;

namespace QSC.DSP.EPI
{
    public class QscDspEpi
    {
        public static void LoadPlugin()
        {
            DeviceFactory.AddFactoryForType("qscdsp", QscDspEpi.BuildDevice);
        }

        public static QscDsp BuildDevice(DeviceConfig dc)
        {
            var config = JsonConvert.DeserializeObject<QscDspPropertiesConfig>(dc.Properties.ToString());
            var comm = CommFactory.CreateCommForDevice(dc);
            var newMe = new QscDsp(dc.Key, dc.Name, comm, config);
            return newMe;
        }
    }
}