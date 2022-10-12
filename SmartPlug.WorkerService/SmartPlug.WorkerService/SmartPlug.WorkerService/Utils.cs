using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SmartPlug.WorkerService
{
    internal static class Utils
    {
        public static string GetLocalIPAddress()
        {
            string strIp = "";
            try
            {
                using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
                {
                    socket.Connect("8.8.8.8", 65530); //yerel ip almak için bir udp bağlantı paketi oluşturmak için...
                    IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
                    strIp = endPoint.Address.ToString();
                }
            }
            catch { }

            return strIp;
        }



        public static string GetMBSerialNumber()
        {
            string motherBoard = "";
            try
            {
                System.Management.ManagementObjectSearcher mos = new System.Management.ManagementObjectSearcher("SELECT * FROM Win32_BaseBoard");
                System.Management.ManagementObjectCollection moc = mos.Get();

                foreach (System.Management.ManagementObject mo in moc)
                {
                    motherBoard = (string)mo["SerialNumber"];
                }
            }
            catch
            {

            }
            return motherBoard;
        }

    }
}
