using System;

namespace GitCredentialManager
{
    public static class Base64UrlConvert
    {
        public static string Encode(byte[] data, bool includePadding = true)
        {
            const char base64PadCharacter = '=';
            const char base64Character62 = '+';
            const char base64Character63 = '/';
            const char base64UrlCharacter62 = '-';
            const char base64UrlCharacter63 = '_';

            // The base64url format is the same as regular base64 format except:
            //   1. character 62 is "-" (minus) not "+" (plus)
            //   2. character 63 is "_" (underscore) not "/" (slash)
            string base64 = Convert.ToBase64String(data);
            
            // Use Span-based approach to avoid multiple string allocations
            int trimLength = includePadding ? base64.Length : base64.TrimEnd(base64PadCharacter).Length;
            char[] result = new char[trimLength];
            
            for (int i = 0; i < trimLength; i++)
            {
                char c = base64[i];
                if (c == base64Character62)
                    result[i] = base64UrlCharacter62;
                else if (c == base64Character63)
                    result[i] = base64UrlCharacter63;
                else
                    result[i] = c;
            }

            return new string(result);
        }
    }
}
