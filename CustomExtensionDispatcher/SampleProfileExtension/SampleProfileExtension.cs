using System;
using DOKuStar.Data.Xml;

namespace ScriptingUtilities
{
    public class Extension1 : ProfileExtension_Transform
    {
        protected override DataPool Indexing(DataPool pool)
        {
            pool.RootNode.Documents[0].Fields["F1"].Value = "Hello";
            return pool;
        }
        public override void FieldDeactivated(IField field, bool sce, ref bool handled)
        {
            field.Value = field.Value.ToUpper();
        }
    }

    public class Extension2 : ProfileExtension_Transform
    {
        protected override DataPool Indexing(DataPool pool)
        {
            pool.RootNode.Documents[0].Fields["F2"].Value = "World!";
            return pool;
        }
        public override void FieldDeactivated(IField field, bool sce, ref bool handled)
        {
            field.Value = field.Value + "_" + field.Value.Length.ToString();
        }
    }
}
