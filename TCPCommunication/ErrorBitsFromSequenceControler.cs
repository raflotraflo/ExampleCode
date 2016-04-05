using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCPCommunication
{
    public class ErrorBitsFromSequenceControler
    {
        public bool LBHDNotFound { get; set; }
        public bool ConnectionAlarm { get; set; }
        public bool UnknownError { get; set; }
        public bool OrderWithSOP { get; set; }
        public bool OrderNotInserted { get; set; }
        public bool OrderNotDeleted { get; set; }
    }
}
