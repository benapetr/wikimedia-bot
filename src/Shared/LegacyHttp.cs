using System;
using System.IO;
using System.Net.Http;

namespace wmib
{
    public static class LegacyHttp
    {
        private static HttpClient CreateClient(bool ignoreCertificateErrors, string userAgent = null)
        {
            HttpClientHandler handler = new HttpClientHandler();
            if (ignoreCertificateErrors)
            {
                handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            }

            HttpClient client = new HttpClient(handler, true);
            if (!string.IsNullOrEmpty(userAgent))
            {
                client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);
            }
            return client;
        }

        public static string DownloadString(string url, string userAgent = null, bool ignoreCertificateErrors = false)
        {
            using (HttpClient client = CreateClient(ignoreCertificateErrors, userAgent))
            {
                return client.GetStringAsync(url).GetAwaiter().GetResult();
            }
        }

        public static void DownloadFile(string url, string path, bool ignoreCertificateErrors = false)
        {
            using (HttpClient client = CreateClient(ignoreCertificateErrors))
            {
                byte[] data = client.GetByteArrayAsync(url).GetAwaiter().GetResult();
                File.WriteAllBytes(path, data);
            }
        }
    }
}
