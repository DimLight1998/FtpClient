#region

using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace FtpClientBase.Tests
{
    [TestClass]
    public class ConnectionTests
    {
        private const string Host = "144.208.69.31";
        private const int Port = 21;
        private const string Username = "dlpuser@dlptest.com";
        private const string Password = "e73jzTRTNqCN9PYAAjjn";

        [TestMethod]
        public void EstablishTest()
        {
            var connection = new Connection(Host, Port);
            connection.Establish(Username, Password);
            connection.Destory();

            Assert.IsTrue(true);
        }
    }
}