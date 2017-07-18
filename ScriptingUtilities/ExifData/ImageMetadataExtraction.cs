using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MetadataExtractor;

namespace ScriptingUtilities
{
    public interface IImageMetadataExtraction
    {
        Dictionary<string, string> Extract(string filename, string group = "Exif SubIFD");
    }
    public class ImageMetadataExtraction_Manager
    {
        public static IImageMetadataExtraction GetExtractor()
        {
#if Drewnoakes

            return new Drewnoakes_ImageMetadataExtractor();
#else
            return new Dummy_ImageMetadataExtractor();
#endif
        }
    }

    public class Dummy_ImageMetadataExtractor : IImageMetadataExtraction
    {
        public Dictionary<string, string> Extract(string filename, string group = "Exif SubIFD")
        {
            return new Dictionary<string, string>();
        }
    }

    public class Drewnoakes_ImageMetadataExtractor : IImageMetadataExtraction
    {
        public Dictionary<string, string> Extract(string filename, string group = "Exif SubIFD")
        {
            Dictionary<string, string> result = new Dictionary<string, string>();

            var directories = ImageMetadataReader.ReadMetadata(filename);
            foreach (Tag tag in directories.Where(n => n.Name == group).First().Tags)
                result[tag.Name] = tag.Description;

            return result;
        }
    }
}
