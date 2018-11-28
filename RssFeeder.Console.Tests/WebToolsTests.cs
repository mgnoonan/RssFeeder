using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RssFeeder.Console.Utility;

namespace RssFeeder.Console.Tests
{
    [TestClass]
    public class WebToolsTests
    {
        private bool IsValidUrl(string url)
        {
            try
            {
                var u = new Uri(url);
                return true;
            }
            catch (UriFormatException)
            {
                return false;
            }
        }

        [TestMethod]
        public void TestRepairUrl_EmptyRelativeUrl_ReturnsRoot()
        {
            // Arrange
            string pathAndQuery = "";

            // Act
            var result = WebTools.RepairUrl(pathAndQuery);

            // Assert
            Assert.IsTrue(result.EndsWith("/"));
            Assert.IsTrue(IsValidUrl(result));
        }

        [TestMethod]
        public void TestRepairUrl_RelativeUrl_PathStartsWithSlash()
        {
            // Arrange
            string pathAndQuery = "/example-path-query.html";

            // Act
            var result = WebTools.RepairUrl(pathAndQuery);

            // Assert
            Assert.IsTrue(result.StartsWith("/example-path"));
            Assert.IsTrue(IsValidUrl(result));
        }

        [TestMethod]
        public void TestRepairUrl_RelativeUrl_PathStartsWithoutSlash()
        {
            // Arrange
            string pathAndQuery = "example-path-query.html";

            // Act
            var result = WebTools.RepairUrl(pathAndQuery);

            // Assert
            Assert.IsTrue(result.StartsWith("/example-path"));
            Assert.IsTrue(IsValidUrl(result));
        }

        [TestMethod]
        public void TestRepairUrl_BadUrl_MissingH()
        {
            // Arrange
            string pathAndQuery = "ttps://example.com/example-path-query.html";

            // Act
            var result = WebTools.RepairUrl(pathAndQuery);

            // Assert
            Assert.IsTrue(result.StartsWith("https"));
            Assert.IsTrue(result.Contains("example.com"));
            Assert.IsTrue(result.Contains("/example-path"));
            Assert.IsTrue(IsValidUrl(result));
        }

        [TestMethod]
        public void TestRepairUrl_BadUrl_MissingProtocol()
        {
            // Arrange
            string pathAndQuery = "//example.com/example-path-query.html";

            // Act
            var result = WebTools.RepairUrl(pathAndQuery);

            // Assert
            Assert.IsTrue(result.StartsWith("https"));
            Assert.IsTrue(result.Contains("example.com"));
            Assert.IsTrue(result.Contains("/example-path"));
            Assert.IsTrue(IsValidUrl(result));
        }
    }
}
