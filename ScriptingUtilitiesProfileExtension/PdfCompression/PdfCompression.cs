using System;
using System.IO;
#if LuraTech
using LuraTech.PdfCompressor;
#endif

namespace ScriptingUtilities
{
    public class PdfCompressor_Manager
    {
        public static IPdfCompressor GetCompressor()
        {
#if LuraTech
            return new Luratech_Compressor();
#else
            return new Dummy_Compressor();
#endif
        }
    }

    public interface IPdfCompressor
    {
        void Initialize(string configFile);
        bool CanCompress { get; }
        void Compress(string inputFilename, string outputFilename);
    }

#if LuraTech
    /// Compress PDF using Luratech compressor (https://www.luratech.com/en/)
    public class Luratech_Compressor: IPdfCompressor
    {
        private JobConfig jobConfig;

        public void Initialize(string configFile)
        {
            Lib.InitializeLibrary();
            if (!File.Exists(configFile))
                throw new Exception("Luratech  config file not found: " + configFile);

            jobConfig = new JobConfig(configFile);
        }

        public bool CanCompress { get { return true; } }

        public void Compress(string inputFilename, string outputFilename)
        {
            // Luratech will add ".pdf" extension to it's output file
            if (Path.GetExtension(outputFilename) != ".pdf")
                throw new Exception("output filename must have extension .pdf");

            // Configure
            jobConfig.InputFile = inputFilename;
            jobConfig.OutputFolder = Path.GetDirectoryName(outputFilename);
            jobConfig.OutputRenamingTemplate = Path.GetFileNameWithoutExtension(outputFilename);
            jobConfig.OutputFormat = 82; // PDF/A-2b

            // Create and execute job
            Job job = new Job(jobConfig);
            job.Run();
        }
    }
#endif

    public class Dummy_Compressor : IPdfCompressor
    {
        public void Initialize(string configFile) { }
        public bool CanCompress { get { return false; } }
        public void Compress(string inputFilename, string outputFilename) { }
    }
}
