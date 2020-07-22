using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace BlackSP.Checkpointing.Extensions
{
    public static class AzureExtensions
    {
        public static void ThrowIfNotSuccessStatusCode(this Azure.Response response)
        {
            var statusCode = (HttpStatusCode)response.Status;
            if (statusCode != HttpStatusCode.OK)
            {
                throw new IOException($"Blob download failed with status code: {statusCode} - {response.ReasonPhrase}");
            }
        }

    }
}
