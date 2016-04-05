using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCPCommunication
{
    public class ReceiveDataFrame
    {
        public const int NumberOfBytes = 110;

        public bool LifeBit { get; set; }

        public bool Insert { get; set; }
        public bool Delete { get; set; }


        #region Buildstring
        public string Prefix { get; set; }
        public string LBHD { get; set; }
        public string Sequence { get; set; }
        public string Broadcast { get; set; }
        public string PLPID { get; set; }
        public string Operation { get; set; }
        public DateTime BalanceDate { get; set; }
        #endregion // Buildstring

        public ReceiveDataFrame()
        {
            Prefix = "";
            LBHD = "";
            Sequence = "";
            Broadcast = "";
            PLPID = "";
            Operation = "";
            //BalanceDate = DateTime.Now;
        }

        public ReceiveDataFrame GetCopy()
        {
            ReceiveDataFrame copy = new ReceiveDataFrame();

            copy.LifeBit = LifeBit;

            copy.Insert = Insert;
            copy.Delete = Delete;

            copy.Prefix = Prefix;
            copy.LBHD = LBHD;
            copy.Sequence = Sequence;
            copy.Broadcast = Broadcast;
            copy.PLPID = PLPID;
            copy.Operation = Operation;
            copy.BalanceDate = BalanceDate;

            return copy;
        }

        public void ChangeSendDataFrame(SendDataFrame sendDataFrame)
        {
            sendDataFrame.InsertAck = false;
            sendDataFrame.DeleteAck = false;

            sendDataFrame.Prefix = Prefix;
            sendDataFrame.LBHD = LBHD;
            sendDataFrame.Sequence = Sequence;
            sendDataFrame.Broadcast = Broadcast;
            sendDataFrame.PLPID = PLPID;
            sendDataFrame.Operation = Operation;
            sendDataFrame.BalanceDate = BalanceDate;

            sendDataFrame.NotFound = false;
            sendDataFrame.SOAP = false;
            sendDataFrame.NotInsert = false;
            sendDataFrame.NotDelete = false;
            sendDataFrame.UnknownError = false;

            ///return sendDataFrame;
        }

    }
}
