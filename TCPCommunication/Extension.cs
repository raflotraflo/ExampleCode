using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCPCommunication
{
    static class Extension
    {
        public static bool GetBit(this byte value, int index)
        {
            return (value & (0x01 << index)) > 0;
        }

        public static byte SetBit(this byte value, int index, bool newValue)
        {
            if (newValue)
            {
                value |= (byte)(0x01 << index);
            }
            else
            {
                value &= (byte)~(0x01 << index);
            }

            return value;
        }

    }
}
