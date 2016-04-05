using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCPCommunication
{
    public class SendDataFrameMapper : IMapper<SendDataFrame, byte[]>
    {
        public byte[] Map(SendDataFrame model)
        {
            byte[] outStream = new byte[SendDataFrame.NumberOfBytes];

            outStream[0] = outStream[0].SetBit(0, model.LifeBit);

            outStream[0] = outStream[0].SetBit(1, model.InsertAck);
            outStream[0] = outStream[0].SetBit(2, model.DeleteAck);

            outStream[0] = outStream[0].SetBit(3, model.NotFound);
            outStream[0] = outStream[0].SetBit(4, model.SOAP);
            outStream[0] = outStream[0].SetBit(5, model.NotInsert);
            outStream[0] = outStream[0].SetBit(6, model.NotDelete);
            outStream[0] = outStream[0].SetBit(7, model.UnknownError);

            GetBytes(model.Prefix, 4, ref outStream, 2);
            GetBytes(model.LBHD, 12, ref outStream, 6);
            GetBytes(model.Broadcast, 60, ref outStream, 18);
            GetBytes(model.Sequence, 10, ref outStream, 78);
            GetBytes(model.PLPID, 2, ref outStream, 88);
            GetBytes(model.Operation, 2, ref outStream, 90);

            FromDataAndTime(model.BalanceDate, ref outStream, 102);

            return outStream;
        }

        private void GetBytes(string txt, int txtLength, ref byte[] stream, int startIndex)
        {
            txt = txt.PadLeft(txtLength, ' ');

            if (txtLength > txt.Length)
                txtLength = txt.Length;

            var a = ASCIIEncoding.ASCII.GetBytes(txt, 0, txtLength, stream, startIndex);
        }

        public static int FromDataAndTime(DateTime input, ref byte[] output, int startindex)
        {
            if (input.Year > 2000)
            {
                output[startindex] = Convert.ToByte((input.Year - 2000).ToString(), 16);
                output[startindex + 1] = Convert.ToByte(input.Month.ToString(), 16);
                output[startindex + 2] = Convert.ToByte(input.Day.ToString(), 16);
                output[startindex + 3] = Convert.ToByte(input.Hour.ToString(), 16);
                output[startindex + 4] = Convert.ToByte(input.Minute.ToString(), 16);
                output[startindex + 5] = Convert.ToByte(input.Second.ToString(), 16);

                string milisecond = input.Millisecond.ToString().PadLeft(3, '0');
                output[startindex + 6] = (byte)(Convert.ToByte(milisecond.Substring(0, 1), 16) + (Convert.ToByte(milisecond.Substring(1, 1), 16) << 4));
                output[startindex + 7] = (byte)(input.DayOfWeek + 1);
            }
            return 0;
        }
    }
}
