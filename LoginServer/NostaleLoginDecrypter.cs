// WingsEmu
// 
// Developed by NosWings Team

using System;
using System.Text;

namespace LoginServer
{
    public static class NostaleLoginDecrypter
    {
        public static string Decode(ReadOnlySpan<byte> bytesBuffer)
        {
            var decryptedPacket = new StringBuilder();
            foreach (byte character in bytesBuffer)
            {
                decryptedPacket.Append(character > 14
                    ? Convert.ToChar(character - 0xF ^ 0xC3)
                    : Convert.ToChar(0x100 - (0xF - character) ^ 0xC3));
            }

            return decryptedPacket.ToString();
        }
    }
}