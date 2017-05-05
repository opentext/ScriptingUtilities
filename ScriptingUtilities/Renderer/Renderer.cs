using System;
using System.Collections.Generic;
using DOKuStar.Diagnostics.Tracing;
using System.Text.RegularExpressions;
using System.IO;

namespace ScriptingUtilities
{
    public interface IRenderer
    {
        bool CanRender { get; }
        void Render(string config, string inFile, string outFile);
    }

    public class Renderer_Manager
    {
        public static IRenderer GetRenderer()
        {
#if Blazon
            return new Blazon_Renderer();
#else
            return new Dummy_Renderer();
#endif
        }
    }

    public class Blazon_Renderer : IRenderer
    {
        public bool CanRender { get { return true; } }

        private static readonly ITrace trace = TraceManager.GetTracer(typeof(Renderer_Manager));

        private enum keyword { mode, folder };

        public void Render(string exchangeFolder, string inFile, string outFile)
        {
            string inFolder = Path.Combine(exchangeFolder, "input");
            string outFolder = Path.Combine(exchangeFolder, "output");
            string errFolder = Path.Combine(exchangeFolder, @"job\errors");
            string logFolder = Path.Combine(exchangeFolder, @"job\logs");

            string tmpFile = Path.GetFileNameWithoutExtension(Path.GetTempFileName());
            string extension = Path.GetExtension(inFile);           
            extension = extension.Substring(1, extension.Length - 1);
            string BlazonInputFile = Path.Combine(inFolder, tmpFile + "." + extension);

            File.Copy(inFile, BlazonInputFile);

            string BlazonResultFile = Path.Combine(outFolder, tmpFile + "_" + extension + ".pdf");
            string BlazonErrFile = Path.Combine(errFolder, tmpFile + "." + extension + ".error");

            string errorMsg = string.Empty;
            DateTime startTime = DateTime.Now;
            while (true)
            {
                if (File.Exists(BlazonResultFile))
                {
                    File.Copy(BlazonResultFile, outFile);
                    break;
                }
                if (File.Exists(BlazonErrFile))
                {
                    errorMsg = "Blazon conversion error. Details in " + BlazonErrFile;
                    break;
                }
                System.Threading.Thread.Sleep(100);
                if ((DateTime.Now - startTime).Minutes > 10)
                {
                    errorMsg = "Blazon conversion error timeout. Input file = " + BlazonInputFile;
                    break;
                } 
            }
            File.Delete(BlazonInputFile);
            File.Delete(BlazonResultFile);
            if (errorMsg == string.Empty) File.Delete(BlazonErrFile);
            foreach(string filename in Directory.EnumerateFiles(logFolder))
            {
                string x = Path.GetFileName(filename).Substring(0, tmpFile.Length);
                if (x == tmpFile)
                    File.Delete(filename);
            }
            if (errorMsg != string.Empty) throw new Exception(errorMsg);
        }
       
        //private Dictionary<keyword, string> parseConfig(string config)
        //{
        //    Dictionary<keyword, string> result = new Dictionary<keyword, string>();

        //    foreach (string line in Regex.Split(config, @"\r?\n|\r"))
        //    {
        //        line.Trim();
        //        if (line.Length > 0 && line[0] == '#') continue;
        //        int pos = line.IndexOf('=');
        //        if (pos == -1) continue;

        //        string key = line.Substring(0, pos).Trim().ToLower();
        //        string value = line.Substring(pos + 1).Trim();
        //        result[(keyword)Enum.Parse(typeof(keyword), key)] = value;
        //    }
        //    return result;
        //}
    }

    public class Dummy_Renderer : IRenderer
    {
        public static byte[] SampleFileContent;
        public bool CanRender { get { return true; } }
        public void Render(string config, string inFile, string outFile)
        {
            File.WriteAllBytes(outFile, SampleFileContent);
        }
    }
}
