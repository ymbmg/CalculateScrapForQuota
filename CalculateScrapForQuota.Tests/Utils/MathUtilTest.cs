using System.Collections.Generic;
using CalculateScrapForQuota.Utils;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateScrapForQuota.Tests.Utils
{
    [TestClass]
    [TestSubject(typeof(MathUtil))]
    public class MathUtilTest
    {
        private class GrabbableObjectMock
        {
            public int scrapValue { get; set; }
        }
        
        [TestMethod]
        public void FindBestCombination_ReturnsCorrectCombinationAndValue_Generic()
        {
            // Arrange
            int quota = 39;
            var items = new List<GrabbableObjectMock>
            {
                new GrabbableObjectMock { scrapValue = 1 },
                new GrabbableObjectMock { scrapValue = 5 },
                new GrabbableObjectMock { scrapValue = 19 },
                new GrabbableObjectMock { scrapValue = 34 },
                new GrabbableObjectMock { scrapValue = 35 },
                new GrabbableObjectMock { scrapValue = 36 }
            };

            // Act
            var (combination, totalValue) = MathUtil.FindBestCombination(items, quota, item => item.scrapValue);

            // Assert
            Assert.IsNotNull(combination, "The combination should not be null.");
            Assert.AreEqual(39, totalValue, "The total value should be equal to the quota.");
            Assert.IsTrue(combination.Count > 0, "The combination should contain items.");
            Assert.IsTrue(combination.Exists(item => item.scrapValue == 34), "The combination should include the item with scrap value 34.");
            Assert.IsTrue(combination.Exists(item => item.scrapValue == 5), "The combination should include the item with scrap value 5.");
        }
        
        [TestMethod]
        public void FindBestCombination_ReturnsCorrectCombinationAndValue_MoreThanQuota()
        {
            // Arrange
            int quota = 10;
            var items = new List<GrabbableObjectMock>
            {
                new GrabbableObjectMock { scrapValue = 8 },
                new GrabbableObjectMock { scrapValue = 9 }
            };

            // Act
            var (combination, totalValue) = MathUtil.FindBestCombination(items, quota, item => item.scrapValue);

            // Assert
            Assert.IsNotNull(combination, "The combination should not be null.");
            Assert.AreEqual(17, totalValue, "The total value should be more than quota.");
            Assert.IsTrue(combination.Count == 2, "The combination should contain two items.");
            Assert.IsTrue(combination.Exists(item => item.scrapValue == 9), "The combination should include the item with scrap value 9.");
            Assert.IsTrue(combination.Exists(item => item.scrapValue == 8), "The combination should include the item with scrap value 8.");
        }
        
        [TestMethod]
        public void FindBestCombination_ReturnsCorrectCombinationAndValue_LessThanQuota()
        {
            // Arrange
            int quota = 9;
            var items = new List<GrabbableObjectMock>
            {
                new GrabbableObjectMock { scrapValue = 5 },
                new GrabbableObjectMock { scrapValue = 4 }
            };

            // Act
            var (combination, totalValue) = MathUtil.FindBestCombination(items, quota, item => item.scrapValue);

            // Assert
            Assert.IsNotNull(combination, "The combination should not be null.");
            Assert.AreEqual(9, totalValue, "The total value should be less than quota.");
            Assert.IsTrue(combination.Count == 2, "The combination should contain two items.");
            Assert.IsTrue(combination.Exists(item => item.scrapValue == 5), "The combination should include the item with scrap value 5.");
            Assert.IsTrue(combination.Exists(item => item.scrapValue == 4), "The combination should include the item with scrap value 4.");
        }
    }
}