using System;
using System.Collections.Generic;
using System.Text;

namespace SmartPlug.WorkerService.Entities
{
    [Serializable]
    public class Plug
    {
      //public string DeviceID { get; set; }
        public string DeviceIP { get; set; }
        public string DeviceHostName { get; set; }
        public string DeviceSerialPort { get; set; }
        public string DeviceStatus { get; set; }
        public DateTime LastUpdateDate { get; set; }
        public bool RelayStatus1 { get; set; }
        public bool RelayStatus2 { get; set; }
        public bool RelayStatus3 { get; set; }
        public bool RelayStatus4 { get; set; }
    }
}
