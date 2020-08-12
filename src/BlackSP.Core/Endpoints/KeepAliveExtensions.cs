using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Core.Endpoints
{
    internal static class KeepAliveExtensions
    {

        internal static bool IsKeepAliveMessage(this byte[] msg)
        {
            return msg != null && msg.Length == 1 && msg[0] == (byte)255;
        }

        internal static byte[] ConstructKeepAliveMessage()
        {
            return new byte[1] { (byte)255 };
        }

    }
}
