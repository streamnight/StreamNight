using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace StreamNight.SupportLibs
{
    public class Hmac
    {
        public static string SignMessage(string message, string key)
        {
            string output;
            long currentTime = DateTimeOffset.Now.ToUnixTimeSeconds();

            // Add timestamp to message. ~ is the separator (change the API if this is also changed)
            message = message.Insert(0, currentTime.ToString() + "~");
            // Initialize the keyed hash object.
            using (HMACSHA256 hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key)))
            {
                // Compute the hash of the input file.
                byte[] hashValue = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
                output = BitConverter.ToString(hashValue).Replace("-", "") + message;
            }

            return output;
        }

        public static bool VerifyMessage(string signedMessage, string key)
        {
            using (HMACSHA256 hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key)))
            {
                // Parse the correct number of characters and convert them to a byte array.
                byte[] storedHash = ParseHex(signedMessage.Substring(0, hmac.HashSize / 4));
                int messageOffset = hmac.HashSize / 4;

                byte[] computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(signedMessage, messageOffset, signedMessage.Length - messageOffset));

                // compare the computed hash with the stored value
                if (computedHash.SequenceEqual(storedHash))
                {
                    // Remove the HMAC hash from the signed message
                    string messageContent = signedMessage.Substring(messageOffset);

                    string[] messageSplit = messageContent.Split('~', 2);
                    long.TryParse(messageSplit[0], out long timestamp);

                    if (timestamp != 0)
                    {
                        long currentTime = DateTimeOffset.Now.ToUnixTimeSeconds();

                        if (currentTime - timestamp < 30 || timestamp - currentTime > 30)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }
        public static string DecodeMessage(string signedMessage)
        {
            using (HMACSHA256 hmac = new HMACSHA256(new byte[0]))
            {
                int messageOffset = hmac.HashSize / 4;

                return signedMessage.Substring(messageOffset).Split('~', 2)[1];
            }
        }

        // https://stackoverflow.com/questions/854012/how-to-convert-hex-to-a-byte-array
        private static byte[] ParseHex(string hex)
        {
            int offset = hex.StartsWith("0x") ? 2 : 0;
            if ((hex.Length % 2) != 0)
            {
                throw new ArgumentException("Invalid length: " + hex.Length);
            }
            byte[] ret = new byte[(hex.Length - offset) / 2];

            for (int i = 0; i < ret.Length; i++)
            {
                ret[i] = (byte)((ParseNibble(hex[offset]) << 4)
                                 | ParseNibble(hex[offset + 1]));
                offset += 2;
            }
            return ret;
        }

        private static int ParseNibble(char c)
        {
            if (c >= '0' && c <= '9')
            {
                return c - '0';
            }
            if (c >= 'A' && c <= 'F')
            {
                return c - 'A' + 10;
            }
            if (c >= 'a' && c <= 'f')
            {
                return c - 'a' + 10;
            }
            throw new ArgumentException("Invalid hex digit: " + c);
        }
    }
}
