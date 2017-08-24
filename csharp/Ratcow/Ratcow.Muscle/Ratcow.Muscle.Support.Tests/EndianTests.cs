using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ratcow.Muscle.Support.Tests
{
    [TestClass]
    public class EndianTests
    {
        [TestMethod]
        public void EndianInt16()
        {
            short value = 12345; //host format

            //convert to big endian
            var bevalue = EndianUtils.B_HOST_TO_BENDIAN_INT16(value);

            //convert back
            var hostvalue = EndianUtils.B_BENDIAN_TO_HOST_INT16(bevalue);

            //value should equal original
            Assert.AreEqual<short>(value, hostvalue);

            //convert to big endian
            var levalue = EndianUtils.B_HOST_TO_LENDIAN_INT16(value);

            //convert back
            hostvalue = EndianUtils.B_LENDIAN_TO_HOST_INT16(levalue);

            //value should equal original
            Assert.AreEqual<short>(value, hostvalue);
        }

        [TestMethod]
        public void EndianInt32()
        {
            int value = 12345678; //host format

            //convert to big endian
            var bevalue = EndianUtils.B_HOST_TO_BENDIAN_INT32(value);

            //convert back
            var hostvalue = EndianUtils.B_BENDIAN_TO_HOST_INT32(bevalue);

            //value should equal original
            Assert.AreEqual<int>(value, hostvalue);

            //convert to big endian
            var levalue = EndianUtils.B_HOST_TO_LENDIAN_INT32(value);

            //convert back
            hostvalue = EndianUtils.B_LENDIAN_TO_HOST_INT32(levalue);

            //value should equal original
            Assert.AreEqual<int>(value, hostvalue);
        }

        [TestMethod]
        public void EndianInt64()
        {
            Int64 value = 1_234_567_890_123_456_780; //host format

            //convert to big endian
            var bevalue = EndianUtils.B_HOST_TO_BENDIAN_INT64(value);

            //convert back
            var hostvalue = EndianUtils.B_BENDIAN_TO_HOST_INT64(bevalue);

            //value should equal original
            Assert.AreEqual<Int64>(value, hostvalue);

            //convert to big endian
            var levalue = EndianUtils.B_HOST_TO_LENDIAN_INT64(value);

            //convert back
            hostvalue = EndianUtils.B_LENDIAN_TO_HOST_INT64(levalue);

            //value should equal original
            Assert.AreEqual<Int64>(value, hostvalue);
        }
    }
}
