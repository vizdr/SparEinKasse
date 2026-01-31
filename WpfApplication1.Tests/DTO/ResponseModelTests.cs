using System;
using System.Collections.Generic;
using System.ComponentModel;
using Xunit;
using FluentAssertions;
using WpfApplication1;
using WpfApplication1.DTO;

namespace WpfApplication1.Tests.DTO
{
    public class ResponseModelTests
    {
        private FilterViewModel CreateFilterViewModel()
        {
            return new FilterViewModel();
        }

        [Fact]
        public void UpdateDataRequired_WhenChanged_ShouldFirePropertyChanged()
        {
            // Arrange
            var filterVM = CreateFilterViewModel();
            var responseModel = new ResponseModel(filterVM);
            string changedProperty = null;
            responseModel.PropertyChanged += (s, e) => changedProperty = e.PropertyName;

            // Act
            responseModel.UpdateDataRequired = !responseModel.UpdateDataRequired;

            // Assert
            changedProperty.Should().Be("UpdateDataRequired");
        }

        [Fact]
        public void ExpensesOverDateRange_WhenSet_ShouldFirePropertyChanged()
        {
            // Arrange
            var filterVM = CreateFilterViewModel();
            var responseModel = new ResponseModel(filterVM);
            string changedProperty = null;
            responseModel.PropertyChanged += (s, e) => changedProperty = e.PropertyName;

            var testData = new List<KeyValuePair<string, decimal>>
            {
                new KeyValuePair<string, decimal>("2024-01-15", 100.00m)
            };

            // Act
            responseModel.ExpensesOverDateRange = testData;

            // Assert
            changedProperty.Should().Be("ExpensesOverDateRange");
            responseModel.ExpensesOverDateRange.Should().BeSameAs(testData);
        }

        [Fact]
        public void BalanceOverDateRange_WhenSet_ShouldFirePropertyChanged()
        {
            // Arrange
            var filterVM = CreateFilterViewModel();
            var responseModel = new ResponseModel(filterVM);
            string changedProperty = null;
            responseModel.PropertyChanged += (s, e) => changedProperty = e.PropertyName;

            var testData = new List<KeyValuePair<DateTime, decimal>>
            {
                new KeyValuePair<DateTime, decimal>(new DateTime(2024, 1, 15), 500.00m)
            };

            // Act
            responseModel.BalanceOverDateRange = testData;

            // Assert
            changedProperty.Should().Be("BalanceOverDateRange");
            responseModel.BalanceOverDateRange.Should().BeSameAs(testData);
        }

        [Fact]
        public void Summary_WhenSet_ShouldFirePropertyChanged()
        {
            // Arrange
            var filterVM = CreateFilterViewModel();
            var responseModel = new ResponseModel(filterVM);
            string changedProperty = null;
            responseModel.PropertyChanged += (s, e) => changedProperty = e.PropertyName;

            // Act
            responseModel.Summary = "Total: 1000.00";

            // Assert
            changedProperty.Should().Be("Summary");
            responseModel.Summary.Should().Be("Total: 1000.00");
        }

        [Fact]
        public void ExpensesAtDate_WhenSet_ShouldFirePropertyChanged()
        {
            // Arrange
            var filterVM = CreateFilterViewModel();
            var responseModel = new ResponseModel(filterVM);
            string changedProperty = null;
            responseModel.PropertyChanged += (s, e) => changedProperty = e.PropertyName;

            var testData = new List<KeyValuePair<string, decimal>>
            {
                new KeyValuePair<string, decimal>("Shop A", 50.00m),
                new KeyValuePair<string, decimal>("Shop B", 75.00m)
            };

            // Act
            responseModel.ExpensesAtDate = testData;

            // Assert
            changedProperty.Should().Be("ExpensesAtDate");
            responseModel.ExpensesAtDate.Should().HaveCount(2);
        }

        [Fact]
        public void Dates4RemiteeOverDateRange_WhenSet_ShouldFirePropertyChanged()
        {
            // Arrange
            var filterVM = CreateFilterViewModel();
            var responseModel = new ResponseModel(filterVM);
            string changedProperty = null;
            responseModel.PropertyChanged += (s, e) => changedProperty = e.PropertyName;

            var testData = new List<KeyValuePair<string, decimal>>
            {
                new KeyValuePair<string, decimal>("2024-01-15", 150.00m)
            };

            // Act
            responseModel.Dates4RemiteeOverDateRange = testData;

            // Assert
            changedProperty.Should().Be("Dates4RemiteeOverDateRange");
        }

        [Fact]
        public void MultiplePropertyChanges_ShouldFireMultipleEvents()
        {
            // Arrange
            var filterVM = CreateFilterViewModel();
            var responseModel = new ResponseModel(filterVM);
            var changedProperties = new List<string>();
            responseModel.PropertyChanged += (s, e) => changedProperties.Add(e.PropertyName);

            // Act
            responseModel.Summary = "Test";
            responseModel.UpdateDataRequired = true;

            // Assert
            changedProperties.Should().Contain("Summary");
            changedProperties.Should().Contain("UpdateDataRequired");
        }

        [Fact]
        public void Constructor_WithNullFilterViewModel_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Action act = () => new ResponseModel(null);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_ShouldInitializeCollections()
        {
            // Arrange & Act
            var filterVM = CreateFilterViewModel();
            var responseModel = new ResponseModel(filterVM);

            // Assert
            responseModel.ExpensesOverDateRange.Should().NotBeNull();
            responseModel.BalanceOverDateRange.Should().NotBeNull();
            responseModel.IncomesOverDatesRange.Should().NotBeNull();
            responseModel.TransactionsAccounts.Should().NotBeNull();
            responseModel.Summary.Should().NotBeNull();
        }

        [Fact]
        public void ExpensesOverCategory_WhenSet_ShouldFirePropertyChanged()
        {
            // Arrange
            var filterVM = CreateFilterViewModel();
            var responseModel = new ResponseModel(filterVM);
            string changedProperty = null;
            responseModel.PropertyChanged += (s, e) => changedProperty = e.PropertyName;

            var testData = new List<KeyValuePair<string, decimal>>
            {
                new KeyValuePair<string, decimal>("Groceries", 250.00m),
                new KeyValuePair<string, decimal>("Transport", 100.00m)
            };

            // Act
            responseModel.ExpensesOverCategory = testData;

            // Assert
            changedProperty.Should().Be("ExpensesOverCategory");
            responseModel.ExpensesOverCategory.Should().HaveCount(2);
        }
    }
}
