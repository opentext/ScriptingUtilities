using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Xml;
using DOKuStar.Data.Xml;
using DOKuStar.Runtime.ScriptingHost;
using DOKuStar.Runtime.Sitemap;

namespace ScriptingUtilities
{
    public partial class CustomExtensionDispatcher : Captaris.Services.BaseCustomProfileExtension
    {
        class ExtensionDefinition
        {
            public Captaris.Services.BaseCustomProfileExtension ProfileExtension { get; set; }
            public string Name { get; set; } = null;
        }

        List<ExtensionDefinition> profileExtensions = new List<ExtensionDefinition>();
        protected void RegisterProfileExtension(Type profileExtensionType, string name = null)
        {
            profileExtensions.Add(new ExtensionDefinition()
            {
                ProfileExtension = (Captaris.Services.BaseCustomProfileExtension)Activator.CreateInstance(profileExtensionType),
                Name = name,
            });
        }

        private List<Captaris.Services.BaseCustomProfileExtension> getActiveProfileExtensions(DataPool pool, bool all=false)
        {
            List<Captaris.Services.BaseCustomProfileExtension> result = new List<Captaris.Services.BaseCustomProfileExtension>();
            result = profileExtensions
                .Where(n => all | n.Name == null | pool.RootNode.Annotations[n.Name] != null)
                .Select(n => n.ProfileExtension)
                .ToList();
            return result;
        }

        public override XmlDocument transform(XmlDocument data, IParameters parameters)
        {
            base.transform(data, parameters);
            DataPool pool = new DataPool(data);
            XmlDocument xd = data;
            bool all = ScriptingManager.GetScriptName(pool) == ScriptingManager.mainRecognitionExceptionName;

            foreach (Captaris.Services.BaseCustomProfileExtension pe in getActiveProfileExtensions(pool, all))
                xd = pe.transform(xd, parameters);

            return xd;
        }

        public override bool FieldChangingBySce(IField field, ref string value, ref Rectangle zone, Rectangle sceRawZone, ref bool handled)
        {
            bool returnValue = true;
            foreach (Captaris.Services.BaseCustomProfileExtension pe in getActiveProfileExtensions(field.OwnerDataPool))
            {
                if (pe.FieldChangingBySce(field, ref value, ref zone, sceRawZone, ref handled) == false)
                    returnValue = false;
                if (handled)
                    return returnValue;
            }
            return returnValue;
        }

        public override void FieldDeactivated(IField field, bool sce, ref bool handled)
        {
            foreach (Captaris.Services.BaseCustomProfileExtension pe in getActiveProfileExtensions(field.OwnerDataPool))
            {
                pe.FieldDeactivated(field, sce, ref handled);
                if (handled) return;
            }
        }

        public override void FieldChanged(IField field, ref bool handled)
        {
            foreach (Captaris.Services.BaseCustomProfileExtension pe in getActiveProfileExtensions(field.OwnerDataPool))
            {
                pe.FieldChanged(field, ref handled);
                if (handled) return;
            }
        }
    }

}
