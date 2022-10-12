using Microsoft.Win32;
using SmartPlug.WorkerService.PduHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;

namespace SmartPlug.WorkerService
{
    internal class PduService
    {

        #region Private Properties
        private SmartPduDevice _smartPduDevice { get; set; }
        #endregion Private Properties

        #region Public Methods
        //public void ClosePort()
        //{
        //    _smartPduDevice.ClosePort();
        //}

        //public bool IsConnected()
        //{
        //    return _smartPduDevice.IsConnected();
        //}

        public bool OpenPort(String Port)
        {
            try
            {
                _smartPduDevice = new SmartPduDevice();
                _smartPduDevice.Init(Port);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public string ScanForSerialComPorts()
        {
            string com = null;
            // Prizin Portunu isminden buluyor
            using (ManagementClass i_Entity = new ManagementClass("Win32_PnPEntity"))
            {
                foreach (ManagementObject i_Inst in i_Entity.GetInstances())
                {
                    Object o_Guid = i_Inst.GetPropertyValue("ClassGuid");
                    if (o_Guid == null || o_Guid.ToString().ToUpper() != "{4D36E978-E325-11CE-BFC1-08002BE10318}")
                        continue; // Skip all devices except device class "PORTS"

                    String s_Caption = i_Inst.GetPropertyValue("Caption").ToString();
                    String s_Manufact = i_Inst.GetPropertyValue("Manufacturer").ToString();
                    String s_DeviceID = i_Inst.GetPropertyValue("PnpDeviceID").ToString();
                    String s_RegPath = "HKEY_LOCAL_MACHINE\\System\\CurrentControlSet\\Enum\\" + s_DeviceID + "\\Device Parameters";
                    String s_PortName = Registry.GetValue(s_RegPath, "PortName", "").ToString();

                    int s32_Pos = s_Caption.IndexOf(" (COM");
                    if (s32_Pos > 0) // remove COM port from description
                        s_Caption = s_Caption.Substring(0, s32_Pos);

                    if (s_Caption == "USB-SERIAL CH340")  //USB-SERIAL CH340 - Silicon Labs CP210x USB to UART Bridge
                    {
                        com = s_PortName;
                    }
                    //Console.WriteLine("Port Name:    " + s_PortName);
                    //Console.WriteLine("Description:  " + s_Caption);
                    //Console.WriteLine("Manufacturer: " + s_Manufact);
                    //Console.WriteLine("Device ID:    " + s_DeviceID);
                    //Console.WriteLine("Device ID:    " + s32_Pos);
                }
            }
            //Console.WriteLine(com);
            return com;
        }

        public void SetState(eChannel Channel, bool? Value)
        {
            //PduHelper.SmartPduDevice.eCommands c = PduHelper.SmartPDU.eCommands.Null;
            switch (Value)
            {
                case null: _smartPduDevice.Execute(Channel, eCmd1.WRITE, eCmd2.NOthenNC); break;
                case true: _smartPduDevice.Execute(Channel, eCmd1.WRITE, eCmd2.NC); break;
                case false: _smartPduDevice.Execute(Channel, eCmd1.WRITE, eCmd2.NO); break;
            }
        }

        public string GetState()
        {
            // PduService.OpenPort() kullanırsan eski haline çevir 
            //var v = Convert.ToChar(_smartPduDevice.RelayStates);
            //return ascii2bin(v.ToString());
            btnCh1Stat.Text = (smartPdu._channelStates & 0x01) > 0 ? "NC" : "NO";
        }

        // 8 bit olan durum bitlerini 4 bit olarak  alır : 1011 0111 --> 0111
        private string ascii2bin(string x)
        {
            string convt;
            x = x.ToUpper();

            convt = "";

            if (x == "0")
                convt = "0000";

            if (x == "1")
                convt = "0001";

            if (x == "2")
                convt = "0010";

            if (x == "3")
                convt = "0011";

            if (x == "4")
                convt = "0100";

            if (x == "5")
                convt = "0101";

            if (x == "6")
                convt = "0110";

            if (x == "7")
                convt = "0111";

            if (x == "8")
                convt = "1000";

            if (x == "9")
                convt = "1001";

            if (x == "A")
                convt = "1010";

            if (x == "B")
                convt = "1011";

            if (x == "C")
                convt = "1100";

            if (x == "D")
                convt = "1101";

            if (x == "E")
                convt = "1110";

            if (x == "F")
                convt = "1111";

            return convt;
        }

        #endregion Public Methods
    }
}
