// WingsEmu
// 
// Developed by NosWings Team

using System;
using System.Text;

namespace LoginServer
{
    public static class NostaleLoginEncrypter
    {
        public static ReadOnlyMemory<byte> Encode(string packet, Encoding encoding)
        {
            packet += " ";
            byte[] tmp = encoding.GetBytes(packet);
            if (tmp.Length == 0)
            {
                return null;
            }

            for (int i = 0; i < packet.Length; i++)
            {
                tmp[i] = Convert.ToByte(tmp[i] + 15);
            }

            tmp[^1] = 25;
            return tmp;
        }
    }
}