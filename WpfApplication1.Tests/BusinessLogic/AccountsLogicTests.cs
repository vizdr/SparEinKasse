using System.Collections.Generic;
using System.Xml.Linq;
using Xunit;
using FluentAssertions;
using WpfApplication1.BusinessLogic;
using WpfApplication1.DAL;
using WpfApplication1.Tests.Mocks;

namespace WpfApplication1.Tests.BusinessLogic
{
    public class AccountsLogicTests
    {
        [Fact]
        public void GetUserAccounts_WithTwoDistinctAccounts_ReturnsBoth()
        {
            // Arrange
            var testData = new XElement("Root",
                new XElement("Transaction", new XAttribute("Auftragskonto", "DE111")),
                new XElement("Transaction", new XAttribute("Auftragskonto", "DE222"))
            );
            var mockProvider = new MockDataSourceProvider(testData);
            var accountsLogic = new AccountsLogic(mockProvider);

            // Act
            var result = accountsLogic.GetUserAccounts();

            // Assert
            result.Should().HaveCount(2);
            result.Should().Contain("DE111");
            result.Should().Contain("DE222");
        }

        [Fact]
        public void GetUserAccounts_WithDuplicateAccounts_ReturnsDistinct()
        {
            // Arrange
            var testData = new XElement("Root",
                new XElement("Transaction", new XAttribute("Auftragskonto", "DE111")),
                new XElement("Transaction", new XAttribute("Auftragskonto", "DE222")),
                new XElement("Transaction", new XAttribute("Auftragskonto", "DE111")),
                new XElement("Transaction", new XAttribute("Auftragskonto", "DE111"))
            );
            var mockProvider = new MockDataSourceProvider(testData);
            var accountsLogic = new AccountsLogic(mockProvider);

            // Act
            var result = accountsLogic.GetUserAccounts();

            // Assert
            result.Should().HaveCount(2);
        }

        [Fact]
        public void GetUserAccounts_WithEmptyDataSource_ReturnsEmptyList()
        {
            // Arrange
            var mockProvider = new MockDataSourceProvider(new XElement("Root"));
            var accountsLogic = new AccountsLogic(mockProvider);

            // Act
            var result = accountsLogic.GetUserAccounts();

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void GetUserAccounts_WithNullDataSource_ReturnsEmptyList()
        {
            // Arrange
            var mockProvider = new MockDataSourceProvider(null);
            mockProvider.DataSource = null;
            var accountsLogic = new AccountsLogic(mockProvider);

            // Act
            var result = accountsLogic.GetUserAccounts();

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void GetUserAccounts_WithSingleAccount_ReturnsSingleItem()
        {
            // Arrange
            var testData = new XElement("Root",
                new XElement("Transaction", new XAttribute("Auftragskonto", "DE99999"))
            );
            var mockProvider = new MockDataSourceProvider(testData);
            var accountsLogic = new AccountsLogic(mockProvider);

            // Act
            var result = accountsLogic.GetUserAccounts();

            // Assert
            result.Should().ContainSingle()
                  .Which.Should().Be("DE99999");
        }
    }
}
