using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace Ratcow.Muscle.Message.Tests
{
    using Message = Ratcow.Muscle.Message.Legacy.Message;

    [TestClass]
    public class BasicLegacyMessageTests
    {
        [TestMethod]
        public void BasicTest0()
        {
            var message = new Message();

            Assert.IsNotNull(message);
        }

        [TestMethod]
        public void BasicTest1()
        {
            var message = new Message(1000);

            Assert.IsNotNull(message);
            Assert.AreEqual(message.What, 1000);
        }

        [TestMethod]
        public void BasicTest2()
        {
            var message = new Message(1000);
            message.SetBoolean("test", true);

            var test = message.GetBoolean("test");

            Assert.IsTrue(test);
        }

        [TestMethod]
        public void BasicTest3()
        {
            var message = new Message(1000);
            message.SetBoolean("test", true);
            message.SetString("I am cool", "this is a test");

            var ms = new MemoryStream();
            var bw = new BinaryWriter(ms);
            message.Flatten(bw);
            bw = null;


            Assert.AreEqual(message.FlattenedSize, ms.Length);

            ms.Seek(0, SeekOrigin.Begin);

            var unflattened = new Message();
            using (var br = new BinaryReader(ms))
            {
                unflattened.Unflatten(br, (int)ms.Length);
            }

            Assert.AreEqual(unflattened.What, 1000);
            var test = unflattened.GetBoolean("test");
            Assert.IsTrue(test);

            var i_am_cool = unflattened.GetString("I am cool");
            Assert.AreEqual<string>(i_am_cool, "this is a test");
        }
    }
}
