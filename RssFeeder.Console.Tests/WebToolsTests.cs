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
        [ExpectedException(typeof(ArgumentNullException), "An empty or null baseUrl was inappropriately allowed.")]
        public void TestRepairUrl_BaseMissing()
        {
            // Arrange
            string baseUrl = "";
            string pathAndQuery = "";

            // Act
            var result = WebTools.RepairUrl(baseUrl, pathAndQuery);

            // Assert
        }

        [TestMethod]
        public void TestRepairUrl_EmptyRelativeUrl_ReturnsRoot()
        {
            // Arrange
            string baseUrl = "https://www.drudgereport.com";
            string pathAndQuery = "";

            // Act
            var result = WebTools.RepairUrl(baseUrl, pathAndQuery);

            // Assert
            Assert.IsTrue(result.StartsWith("https"));
            Assert.IsTrue(result.Contains("www.drudgereport.com"));
            Assert.IsTrue(result.EndsWith("/"));
            Assert.IsTrue(IsValidUrl(result));
        }

        [TestMethod]
        public void TestRepairUrl_RelativeUrl_BaseWithoutSlash_PathWithSlash()
        {
            // Arrange
            string baseUrl = "https://www.drudgereport.com";
            string pathAndQuery = "/example-path-query.html";

            // Act
            var result = WebTools.RepairUrl(baseUrl, pathAndQuery);

            // Assert
            Assert.IsTrue(result.StartsWith("https"));
            Assert.IsTrue(result.Contains("www.drudgereport.com"));
            Assert.IsTrue(result.Contains("/example-path"));
            Assert.IsTrue(IsValidUrl(result));
        }

        [TestMethod]
        public void TestRepairUrl_RelativeUrl_BaseWithoutSlash_PathWithoutSlash()
        {
            // Arrange
            string baseUrl = "https://www.drudgereport.com";
            string pathAndQuery = "example-path-query.html";

            // Act
            var result = WebTools.RepairUrl(baseUrl, pathAndQuery);

            // Assert
            Assert.IsTrue(result.StartsWith("https"));
            Assert.IsTrue(result.Contains("www.drudgereport.com"));
            Assert.IsTrue(result.Contains("/example-path"));
            Assert.IsTrue(IsValidUrl(result));
        }

        [TestMethod]
        public void TestRepairUrl_RelativeUrl_BaseWithSlash_PathWithSlash()
        {
            // Arrange
            string baseUrl = "https://www.drudgereport.com/";
            string pathAndQuery = "/example-path-query.html";

            // Act
            var result = WebTools.RepairUrl(baseUrl, pathAndQuery);

            // Assert
            Assert.IsTrue(result.StartsWith("https"));
            Assert.IsTrue(result.Contains("www.drudgereport.com"));
            Assert.IsTrue(!result.Contains("//example-path"));
            Assert.IsTrue(IsValidUrl(result));
        }

        [TestMethod]
        public void TestRepairUrl_RelativeUrl_BaseWithSlash_PathWithoutSlash()
        {
            // Arrange
            string baseUrl = "https://www.drudgereport.com/";
            string pathAndQuery = "example-path-query.html";

            // Act
            var result = WebTools.RepairUrl(baseUrl, pathAndQuery);

            // Assert
            Assert.IsTrue(result.StartsWith("https"));
            Assert.IsTrue(result.Contains("www.drudgereport.com"));
            Assert.IsTrue(result.Contains("/example-path"));
            Assert.IsTrue(IsValidUrl(result));
        }

        [TestMethod]
        public void TestRepairUrl_BadUrl_MissingH()
        {
            // Arrange
            string baseUrl = "https://www.drudgereport.com/";
            string pathAndQuery = "ttps://example.com/example-path-query.html";

            // Act
            var result = WebTools.RepairUrl(baseUrl, pathAndQuery);

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
            string baseUrl = "https://www.drudgereport.com/";
            string pathAndQuery = "//example.com/example-path-query.html";

            // Act
            var result = WebTools.RepairUrl(baseUrl, pathAndQuery);

            // Assert
            Assert.IsTrue(result.StartsWith("https"));
            Assert.IsTrue(result.Contains("example.com"));
            Assert.IsTrue(result.Contains("/example-path"));
            Assert.IsTrue(IsValidUrl(result));
        }
    }
}
