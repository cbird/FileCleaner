using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FileCleaner.Tests
{
    [TestClass]
    public class UnitTests
    {
        [TestMethod]
        public void LoadConfig()
        {
            var config = Program.LoadConfig("./test-config.json");
            Assert.IsNotNull(config, "Something went wrong loading the config file");
        }

        [TestMethod]
        public void CleanFolders()
        {
            var config = Program.LoadConfig("./test-config.json");
            var result = true;

            try
            {
                Program.CleanFolders(config);
            }
            catch
            {
                result = false;
            }

            Assert.IsTrue(result, "Something went wrong deleting folders");
        }
    }
}
