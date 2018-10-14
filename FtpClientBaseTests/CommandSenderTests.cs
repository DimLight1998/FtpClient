using Microsoft.VisualStudio.TestTools.UnitTesting;
using FtpClientBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FtpClientBase.Tests
{
    [TestClass()]
    public class CommandSenderTests
    {
        [TestMethod()]
        public void IsFullResponseTest()
        {
            Assert.IsTrue(CommandSender.IsFullResponse("123 hello world!\r\n"));
            Assert.IsTrue(CommandSender.IsFullResponse("123 123 hello world!\r\n"));
            Assert.IsFalse(CommandSender.IsFullResponse("123 hello world!\r"));
            Assert.IsFalse(CommandSender.IsFullResponse("123 hello world!\r\n blah"));
            Assert.IsFalse(CommandSender.IsFullResponse("123-hello world!\r\n"));
            Assert.IsFalse(CommandSender.IsFullResponse("1234 hello world!\r\n"));
            Assert.IsFalse(CommandSender.IsFullResponse("12 hello world!\r\n"));

            Assert.IsTrue(CommandSender.IsFullResponse("123-hello, world\r\n234hhh\r\n123 end of message\r\n"));
            Assert.IsFalse(CommandSender.IsFullResponse("123 hello, world\r\n234hhh\r\n123 end of message\r\n"));
            Assert.IsFalse(CommandSender.IsFullResponse("123hello, world\r\n234hhh\r\n123 end of message\r\n"));
            Assert.IsFalse(CommandSender.IsFullResponse("123-hello, world\r\n234hhh\r\n123-end of message\r\n"));
            Assert.IsTrue(CommandSender.IsFullResponse("123-hello, world\r\n123 hhh\r\n123 end of message\r\n"));
            Assert.IsFalse(CommandSender.IsFullResponse("123-hello, world\r\n234hhh\r\n123-end of message\r\n123"));
            Assert.IsFalse(CommandSender.IsFullResponse("123-hello, world\r\n234hhh\r\n123 end of message\r"));
        }
    }
}