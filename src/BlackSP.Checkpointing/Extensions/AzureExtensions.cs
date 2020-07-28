using System.IO;

namespace BlackSP.Checkpointing.Extensions
{
    public static class AzureExtensions
    {
        public static void ThrowIfNotSuccessStatusCode(this Azure.Response response)
        {
            var statusCode = response.Status;
            if (statusCode < 200 || statusCode > 299) //okay if 2xx
            {
                throw new IOException($"Blob download failed with status code: {statusCode} - {response.ReasonPhrase}");
            }
        }

    }
}
