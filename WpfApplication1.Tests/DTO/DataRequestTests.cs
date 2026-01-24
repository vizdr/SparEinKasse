using System;
using Xunit;
using FluentAssertions;
using WpfApplication1.DTO;

namespace WpfApplication1.Tests.DTO
{
    public class DataRequestTests
    {
        [Fact]
        public void TimeSpan_WhenChanged_ShouldFireDataRequestedEvent()
        {
            // Arrange
            var filterVM = new FilterViewModel();
            var dataRequest = new DataRequest(filterVM);
            bool eventFired = false;
            dataRequest.DataRequested += (s, e) => eventFired = true;

            var newTimeSpan = new Tuple<DateTime, DateTime>(
                DateTime.Now.AddDays(-60),
                DateTime.Now.AddDays(-30));

            // Act
            dataRequest.TimeSpan = newTimeSpan;

            // Assert
            eventFired.Should().BeTrue();
        }

        [Fact]
        public void TimeSpan_WhenSetToSameValue_ShouldNotFireEvent()
        {
            // Arrange
            var filterVM = new FilterViewModel();
            var dataRequest = new DataRequest(filterVM);

            // Set initial value
            var initialTimeSpan = dataRequest.TimeSpan;

            bool eventFired = false;
            dataRequest.DataRequested += (s, e) => eventFired = true;

            // Act - set to same value
            dataRequest.TimeSpan = initialTimeSpan;

            // Assert
            eventFired.Should().BeFalse();
        }

        [Fact]
        public void DataBankUpdating_WhenSet_ShouldFireDataBankUpdateRequestedEvent()
        {
            // Arrange
            var filterVM = new FilterViewModel();
            var dataRequest = new DataRequest(filterVM);
            bool eventFired = false;
            dataRequest.DataBankUpdateRequested += (s, e) => eventFired = true;

            // Act
            dataRequest.DataBankUpdating = true;

            // Assert
            eventFired.Should().BeTrue();
        }

        [Fact]
        public void AtDate_WhenChanged_ShouldFireViewDataRequestedEvent()
        {
            // Arrange
            var filterVM = new FilterViewModel();
            var dataRequest = new DataRequest(filterVM);
            bool eventFired = false;
            dataRequest.ViewDataRequested += (s, e) => eventFired = true;

            // Act
            dataRequest.AtDate = new DateTime(2024, 6, 15);

            // Assert
            eventFired.Should().BeTrue();
        }

        [Fact]
        public void AtDate_WhenSetToSameValue_ShouldNotFireEvent()
        {
            // Arrange
            var filterVM = new FilterViewModel();
            var dataRequest = new DataRequest(filterVM);
            var testDate = new DateTime(2024, 6, 15);
            dataRequest.AtDate = testDate;

            bool eventFired = false;
            dataRequest.ViewDataRequested += (s, e) => eventFired = true;

            // Act - set to same value
            dataRequest.AtDate = testDate;

            // Assert
            eventFired.Should().BeFalse();
        }

        [Fact]
        public void SelectedRemittee_WhenChanged_ShouldFireViewDataRequestedEvent()
        {
            // Arrange
            var filterVM = new FilterViewModel();
            var dataRequest = new DataRequest(filterVM);
            bool eventFired = false;
            dataRequest.ViewDataRequested += (s, e) => eventFired = true;

            // Act
            dataRequest.SelectedRemittee = "Test Company GmbH";

            // Assert
            eventFired.Should().BeTrue();
        }

        [Fact]
        public void SelectedCategory_WhenChanged_ShouldFireViewDataRequestedEvent()
        {
            // Arrange
            var filterVM = new FilterViewModel();
            var dataRequest = new DataRequest(filterVM);
            bool eventFired = false;
            dataRequest.ViewDataRequested += (s, e) => eventFired = true;

            // Act
            dataRequest.SelectedCategory = "Groceries";

            // Assert
            eventFired.Should().BeTrue();
        }

        [Fact]
        public void Constructor_ShouldInitializeTimeSpanToLast30Days()
        {
            // Arrange & Act
            var filterVM = new FilterViewModel();
            var dataRequest = new DataRequest(filterVM);

            // Assert
            dataRequest.TimeSpan.Should().NotBeNull();
            dataRequest.TimeSpan.Item1.Should().BeCloseTo(DateTime.Now.Date.AddDays(-30), TimeSpan.FromSeconds(1));
            dataRequest.TimeSpan.Item2.Should().BeCloseTo(DateTime.Now.Date, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void Constructor_ShouldSetFiltersFromParameter()
        {
            // Arrange
            var filterVM = new FilterViewModel();

            // Act
            var dataRequest = new DataRequest(filterVM);

            // Assert
            dataRequest.Filters.Should().BeSameAs(filterVM);
        }

        [Fact]
        public void Constructor_WithNullFilterViewModel_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Action act = () => new DataRequest(null);
            act.Should().Throw<ArgumentNullException>();
        }
    }
}
