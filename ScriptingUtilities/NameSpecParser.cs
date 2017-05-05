using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScriptingUtilities
{
    /// This class handles the file name specification string. It parses the input string
    /// and handles the substitution.
    public class NameSpecParser
    {
        /// A Substitute holds a parsed part of the specification string.
        /// Parsing creates a list of these objects.
        public class SubstituteItem
        {
            public SubstitutionType SubstitutionType { get; set; }
            public String Parameter { get; set; }
            public string FinalValue { get; set; }
        }
        public enum SubstitutionType { BatchId, DocumentNumber, PageNumber, Guid, Host, Date, Time, Field, UniqueId, Const};

        // The below values are used to do the substitution
        public string BatchId { get; set; }
        public string DocumentNumber { get; set; }
        public string PageNumber { get; set; }
        public List<KeyValuePair<string,string>> ValueList { get; set; }

        // Constructors
        public NameSpecParser() // Set some defaults
        {
            BatchId = "4711";
            DocumentNumber = "0001";
            PageNumber = "0001";
            ValueList = new List<KeyValuePair<string, string>>();
        }

        public NameSpecParser(string batchId, string documentNummber, List<KeyValuePair<string, string>> vl)
        {
            BatchId = batchId;
            DocumentNumber = documentNummber;
            ValueList = vl;
        }

        // Do a complete conversion
        public string Convert(string spec)
        {
            if (spec == null) return string.Empty;
            List<SubstituteItem> ls = Parse(spec);
            SetValues(ls);
            return ComposeResultString(ls);
        }

        /// The parsing function is hardcore. Character by character is handled.
        public List<SubstituteItem> Parse(string spec)
        {
            List<SubstituteItem> result = new List<SubstituteItem>();

            bool readingSpec = false;
            string curr = string.Empty;

            for (int i = 0; i != spec.Length; i++)
            {
                char c = spec[i];

                if (readingSpec)    // We have seen an unmasked '<', so we are collection substitution
                {
                    if (c != '>') { curr += c; continue; }  // continue assembling spec
                    result.Add(parseSpec(curr));
                    curr = string.Empty;
                    readingSpec = false;
                    continue;
                }
                if ((c == '\\' || c == '<') && i == spec.Length - 1)
                {
                    curr += c;
                    result.Add(new SubstituteItem() { SubstitutionType = SubstitutionType.Const, Parameter = curr });
                    curr = string.Empty;
                    break;
                }
                if (c == '\\' && spec[i + 1] == '\\') { curr += '\\'; i++; continue; }
                if (c == '\\' && spec[i + 1] == '<') { curr += '<'; i++; continue; }
                if (c == '<') {
                    result.Add(new SubstituteItem() { SubstitutionType = SubstitutionType.Const, Parameter = curr });
                    curr = string.Empty;
                    readingSpec = true;
                    continue;
                }
                curr += spec[i];
            }
            if (readingSpec) curr = "<" + curr;
            if (curr.Length != 0)
                result.Add(new SubstituteItem() { SubstitutionType = SubstitutionType.Const, Parameter = curr });

            return result;
        }

        /// Spec is one of the keywords, e.g. "BatchId", or ":Fieldname".
        /// However there is the possiblity that is is just some string.
        private SubstituteItem parseSpec(string spec)
        {
            // First try to convert known substitution strings from the enum
            try
            {
                SubstitutionType s = (SubstitutionType)Enum.Parse(typeof(SubstitutionType), spec, true);
                return new SubstituteItem() { SubstitutionType = s };
            }
            catch { }
            // Now we either have a field name spec or some arbitrary text
            if (spec.Length > 0 && spec[0] == ':')
                return new SubstituteItem() { SubstitutionType = SubstitutionType.Field, Parameter = spec.Substring(1) };
            else
                return new SubstituteItem() { SubstitutionType = SubstitutionType.Const, Parameter = "<" + spec + ">" };
        }

        // Set the "FinalValue" properties in the list
        public void SetValues(List<SubstituteItem> ls)
        {
            foreach (SubstituteItem s in ls)
            {
                if (s.SubstitutionType == SubstitutionType.Const)
                {
                    s.FinalValue = s.Parameter; continue;
                }
                if (s.SubstitutionType == SubstitutionType.BatchId)
                {
                    s.FinalValue = BatchId; continue;
                }
                if (s.SubstitutionType == SubstitutionType.DocumentNumber)
                {
                    s.FinalValue = DocumentNumber; continue;
                }
                if (s.SubstitutionType == SubstitutionType.PageNumber)
                {
                    s.FinalValue = PageNumber; continue;
                }
                if (s.SubstitutionType == SubstitutionType.Date)
                {
                    s.FinalValue = DateTime.Now.ToString("yyyy-MM-dd"); continue;
                }
                if (s.SubstitutionType == SubstitutionType.Time)
                {
                    s.FinalValue = DateTime.Now.ToString("HHmmss"); continue;
                }
                if (s.SubstitutionType == SubstitutionType.Host)
                {
                    s.FinalValue = System.Environment.MachineName; continue;
                }
                if (s.SubstitutionType == SubstitutionType.Guid)
                {
                    s.FinalValue = Guid.NewGuid().ToString(); continue;
                }
                if (s.SubstitutionType == SubstitutionType.UniqueId)
                {
                    string tmp = System.IO.Path.GetRandomFileName();
                    s.FinalValue = tmp.Substring(0, 8) + tmp.Substring(9, 3);
                    continue;
                }
                if (s.SubstitutionType == SubstitutionType.Field)
                {
                    KeyValuePair<string,string> v = ValueList.Where(n => n.Key == s.Parameter).FirstOrDefault();
                    s.FinalValue = v.Key == null ? s.Parameter : v.Value ;
                    continue;
                }
            }
        }

        // Combine all FinalValue properties in the list
        public string ComposeResultString(List<SubstituteItem> ls)
        {
            string result = string.Empty;
            foreach(SubstituteItem s in ls) result += s.FinalValue;
            return result;
        }
    }
}
