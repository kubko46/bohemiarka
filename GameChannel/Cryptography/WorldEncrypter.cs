// WingsEmu
// 
// Developed by NosWings Team

using System;
using System.Text;

namespace GameChannel.Cryptography
{
    internal static class WorldEncrypter
    {
        internal static byte[] Encrypt(string packet, Encoding encoding)
        {
            byte[] strBytes = Encoding.Convert(Encoding.UTF8, encoding, Encoding.UTF8.GetBytes(packet));
            byte[] encryptedData = new byte[strBytes.Length + (int)Math.Ceiling((decimal)strBytes.Length / 126) + 1];

            int j = 0;
            for (int i = 0; i < strBytes.Length; i++)
            {
                if ((i % 126) == 0)
                {
                    encryptedData[i + j] = (byte)(strBytes.Length - i > 126 ? 126 : strBytes.Length - i);
                    j++;
                }

                encryptedData[i + j] = (byte)~strBytes[i];
            }

            encryptedData[^1] = 0xFF;
            return encryptedData;
        }
    }
}