using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCPCommunication
{
    public class ReceiveDataFrameMapper : IMapper<byte[], ReceiveDataFrame>
    {
        public ReceiveDataFrame Map(byte[] data)
        {
            ReceiveDataFrame model = new ReceiveDataFrame();

            model.LifeBit = data[0].GetBit(0);

            model.Insert = data[0].GetBit(1);
            model.Delete = data[0].GetBit(2);

            model.Prefix = GetString(data, 2, 4);
            model.LBHD = GetString(data, 6, 12);
            model.Broadcast = GetString(data, 18, 60);
            model.Sequence = GetString(data, 78, 10);
            model.PLPID = GetString(data, 88, 2);
            model.Operation = GetString(data, 90, 2);

            string dt;
            ToDataAndTime(data, 102, out dt);
            model.BalanceDate = DateTime.Parse(dt);

            return model;
        }

        private string GetString(byte[] input, int startindex, int num_bytes)
        {
            return ASCIIEncoding.ASCII.GetString(input, startindex, num_bytes).Replace("\0", " ");
        }



        public static int ToDataAndTime(byte[] input, int startindex, out string output)
        {
            if (input[startindex + 0] >= 0x90 ||
                input[startindex + 1] < 0x01 || input[startindex + 1] > 0x12 ||
                input[startindex + 2] < 0x01 || input[startindex + 2] > 0x31 ||
                input[startindex + 3] > 0x23 ||
                input[startindex + 4] > 0x59 ||
                input[startindex + 5] > 0x59)
            {
                output = "2000-01-01 00:00:00";
            }
            else
            {
                output = string.Format("20{0}-{1}-{2} {3}:{4}:{5}", input[startindex + 0].ToString("X").PadLeft(2, '0'), input[startindex + 1].ToString("X").PadLeft(2, '0'), input[startindex + 2].ToString("X").PadLeft(2, '0'), input[startindex + 3].ToString("X").PadLeft(2, '0'), input[startindex + 4].ToString("X").PadLeft(2, '0'), input[startindex + 5].ToString("X").PadLeft(2, '0'));
            }

            return 0;
        }
    }
}
