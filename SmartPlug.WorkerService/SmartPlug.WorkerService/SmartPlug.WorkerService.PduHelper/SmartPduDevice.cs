using SmartPlug.WorkerService.PduHelper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Channels;

namespace SmartPlug.WorkerService.PduHelper
{
    public class SmartPduDevice
    {
        public event EventHandler OnStateChanged;

        public event EventHandler OnCommandResult;

        public event dlgOnSensorStateChanged OnSensorStateChanged;

        public event dlgOnDeviceLog OnDeviceLog;

        private System.IO.Ports.SerialPort _serial = new System.IO.Ports.SerialPort();

        private System.Timers.Timer _tmrCheckState = new System.Timers.Timer();
        private System.Timers.Timer _tmrParseTimeout = new System.Timers.Timer();
        private System.Timers.Timer _tmrCmdTimeout = new System.Timers.Timer();

        private List<byte> _lastCmd = new List<byte> { };
        private DateTime _lastCmdSentTime = DateTime.MinValue;
        private DateTime _lastHeartBitRecieveTime = DateTime.MinValue;

        private eCommandResult _lastCmdResult;
        private eStatus _status;

        private string _comAddress { get; set; }
        public byte _channelStates = 255; // bit = 0 --> channel open, bit = 1 ---> channel closed

        public eStatus Status { get => _status; set => _status = value; }
        public eCommandResult CommandResult { get => _lastCmdResult; set => _lastCmdResult = value; }
        public byte RelayStates { get => _channelStates; set => _channelStates = value; }

        public SmartPduDevice()
        {
            _tmrCheckState.Interval = 1000; // bunu 1100 yapsak mı?
            _tmrCheckState.Elapsed += _tmrCheckState_Elapsed;

            _tmrParseTimeout.Interval = 150;
            _tmrParseTimeout.Elapsed += _tmrParseTimeout_Elapsed;

            _tmrCmdTimeout.Interval = 150;
            _tmrCmdTimeout.Elapsed += _tmrCommandResult_Elapsed;

            _status = eStatus.Halted;

            ResetBuffer(_buffer);

            Log("SmartPdu Instance Created");
        }

        public bool Init(string ComPort)
        {
            Log("Init Started");
            _comAddress = ComPort;

            _serial.PortName = _comAddress;
            _serial.DataReceived += _serial_DataReceived;
            _serial.BaudRate = 115200;
            _serial.Parity = System.IO.Ports.Parity.None;
            _serial.StopBits = System.IO.Ports.StopBits.One;
            _serial.DiscardNull = false;

            if (!System.IO.Ports.SerialPort.GetPortNames().ToList().Contains(_comAddress))
            {
                Log("Error :Given Port Not Found");
                return false;
            }

            try
            {
                _tmrCheckState.Start();
                Log("Opening serial conection on port :" + ComPort);
                _serial.Open(); // baska yere bagli ise crash!!!
            }
            catch (Exception exp)
            {
                //Console.WriteLine(exp.Message);
                Log("Error : " + exp.Message);
            }

            if (_serial.IsOpen)
            {
                Log("Connection Succeed");
                return true;
            }
            else
            {
                Log("Connection failed");
                return false;
            }
        }

        private void _tmrCommandResult_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            _tmrCmdTimeout.Enabled = false;

            TimeSpan timeout = TimeSpan.FromMilliseconds(150);
            DateTime t = DateTime.Now;
            CommandResultEventArgs eArgs = new CommandResultEventArgs();
            eArgs.cmd1 = (eCmd1)_lastCmd[(int)eCmdContentIdx.CMD1];
            eArgs.cmd2 = (eCmd2)_lastCmd[(int)eCmdContentIdx.CMD2];
            eArgs.channel = (eChannel)_lastCmd[(int)eCmdContentIdx.DATASTART];
            if (_lastCmdResult != eCommandResult.Complete && t.Subtract(_lastCmdSentTime) > timeout)
            {
                eArgs.cmdResult = eCommandResult.Incomplete;
                if (OnCommandResult != null)
                    OnCommandResult(this, eArgs);
            }
            else
            {
                eArgs.cmdResult = eCommandResult.Complete;
                if (OnCommandResult != null)
                    OnCommandResult(this, eArgs);
            }
        }

        private void _tmrParseTimeout_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            _tmrParseTimeout.Enabled = false;
            if ((_parseState != eParseState.S_W_STX1) && (_parseState < eParseState.S_F_CMD))
            {
                _parseState = eParseState.S_W_STX1;
            }
        }

        private void ResetBuffer(byte[] buffer)
        {
            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[i] = 0;
            }
            _idxCursor = 0;
            _idxSearchStart = 0;
            _parseState = eParseState.S_W_STX1;
        }

        private byte[] _buffer = new byte[1024];
        private int _idxCursor = 0;
        private int _idxSearchStart = 0;

        private void ReestablishSerialConnection()
        {
            if
            (
            !_serial.IsOpen &&
            System.IO.Ports.SerialPort.GetPortNames().ToList().Contains(_serial.PortName)
            )
            {
                _serial.Open();
            }
        }

        public void Execute(eChannel channel, eCmd1 cmd1, eCmd2 cmd2)
        {
            byte len = 0x01; // data: 1 byte röle indexi
            byte[] bytes = new byte[9]
            {
                (byte)eBorderByte.STX1,
                (byte)eBorderByte.STX2,
                (byte)cmd1,
                (byte)cmd2,
                len,
                (byte)channel,
                0,
                (byte)eBorderByte.ETX1,
                (byte)eBorderByte.ETX2,
            };

            byte crc = (byte)((byte)cmd1 ^ (byte)cmd2 ^ len ^ (byte)channel);
            bytes[bytes.Length - 3] = crc;

            try
            {
                _serial.Write(bytes, 0, bytes.Length);

                _lastCmdSentTime = DateTime.Now;
                _lastCmdResult = eCommandResult.Incomplete;
                _lastCmd.Clear();
                _lastCmd.AddRange(bytes.ToArray());
                _tmrCmdTimeout.Enabled = true;
            }
            catch (Exception exc)
            {
                Log(exc.Message);
            }
        }

        public void Execute(eChannel channel, eCmd1 cmd1, eCmd2 cmd2, List<byte> param)
        {
            byte len = (byte)(param.Count + 0x01);
            List<byte> bytes = new List<byte>()
            {
                (byte)eBorderByte.STX1,
                (byte)eBorderByte.STX2,
                (byte)cmd1,
                (byte)cmd2,
                (byte)len,
                (byte)channel,
                0,
                (byte)eBorderByte.ETX1,
                (byte)eBorderByte.ETX2,
            };
            bytes.InsertRange(bytes.Count - 3, param);

            byte crc = 0;
            for (int i = 2; i < bytes.Count - 3; i++)
            {
                crc ^= bytes[i];
            }
            bytes[bytes.Count - 3] = crc;

            try
            {
                _serial.Write(bytes.ToArray(), 0, bytes.Count);
                _lastCmdSentTime = DateTime.Now;
                _lastCmdResult = eCommandResult.Incomplete;
                _lastCmd.Clear();
                _lastCmd.AddRange(bytes.ToArray());
                _tmrCmdTimeout.Enabled = true;
            }
            catch (Exception exc)
            {
                Log(exc.Message);
            }
        }

        private const int _cmdMaxlen = 11;

        private void _tmrCheckState_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            _tmrCheckState.Enabled = false;
            StateChangeEventArgs eArgs = new StateChangeEventArgs();
            TimeSpan timeoutLimit = TimeSpan.FromMilliseconds(1100);
            DateTime t = DateTime.Now;
            if (t.Subtract(_lastHeartBitRecieveTime) > timeoutLimit)
            {
                if (eStatus.Halted != _status)
                {
                    _status = eStatus.Halted;

                    if (OnStateChanged != null)
                    {
                        eArgs.Status = _status;
                        OnStateChanged(this, eArgs);
                        Log("Connection State Changed :" + _status);
                    }
                }
                ReestablishSerialConnection();
            }
            else
            {
                if (eStatus.OK != _status)
                {
                    _status = eStatus.OK;
                    if (OnStateChanged != null)
                    {
                        eArgs.Status = _status;
                        OnStateChanged(this, eArgs);
                        Log("Connection State Changed :" + _status);
                    }
                }
            }
            _tmrCheckState.Enabled = true;
        }

        private eParseState _parseState = eParseState.S_W_STX1;

        private int _idxSTX1 = 0;
        private int _idxSTX2 = 0;
        private int _idxLEN = 0;
        private int _idxDATAENDPOINT = 0;
        private int _idxCRC = 0;
        private int _idxETX1 = 0;
        private int _idxETX2 = 0;

        private void ParseFunc()
        {
            switch (_parseState)
            {
                case eParseState.S_W_STX1:
                    _idxSTX1 = Array.IndexOf(_buffer, (byte)eBorderByte.STX1, _idxSearchStart);
                    if (_idxSTX1 != -1)
                    {
                        _tmrParseTimeout.Enabled = true;
                        _parseState = eParseState.S_W_STX2;
                        _idxSearchStart = _idxSTX1 + 1;
                    }
                    else
                    {
                        _idxSearchStart = _idxCursor;
                        return;
                    }
                    goto case eParseState.S_W_STX2;
                case eParseState.S_W_STX2:
                    if (!(_idxSTX1 + 1 < _idxCursor))
                    {
                        return;
                    }
                    else if (_buffer[_idxSTX1 + 1] != (byte)eBorderByte.STX2)
                    {
                        _idxSearchStart = _idxSTX1 + 1;
                        _parseState = eParseState.S_W_STX1;
                        return;
                    }
                    else
                    {
                        _idxSTX2 = _idxSTX1 + 1;
                        _idxSearchStart = _idxSTX2 + 1;
                        _parseState = eParseState.S_W_LEN;
                    }
                    goto case eParseState.S_W_LEN;
                case eParseState.S_W_LEN:
                    if (!(_idxSTX2 + 3 < _idxCursor)) //cmd: STX1 STX2 CMD1 CMD2 L => L = STX2 + 3
                    {
                        return;
                    }
                    else
                    {
                        _idxLEN = _idxSTX2 + 3;
                        _idxSearchStart = _idxLEN + 1;
                        _parseState = eParseState.S_W_DATA;
                    }
                    goto case eParseState.S_W_DATA;
                case eParseState.S_W_DATA:
                    if (!(_idxLEN + _buffer[_idxLEN] < _idxCursor)) // cmd: ... L D1 D2 // diyelim ki L 2 olsun data tamamlandi ise pL + 2 cursordan kucuk olmali
                    {
                        return;
                    }
                    else
                    {
                        _idxDATAENDPOINT = _idxLEN + _buffer[_idxLEN];
                        _idxSearchStart = _idxDATAENDPOINT + 1;
                        _parseState = eParseState.S_W_CRC;
                    }
                    goto case eParseState.S_W_CRC;
                case eParseState.S_W_CRC:
                    if (!(_idxDATAENDPOINT + 1 < _idxCursor))
                    {
                        return;
                    }
                    else
                    {
                        _idxCRC = _idxDATAENDPOINT + 1;
                        _idxSearchStart = _idxCRC + 1;
                        _parseState = eParseState.S_W_ETX1;
                    }
                    goto case eParseState.S_W_ETX1;
                case eParseState.S_W_ETX1:
                    if (!(_idxCRC + 1 < _idxCursor))
                    {
                        return;
                    }
                    else if (_buffer[_idxCRC + 1] != (byte)eBorderByte.ETX1)
                    {
                        _idxSearchStart = _idxCRC + 1;
                        _parseState = eParseState.S_W_STX1;
                        return;
                    }
                    else
                    {
                        _idxETX1 = _idxCRC + 1;
                        _idxSearchStart = _idxETX1 + 1;
                        _parseState = eParseState.S_W_ETX2;
                    }
                    goto case eParseState.S_W_ETX2;

                case eParseState.S_W_ETX2:
                    if (!(_idxETX1 + 1 < _idxCursor))
                    {
                        return;
                    }
                    else if (_buffer[_idxETX1 + 1] != (byte)eBorderByte.ETX2)
                    {
                        _idxSearchStart = _idxETX1 + 1;
                        _parseState = eParseState.S_W_STX1;
                        return;
                    }
                    else
                    {
                        _idxETX2 = _idxETX1 + 1;
                        _idxSearchStart = _idxETX2 + 1;
                        _parseState = eParseState.S_F_CMD;
                    }

                    goto case eParseState.S_F_CMD;

                case eParseState.S_F_CMD:
                    byte crc = 0;
                    for (int i = _idxSTX2 + 1; i < _idxCRC; ++i)
                    {
                        crc ^= _buffer[i];
                    }
                    if (crc == _buffer[_idxCRC])
                    {
                        //Console.WriteLine("Found cmd\n");
                        ExamineCmd(_buffer, _idxSTX1, _idxETX2);
                    }
                    _parseState = eParseState.S_W_STX1;
                    return;
            }
        }

        private int cnt = 0;

        private void ExamineCmd(byte[] cmdContainer, int idxStart, int idxEnd)
        {
            if ((cmdContainer[idxStart + (int)eCmdContentIdx.CMD1] == (byte)eCmd1.CONNECTIONCHECK)
                && (cmdContainer[idxStart + (int)eCmdContentIdx.CMD2] == (byte)eCmd2.HeartBeat))
            {
                _channelStates = _buffer[_idxSTX1 + (int)eCmdContentIdx.DATASTART];
                _lastHeartBitRecieveTime = DateTime.Now;
            }
            else if ((cmdContainer[idxStart + (int)eCmdContentIdx.CMD1] == (byte)eCmd1.READ)
                    && (cmdContainer[idxStart + (int)eCmdContentIdx.CMD2] == (byte)eCmd2.SOLENOIDINPUT))
            {
                ++cnt;
                //Log(cnt);

                var SensorState = (eSensorStateEnum)cmdContainer[idxStart + (int)eCmdContentIdx.DATASTART];

                OnSensorStateChanged?.Invoke(SensorState);

                //Console.WriteLine("solenoid input 1\n");
            }
            else if (cmdContainer[idxStart + (int)eCmdContentIdx.CMD1] == (byte)eCmd1.WRITE)
            {
                _lastCmdResult = (cmdContainer.ToList().GetRange(idxStart, idxEnd - idxStart + 1).SequenceEqual(_lastCmd) ? eCommandResult.Complete : eCommandResult.Incomplete);
            }
        }

        private void _serial_DataReceived(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
        {
            //Burada overflow olmamak icin nasil bir yontem izlemeliyiz???
            int bytesToRead = _serial.BytesToRead;

            if (_buffer.Length - _idxCursor < _cmdMaxlen)
            {
                ResetBuffer(_buffer);
            }
            try
            {
                int n = _serial.Read(_buffer, _idxCursor, _serial.BytesToRead);
                _idxCursor += n;

                ParseFunc();
            }
            catch (Exception exc)
            {
                if (exc is ArgumentOutOfRangeException)
                {
                    ResetBuffer(_buffer);
                }
                else if (exc is System.ArgumentException)
                {
                    Log(exc.ToString());

                    byte[] msg = new byte[_serial.BytesToRead];
                    _serial.Read(msg, 0, msg.Length);
                    Log(Encoding.Default.GetString(msg));
                    _serial.DiscardInBuffer();
                }
            }
            //finally
            //{
            //}
        }

        private void Log(string Message)
        {
            Console.WriteLine(Message);
            OnDeviceLog?.Invoke(Message);
        }
    }
}