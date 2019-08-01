using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScriptingUtilities
{
    public class PreventBrokenBatchesProfileExtension_Dispatch : CustomExtensionDispatcher
    {
        public PreventBrokenBatchesProfileExtension_Dispatch()
        {
            RegisterProfileExtension(typeof(PreventBrokenBatchesProfileExtension), "PreventBrokenBatches");
        }
    }
}
