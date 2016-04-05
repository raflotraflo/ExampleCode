using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace TCPCommunication
{
    public interface ISequenceControllerForVisuControl
    {
        event EventHandler SendDataChanged;
        event EventHandler ReceiveDataChanged;
        event EventHandler ErrorChanged;
        event EventHandler EnableChanged;
        SendDataFrame SendDataBits { get; }
        ReceiveDataFrame ReceiveDataBits { get; }
        ErrorBitsFromSequenceControler ErrorBits { get; }

        Task SaveAsync(IPAddress ip, int port);
        Task EnableAsync();
        Task DisableAsync();

        IPAddress IP
        {
            get;
        }
        int Port
        {
            get;
        }
        bool Enabled
        {
            get;
        }

    }
}
