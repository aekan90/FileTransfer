using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace SmartPlug.WorkerService.PduHelper
{
    public class CommandResultEventArgs : EventArgs
    {
        public eCmd1 cmd1;
        public eCmd2 cmd2;
        public eChannel channel;
        public eCommandResult cmdResult;
    }

    public class StateChangeEventArgs : EventArgs
    {
        public eStatus Status { get; set; }
    }

    public delegate void dlgOnSensorStateChanged(eSensorStateEnum SensorState);

    public delegate void dlgOnDeviceLog(string Log);
}
