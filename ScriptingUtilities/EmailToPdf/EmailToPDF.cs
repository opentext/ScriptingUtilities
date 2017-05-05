using System;
using System.IO;
using Microsoft.Win32;
using DOKuStar.Diagnostics.Tracing;

namespace ScriptingUtilities
{
    public interface IEmailToPdf
    {
        bool CanConvert { get; }
        void Convert(string htmlContent, string filename);
    }

    public class EmailToPDF_Manager
    {
        public static IEmailToPdf GetEmailToPDF()
        {
#if PdfCrowd
            return new PdfCrowd_EmailToPdf();
#else
            return new Dummy_Email2Pdf();
#endif
        }
    }

#if PdfCrowd
    public class PdfCrowd_EmailToPdf : IEmailToPdf
    { 
        public bool CanConvert {  get { return true; } }

        private static readonly ITrace trace = TraceManager.GetTracer(typeof(EmailToPDF_Manager));

        private static pdfcrowd.Client client = null;
        public void Convert(string htmlContent, string filename)
        {
            // Create a client instance if needed. Take credentials from Registry
            if (client == null)
            {
                RegistryKey key = Registry.LocalMachine.OpenSubKey(@"Software\Wow6432Node\Opentext\Capture Center\ScriptingUtilities\PdfCrowd");
                string username = (string)key.GetValue("user");
                string api_key = (string)key.GetValue("api_key");
                client = new pdfcrowd.Client(username, api_key);
            }

            // Convert HTML string and store result in file
            MemoryStream memStream = new MemoryStream();
            client.convertHtml(htmlContent, memStream);
            using (FileStream file = new FileStream(filename, FileMode.Create, System.IO.FileAccess.Write))
            {
                byte[] bytes = new byte[memStream.Length];
                memStream.Read(bytes, 0, (int)memStream.Length);
                file.Write(bytes, 0, bytes.Length);
                memStream.Close();
            }

            // Log remaining tokens
            int ntokens = client.numTokens();
            trace.WriteInfo("PdfCrowd remaining ´tokens: " + ntokens);
        }
    }
#else
    public class Dummy_Email2Pdf : IEmailToPdf
    {
        public bool CanConvert { get { return false; } }
        public void Convert(string htmlContent, string filename) { }
    }
#endif
}
