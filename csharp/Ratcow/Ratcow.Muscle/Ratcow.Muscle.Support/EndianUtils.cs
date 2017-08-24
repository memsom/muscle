using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ratcow.Muscle.Support
{
    using int16 = Int16;
    using int32 = Int32;
    using int64 = Int64;
    using status_t = UInt32;



    public static class EndianUtils
    {
        public static readonly bool B_HOST_IS_LENDIAN;                              /**< Set to 1 if native CPU is little-endian; set to 0 if native CPU is big-endian. */
        public static readonly bool B_HOST_IS_BENDIAN;                              /**< Set to 1 if native CPU is big-endian; set to 0 if native CPU is little-endian. */

        static EndianUtils()
        {
            B_HOST_IS_LENDIAN = BitConverter.IsLittleEndian;
            B_HOST_IS_BENDIAN = !BitConverter.IsLittleEndian;
        }

        public static int64 B_HOST_TO_LENDIAN_INT64(int64 arg)
        {
            return SwapInt64(B_HOST_IS_BENDIAN, arg);
        }

        public static int32 B_HOST_TO_LENDIAN_INT32(int32 arg)
        {
            return SwapInt32(B_HOST_IS_BENDIAN, arg);
        }

        public static int16 B_HOST_TO_LENDIAN_INT16(int16 arg)
        {
            return SwapInt16(B_HOST_IS_BENDIAN, arg);
        }

        public static int64 B_HOST_TO_BENDIAN_INT64(int64 arg)
        {
            return SwapInt64(B_HOST_IS_LENDIAN, arg);
        }

        public static int32 B_HOST_TO_BENDIAN_INT32(int32 arg)
        {
            return SwapInt32(B_HOST_IS_LENDIAN, arg);
        }

        public static int16 B_HOST_TO_BENDIAN_INT16(int16 arg)
        {
            return SwapInt16(B_HOST_IS_LENDIAN, arg);
        }



        public static int64 B_LENDIAN_TO_HOST_INT64(int64 arg)
        {
            return SwapInt64(B_HOST_IS_BENDIAN, arg);
        }

        public static int32 B_LENDIAN_TO_HOST_INT32(int32 arg)
        {
            return SwapInt32(B_HOST_IS_BENDIAN, arg);
        }

        public static int16 B_LENDIAN_TO_HOST_INT16(int16 arg)
        {
            return SwapInt16(B_HOST_IS_BENDIAN, arg);
        }

        public static int64 B_BENDIAN_TO_HOST_INT64(int64 arg)
        {
            return SwapInt64(B_HOST_IS_LENDIAN, arg);
        }

        public static int32 B_BENDIAN_TO_HOST_INT32(int32 arg)
        {
            return SwapInt32(B_HOST_IS_LENDIAN, arg);
        }

        public static int16 B_BENDIAN_TO_HOST_INT16(int16 arg)
        {
            return SwapInt16(B_HOST_IS_LENDIAN, arg);
        }

        #region Helpers


        static short SwapInt16(bool shouldswap, short v)
        {
            if (shouldswap)
                return (short)(((v & 0xff) << 8) | ((v >> 8) & 0xff));

            return v;
        }



        static ushort SwapUInt16(bool shouldswap, ushort v)
        {
            if (shouldswap)
            {
                return (ushort)(((v & 0xff) << 8) | ((v >> 8) & 0xff));
            }

            return v;
        }



        static int SwapInt32(bool shouldswap, int v)
        {
            if (shouldswap)
            {
                return (int)(((SwapInt16(shouldswap, (short)v) & 0xffff) << 0x10) | (SwapInt16(shouldswap, (short)(v >> 0x10)) & 0xffff));
            }

            return v;
        }



        static uint SwapUInt32(bool shouldswap, uint v)
        {
            if (shouldswap)
            {
                return (uint)(((SwapUInt16(shouldswap, (ushort)v) & 0xffff) << 0x10) | (SwapUInt16(shouldswap, (ushort)(v >> 0x10)) & 0xffff));
            }

            return v;
        }



        static long SwapInt64(bool shouldswap, long v)
        {
            if (shouldswap)
            {
                return (long)(((SwapInt32(shouldswap, (int)v) & 0xffffffffL) << 0x20) | (SwapInt32(shouldswap, (int)(v >> 0x20)) & 0xffffffffL));
            }

            return v;
        }



        static ulong SwapUInt64(bool shouldswap, ulong v)
        {
            if (shouldswap)
            {
                return (ulong)(((SwapUInt32(shouldswap, (uint)v) & 0xffffffffL) << 0x20) | (SwapUInt32(shouldswap, (uint)(v >> 0x20)) & 0xffffffffL));
            }

            return v;
        }

        #endregion
    }
}
