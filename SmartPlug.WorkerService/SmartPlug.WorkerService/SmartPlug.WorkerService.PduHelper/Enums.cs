using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartPlug.WorkerService.PduHelper
{
   
    public enum eStatus
    {
        OK,
        Halted
    }

    public enum eSensorStateEnum
    {
        Low,
        High
    }

    public enum eCommandResult
    {
        Incomplete,
        Complete
    }

    public enum eCmd1 : byte
    {
        READ = 0x01,
        WRITE = 0x02,
        CONNECTIONCHECK = 0x03,
    }

    public enum eCmd2 : byte
    {
        NC = 0x4F, // aç
        NO = 0x43, // kapat
        NOthenNC = 0x52, // reset
        HeartBeat = 0x48,
        SOLENOIDINPUT = 0x55,
    }

    public enum eBorderByte : byte
    {
        STX1 = 0x02,
        STX2 = 0xFE,
        ETX1 = 0xFF,
        ETX2 = 0x03,
    }

    public enum eChannel : byte
    {
        Channel_1 = (byte)0x00,
        Channel_2 = (byte)0x01,
        Channel_3 = (byte)0x02,
        Channel_4 = (byte)0x03,
        Channel_5 = (byte)0x04,
        Channel_6 = (byte)0x05,
    }

    public enum eCmdContentIdx : byte
    {
        STX1,
        STX2,
        CMD1,
        CMD2,
        LEN,
        DATASTART,
    }


    internal enum eParseState
    {
        S_W_STX1,
        S_W_STX2,
        S_W_LEN,
        S_W_DATA,
        S_W_CRC,
        S_W_ETX1,
        S_W_ETX2,
        S_F_CMD,
    };

}
