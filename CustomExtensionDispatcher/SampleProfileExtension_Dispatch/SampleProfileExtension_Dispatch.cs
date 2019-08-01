using ScriptingUtilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScriptingUtilities
{
    public class SampleProfileExtension_Dispatch : CustomExtensionDispatcher
    {
        public SampleProfileExtension_Dispatch()
        {
            RegisterProfileExtension(typeof(Extension1), "Extension1");
            RegisterProfileExtension(typeof(Extension2));
        }
    }
}
