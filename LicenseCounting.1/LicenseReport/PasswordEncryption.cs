using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace LicenseReport
{
    public class PasswordEncryption
    {
        public static string Encrypt(string value)
        {
            byte[] input = Encoding.UTF8.GetBytes(value);
            byte[] output = Encoding.UTF8.GetBytes(value);

            byte[] key = Encoding.UTF8.GetBytes(">mG$bM:Y,j56R))4");
            byte[] iv = Encoding.UTF8.GetBytes("9x@9N8Vk.E#k8JqZ");
            using (AesCryptoServiceProvider aes = new AesCryptoServiceProvider())
            {
                aes.Key = key;
                aes.IV = iv;
                using (ICryptoTransform encrypt = aes.CreateEncryptor())
                    output = encrypt.TransformFinalBlock(input, 0, input.Length);
            }
            string result = Convert.ToBase64String(output);
            return result;
        }

        public static string Decrypt(string value)
        {
            byte[] input = Convert.FromBase64String(value);
            byte[] output = Convert.FromBase64String(value);

            byte[] key = Encoding.UTF8.GetBytes(">mG$bM:Y,j56R))4");
            byte[] iv = Encoding.UTF8.GetBytes("9x@9N8Vk.E#k8JqZ");
            using (AesCryptoServiceProvider aes = new AesCryptoServiceProvider())
            {
                aes.Key = key;
                aes.IV = iv;
                using (ICryptoTransform decrypt = aes.CreateDecryptor(aes.Key, aes.IV))
                    output = decrypt.TransformFinalBlock(input, 0, input.Length);
            }
            string result = Encoding.UTF8.GetString(output);
            return result;
        }
    }
}
