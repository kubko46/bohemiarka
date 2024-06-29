// WingsEmu
// 
// Developed by NosWings Team

using System;
using System.Text;
using PhoenixLib.MultiLanguage;

namespace GameChannel.Cryptography
{
    public static class EncodingExtensions
    {
        public static Encoding GetEncoding(this RegionLanguageType key)
        {
            switch (key)
            {
                case RegionLanguageType.EN:
                case RegionLanguageType.FR:
                case RegionLanguageType.ES:
                    return Encoding.GetEncoding(1252);
                case RegionLanguageType.DE:
                case RegionLanguageType.PL:
                case RegionLanguageType.IT:
                case RegionLanguageType.RU:
                    return Encoding.GetEncoding(1250);
                case RegionLanguageType.CZ:
                    return Encoding.GetEncoding(1250);
                case RegionLanguageType.TR:
                    return Encoding.GetEncoding(1254);
                default:
                    throw new ArgumentOutOfRangeException(nameof(key), key, null);
            }
        }
    }
}