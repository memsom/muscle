using System.Text;
using Ratcow.Muscle.Support;

namespace Ratcow.Muscle.Message.Legacy
{
    public static class MessageUtils
    {
        /// Returns the given 'what' constant as a human readable 4-byte 
        /// string, e.g. "BOOL", "BYTE", etc.
        /// <param name="w">Any 32-bit value you would like to have turned 
        /// into a string</param>
        public static string WhatString(int w)
        {
            byte[] temp = new byte[4];
            temp[0] = (byte)((w >> 24) & 0xFF);
            temp[1] = (byte)((w >> 16) & 0xFF);
            temp[2] = (byte)((w >> 8) & 0xFF);
            temp[3] = (byte)((w >> 0) & 0xFF);

            Decoder d = Encoding.UTF8.GetDecoder();

            int charArrayLen = d.GetCharCount(temp, 0, temp.Length);
            char[] charArray = new char[charArrayLen];

            int charsDecoded = d.GetChars(temp, 0, temp.Length, charArray, 0);

            return new string(charArray, 0, charsDecoded - 1);
        }
    }
}


