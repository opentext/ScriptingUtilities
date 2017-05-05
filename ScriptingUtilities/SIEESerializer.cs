using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

namespace ScriptingUtilities
{
    public class SIEESerializer
    {
        public static string ObjectToString(object o)
        {
            MemoryStream ms = new MemoryStream();
            BinaryFormatter bf = new BinaryFormatter();
            bf.Serialize(ms, o);
            ms.Position = 0;
            BinaryReader br = new BinaryReader(ms);
            byte[] b = br.ReadBytes((int)ms.Length);
            string encode = Convert.ToBase64String(b);
            return encode;
        }

        public static object StringToObject(string s)
        {
            byte[] b = Convert.FromBase64String(s);
            BinaryFormatter bf = new BinaryFormatter();
            MemoryStream ms = new MemoryStream(b);
            object o = bf.Deserialize(ms);
            return o;
        }

        public static object Clone(object o)
        {
            return SIEESerializer.StringToObject(SIEESerializer.ObjectToString(o));
        }
    }
}
