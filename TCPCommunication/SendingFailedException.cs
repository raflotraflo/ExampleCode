using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace TCPCommunication
{
    [Serializable]
    public class SendingFailedException : Exception
    {
        public SendingFailedException()
        { }

        public SendingFailedException(string message)
        : base(message)
        { }

        public SendingFailedException(string message, Exception innerException)
        : base(message, innerException)
        { }

        protected SendingFailedException(SerializationInfo info, StreamingContext context)
        : base(info, context)
        { }
    }
}
