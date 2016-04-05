using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCPCommunication
{
    public class SendDataFrame
    {
        public const int NumberOfBytes = 110;

        public bool LifeBit { get; set; }
        public bool InsertAck { get; set; }
        public bool DeleteAck { get; set; }

        public bool NotFound { get; set; }
        public bool SOAP { get; set; }
        public bool NotInsert { get; set; }
        public bool NotDelete { get; set; }
        public bool UnknownError { get; set; }

        #region Buildstring
        public string Prefix { get; set; }
        public string LBHD { get; set; }
        public string Sequence { get; set; }
        public string Broadcast { get; set; }
        public string PLPID { get; set; }
        public string Operation { get; set; }
        public DateTime BalanceDate { get; set; }
        #endregion // Buildstring

        public SendDataFrame()
        {
            Prefix = "";
            LBHD = "";
            Sequence = "";
            Broadcast = "";
            PLPID = "";
            Operation = "";
            BalanceDate = DateTime.Now;
        }

        public SendDataFrame GetCopy()
        {
            SendDataFrame copy = new SendDataFrame();

            copy.LifeBit = LifeBit;
            copy.InsertAck = InsertAck;
            copy.DeleteAck = DeleteAck;
            copy.NotFound = NotFound;
            copy.SOAP = SOAP;
            copy.NotInsert = NotInsert;
            copy.NotDelete = NotDelete;
            copy.UnknownError = UnknownError;
            copy.Prefix = Prefix;
            copy.LBHD = LBHD;
            copy.Sequence = Sequence;
            copy.Broadcast = Broadcast;
            copy.PLPID = PLPID;
            copy.Operation = Operation;
            copy.BalanceDate = BalanceDate;

            return copy;
        }
    }
}
