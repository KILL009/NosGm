using System;
using System.Collections.Generic;
using System.Text;

namespace NosGm.Core
{
    public class WorldCryptography : CryptographyBase
    {
        #region Instantiation

        public WorldCryptography() : base(true)
        {
        }

        #endregion

        #region Methods

        public static string Decrypt2(string str)
        {
            var receiveData = new List<byte>();
            char[] table = { ' ', '-', '.', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'n' };
            for (var i = 0; i < str.Length; i++)
                if (str[i] <= 0x7A)
                {
                    int len = str[i];

                    for (var j = 0; j < len; j++)
                    {
                        i++;

                        try
                        {
                            receiveData.Add(unchecked((byte)(str[i] ^ 0xFF)));
                        }
                        catch (Exception)
                        {
                            receiveData.Add(255);
                        }
                    }
                }
                else
                {
                    int len = str[i];
                    len &= 0x7F;

                    for (var j = 0; j < len; j++)
                    {
                        i++;
                        int highbyte;
                        try
                        {
                            highbyte = str[i];
                        }
                        catch (Exception)
                        {
                            highbyte = 0;
                        }

                        highbyte &= 0xF0;
                        highbyte >>= 0x4;

                        int lowbyte;
                        try
                        {
                            lowbyte = str[i];
                        }
                        catch (Exception)
                        {
                            lowbyte = 0;
                        }

                        lowbyte &= 0x0F;

                        if (highbyte != 0x0 && highbyte != 0xF)
                        {
                            receiveData.Add(unchecked((byte)table[highbyte - 1]));
                            j++;
                        }

                        if (lowbyte != 0x0 && lowbyte != 0xF) receiveData.Add(unchecked((byte)table[lowbyte - 1]));
                    }
                }

            return Encoding.UTF8.GetString(Encoding.Convert(Encoding.Default, Encoding.UTF8, receiveData.ToArray()));
        }

        public override string Decrypt(byte[] data, int sessionId = 0)
        {
            var sessionKey = sessionId & 0xFF;
            var sessionNumber = unchecked((byte)(sessionId >> 6));
            sessionNumber &= 0xFF;
            sessionNumber &= unchecked((byte)0x80000003);

            var decryptPart = new StringBuilder();
            switch (sessionNumber)
            {
                case 0:

                    foreach (var character in data)
                    {
                        var firstbyte = unchecked((byte)(sessionKey + 0x40));
                        var highbyte = unchecked((byte)(character - firstbyte));
                        decryptPart.Append((char)highbyte);
                    }

                    break;

                case 1:
                    foreach (var character in data)
                    {
                        var firstbyte = unchecked((byte)(sessionKey + 0x40));
                        var highbyte = unchecked((byte)(character + firstbyte));
                        decryptPart.Append((char)highbyte);
                    }

                    break;

                case 2:
                    foreach (var character in data)
                    {
                        var firstbyte = unchecked((byte)(sessionKey + 0x40));
                        var highbyte = unchecked((byte)((character - firstbyte) ^ 0xC3));
                        decryptPart.Append((char)highbyte);
                    }

                    break;

                case 3:
                    foreach (var character in data)
                    {
                        var firstbyte = unchecked((byte)(sessionKey + 0x40));
                        var highbyte = unchecked((byte)((character + firstbyte) ^ 0xC3));
                        decryptPart.Append((char)highbyte);
                    }

                    break;

                default:
                    decryptPart.Append((char)0xF);
                    break;
            }

            var decrypted = new StringBuilder();

            var encryptedSplit = decryptPart.ToString().Split((char)0xFF);
            for (var i = 0; i < encryptedSplit.Length; i++)
            {
                decrypted.Append(Decrypt2(encryptedSplit[i]));
                if (i < encryptedSplit.Length - 2) decrypted.Append((char)0xFF);
            }

            return decrypted.ToString();
        }

        public override string DecryptCustomParameter(byte[] data)
        {
            try
            {
                var builder = new StringBuilder();
                for (var i = 1; i < data.Length; i++)
                {
                    if (Convert.ToChar(data[i]) == 0xE) return builder.ToString();

                    var firstByte = Convert.ToInt32(data[i] - 0xF);
                    var secondByte = firstByte;
                    secondByte &= 0xF0;
                    firstByte = Convert.ToInt32(firstByte - secondByte);
                    secondByte >>= 0x4;

                    switch (secondByte)
                    {
                        case 0:
                        case 1:
                            builder.Append(' ');
                            break;

                        case 2:
                            builder.Append('-');
                            break;

                        case 3:
                            builder.Append('.');
                            break;

                        default:
                            secondByte += 0x2C;
                            builder.Append(Convert.ToChar(secondByte));
                            break;
                    }

                    switch (firstByte)
                    {
                        case 0:
                        case 1:
                            builder.Append(' ');
                            break;

                        case 2:
                            builder.Append('-');
                            break;

                        case 3:
                            builder.Append('.');
                            break;

                        default:
                            firstByte += 0x2C;
                            builder.Append(Convert.ToChar(firstByte));
                            break;
                    }
                }

                return builder.ToString();
            }
            catch (OverflowException)
            {
                return "";
            }
        }

        public override byte[] Encrypt(string data)
        {
            var dataBytes = Encoding.Default.GetBytes(data);
            var encryptedData = new byte[dataBytes.Length + (int)Math.Ceiling((decimal)dataBytes.Length / 0x7E) + 1];
            for (int i = 0, j = 0; i < dataBytes.Length; i++)
            {
                if (i % 0x7E == 0)
                {
                    encryptedData[i + j] = (byte)(dataBytes.Length - i > 0x7E ? 0x7E : dataBytes.Length - i);
                    j++;
                }

                encryptedData[i + j] = (byte)~dataBytes[i];
            }

            encryptedData[encryptedData.Length - 1] = 0xFF;
            return encryptedData;
        }

        #endregion
    }
}