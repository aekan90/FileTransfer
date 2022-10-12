using SmartPlug.WorkerService.Entities;
using SmartPlug.WorkerService.PduHelper;
using System;
using System.CodeDom;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Reflection.Metadata;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using System.Text;
using System.Xml.Linq;
using System.IO;
using Newtonsoft.Json;
using Nancy.Json;
using System.Security.Principal;
using Nancy;

namespace SmartPlug.WorkerService
{
    public class Worker : BackgroundService
    {
        private string logsPath = AppDomain.CurrentDomain.BaseDirectory + "Logs";
        public ResponseMessage<Plug> _responseMessage { get; set; }
        private PduService _pduService { get; set; }
        private string _serialComPort;
        private Plug _plug;
        private Logger _logger { get; set; }


        public Worker()
        {
            _logger = new Logger(true, logsPath); // transient yap
            _pduService = new PduService();
            Log("Scanning SmartPDU");
            try
            {
                _serialComPort = _pduService.ScanForSerialComPorts();
            }
            catch (Exception e)
            {
                Log("Hata : Worker:43" + e.Message);
                throw;
            }

            Log("Device Found on " + _serialComPort);
            _responseMessage = new ResponseMessage<Plug>();
            _plug = new Plug();
        }

        //private void PduHelper_StateChange(object sender, EventArgs e)
        //{
        //    SmartPDU.eStatus status = ((SmartPDU.OnStateChangedEventArgs)e).Status;
        //}

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            Log("StartAsync Çalýþtý");
            await base.StartAsync(cancellationToken);
        }
        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            Log("StopAsync Çalýþtý");
            await base.StopAsync(cancellationToken);
        }
        public override void Dispose()
        {
            Log("Dispose Çalýþtý");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested) // Worker Servis Çalýþýyorsa Sonsuz Döngüdedir.
            {
                TcpListener server = null;
                try
                {
                    Log("Service Started");
                    //   Log("---------------------Configuration Complete-------------------------");
                    //   Log("--------------------------------------------------------------------");
                    string IPv4 = Utils.GetLocalIPAddress();
                    _plug.DeviceIP = IPv4;
                    Log("Local IPv4 : " + IPv4);
                    string hostName = Dns.GetHostName();
                    _plug.DeviceHostName = hostName;
                    Log("Kiosk Hostname : " + hostName);
                    Log("Kiosk Serial Port : " + _serialComPort);

                    server = new TcpListener(IPAddress.Parse(IPv4), 2301);
                    server.Start(); // Client isteklerini dinlemeye baþla.
                    Log("server.Started");
                    // Buffer for reading data
                    Byte[] bytes = new Byte[256];
                    String data = null;

                    // Enter the listening loop.
                    while (true)
                    {
                        _pduService.OpenPort(_serialComPort); // prizin COM port baðlantýsýný baþlattýk
                        Log("Service Started : Opened serial com port " + _serialComPort);
                        _plug.DeviceSerialPort = _serialComPort;
                        Log("Waiting for a connection");
                        TcpClient client = await server.AcceptTcpClientAsync();
                        Log("Status : " + _pduService.GetState());
                        // Okuma ve yazma için bir stream object alýn
                        try
                        {
                            NetworkStream stream = client.GetStream();
                            int i;
                            // Ýstemci-->Api--> tarafýndan gönderilen tüm verileri almak için sonsuz döngü.
                            while ((i = await stream.ReadAsync(bytes, 0, bytes.Length)) != 0)
                            {
                                data = System.Text.Encoding.ASCII.GetString(bytes, 0, i);
                                Log("Request" + data);
                                // data = (GuidId,1,Open)
                                string[] subs = data.Split(',');
                                string requestId = subs[0];
                                string portNo = subs[1];
                                string OCR = subs[2];

                                PlugControl(data, OCR); // port açýp kapatan metot
                                Log(portNo + ". Porta " + OCR + " isteði Geldi");
                                if (OCR == "Reset")
                                    Thread.Sleep(4000); // reset
                                Thread.Sleep(1000); // open or close

                                string LastStatus = _pduService.GetState();
                                Log("Son Durum : " + LastStatus);

                                _plug.DeviceStatus = LastStatus;
                                _plug.LastUpdateDate = DateTime.Now;

                                var LastStatusCharArray = LastStatus.ToCharArray();
                                // charArray to Bool
                                _plug.RelayStatus1 = Convert.ToBoolean(Convert.ToInt32(LastStatusCharArray[0].ToString())); // 0/1   
                                _plug.RelayStatus2 = Convert.ToBoolean(Convert.ToInt32(LastStatusCharArray[1].ToString())); // 0/1  
                                _plug.RelayStatus3 = Convert.ToBoolean(Convert.ToInt32(LastStatusCharArray[2].ToString())); // 0/1   
                                _plug.RelayStatus4 = Convert.ToBoolean(Convert.ToInt32(LastStatusCharArray[3].ToString())); // 0/1   

                                _responseMessage.Data = _plug;
                                _responseMessage.IsSuccess = true;
                                _responseMessage.Message = "true";
                                _responseMessage.DeveloperMessage = OCR;
                                _responseMessage.StatusCode = 200;
                                _responseMessage.Exception = null;

                                string jsonString = JsonConvert.SerializeObject(_responseMessage, Formatting.Indented);
                                byte[] msg2 = System.Text.Encoding.ASCII.GetBytes(jsonString);
                                stream.WriteAsync(msg2, 0, msg2.Length);

                                stream.Close();
                                client.Close(); // end connection
                                _pduService.ClosePort();
                                Log("---------------------Success1-------------------------");

                            }
                        }
                        catch (Exception ex)
                        {
                            Log("--------------------Exeption1--------------------------");
                            Log(ex.Message);
                        }
                    }
                }
                catch (SocketException ex)
                {
                    Log("--------------------Exeption2--------------------------");
                    Log(ex.Message);
                }
                finally
                {
                    server.Stop();
                    Log("---------------------Success1-------------------------");
                }
            }
        }

        private async Task PlugControl(String data, String OCR)
        {
            //data = null;
            string RequestId = null;

            var Open = eCmd2.NC;
            var Close = eCmd2.NO;
            var Reset = eCmd2.NOthenNC;

            var priz1 = eChannel.Channel_1;
            var priz2 = eChannel.Channel_2;
            var priz3 = eChannel.Channel_3;
            var priz4 = eChannel.Channel_4;

            // ör : RequestId,1,Open
            RequestId = data.Split(',')[0]; // nfklf3-3f3f3fko43p-2dwef4-h5tgr   
            string prizNo = data.Split(',')[1]; // 1   
            OCR = data.Split(',')[2];  // Open

            switch (prizNo)
            {
                case "1":
                    if (OCR == "Open")
                        _pduService.SetState(priz1, true);
                    else if (OCR == "Close")
                        _pduService.SetState(priz1, false);
                    else if (OCR == "Reset")
                        _pduService.SetState(priz1, null);
                    break;
                case "2":
                    if (OCR == "Open")
                        _pduService.SetState(priz2, true);
                    else if (OCR == "Close")
                        _pduService.SetState(priz2, false);
                    else if (OCR == "Reset")
                        _pduService.SetState(priz2, null);
                    break;
                case "3":
                    if (OCR == "Open")
                        _pduService.SetState(priz3, true);
                    else if (OCR == "Close")
                        _pduService.SetState(priz3, false);
                    else if (OCR == "Reset")
                        _pduService.SetState(priz3, null);
                    break;
                case "4":
                    if (OCR == "Open")
                        _pduService.SetState(priz4, true);
                    else if (OCR == "Close")
                        _pduService.SetState(priz4, false);
                    else if (OCR == "Reset")
                        _pduService.SetState(priz4, null);
                    break;
            }
        }

        public void Log(string Message)
        {
            _logger.Add(Message);
        }

    }
}