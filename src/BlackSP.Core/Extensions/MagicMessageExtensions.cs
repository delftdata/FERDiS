using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Core.Extensions
{
    internal static class MagicMessageExtensions
    {
        /// <summary>
        /// Checks if the byte[] matches the conditions to be considered a KeepAlive message
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        internal static bool IsKeepAliveMessage(this byte[] msg)
        {
            return msg != null && msg.Length == 1 && msg[0] == (byte)255;
        }

        /// <summary>
        /// Constructs a special byte[] which is considered a KeepAlive message
        /// </summary>
        /// <returns></returns>
        internal static byte[] ConstructKeepAliveMessage()
        {
            return new byte[1] { (byte)255 };
        }

        /// <summary>
        /// Checks if the byte[] matches the conditions to be considered a KeepAlive message
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        internal static bool IsFlushMessage(this byte[] msg)
        {
            return msg != null && msg.Length == 1 && msg[0] == (byte)254;
        }

        /// <summary>
        /// Constructs a special byte[] which is considered a KeepAlive message
        /// </summary>
        /// <returns></returns>
        internal static byte[] ConstructFlushMessage()
        {
            var msg = new byte[1];
            msg[0] = 254;
            return msg;
        }

    }
}
