using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using Newtonsoft.Json;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Devices.Common.DSP;
using PepperDash.Essentials.Core.Config;
using PepperDash.Core;
using PepperDash.Essentials.Bridges;
using PepperDash.Essentials.Core.CrestronIO;

namespace QSC.DSP.EPI
{
    public class QscDspBridgeEisc
    {
        public static void LoadPlugin()
        {
            DeviceFactory.AddFactoryForType("qscdsp", QscDspBridgeEisc.BuildDevice);
        }

        public static QscDsp BuildDevice(DeviceConfig dc)
        {
            Debug.Console(2, "QscDsp config is null: {0}", dc == null);
            //var config = JsonConvert.DeserializeObject<QscDspPropertiesConfig>(dc.Properties.ToString());
            //Debug.Console(2, "QscDsp properties config is null: {0}", config == null);
            var comm = CommFactory.CreateCommForDevice(dc);
            Debug.Console(2, "QscDsp comm is null: {0}", comm == null);
            var newMe = new QscDsp(dc.Key, dc.Name, comm, dc);         

            return newMe;
        }
    }
}