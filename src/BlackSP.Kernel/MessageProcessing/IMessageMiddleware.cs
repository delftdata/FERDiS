using BlackSP.Kernel.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Kernel.MessageProcessing
{
    public interface IMessageMiddleware
    {

        IEnumerable<IMessage> Handle(IMessage message);

    }
}
