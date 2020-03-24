using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfonetPOS.Core.DB
{
    public class EncryptionManager
    {
        /// <summary>
        /// Method to return the encrypted text
        /// </summary>
        /// <param name="text">Password</param>
        /// <returns>Encrypted Text</returns>
        public string EncryptText(string text)
        {
            var passKey = "gb$171a?9v@4tFeD<p";
            var time = DateTime.Now;
            var appendTime = string.Format("{0:00.##}{1:00.##}.{2:00.##}{0:00.##}{1:00.##}.{2:00.##}", time.Hour, time.Minute, time.Second);
            var encryptedText = $"{text}{Strings.Chr(255)}{appendTime}";
            var value = Encrypt(encryptedText, passKey);
            return value;
        }

        /// <summary>
        /// Method to return the decrypted password
        /// </summary>
        /// <param name="text">Username</param>
        /// <returns>Decrypted Text</returns>
        public string DecryptText(string text)
        {
            var passKey = "gb$171a?9v@4tFeD<p";
            var password = Encrypt(text, passKey);
            int length = password.IndexOf(Strings.Chr(255));
            var result = password.Substring(0, length);
            return result;
        }

        /// <summary>
        /// Method to encrypt the text
        /// </summary>
        /// <param name="inputText">Input text</param>
        /// <param name="passKey">Passkey</param>
        /// <returns>Encrypt text</returns>
        private string Encrypt(string inputText, string passKey)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < inputText.Length; i++)
                sb.Append(Strings.Chr((Strings.Asc(inputText[i]) ^ Strings.Asc(passKey[(i % passKey.Length)]))));
            return sb.ToString();
        }
    }
}

