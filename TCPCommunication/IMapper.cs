using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCPCommunication
{
    public interface IMapper<T, h>
    {
        h Map(T input);
    }
}
