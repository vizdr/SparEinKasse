using System.Collections.ObjectModel;
using Xunit;
using FluentAssertions;
using WpfApplication1;
using WpfApplication1.DTO;

namespace WpfApplication1.Tests.DTO
{
    public class FilterViewModelTests
    {
        [Fact]
        public void Constructor_ShouldInitializeCollections()
        {
            // Act
            var filterVM = new FilterViewModel();

            // Assert
            filterVM.UserAccounts.Should().NotBeNull();
            filterVM.BuchungstextValues.Should().NotBeNull();
        }

        [Fact]
        public void Constructor_ShouldInitializeEmptyStrings()
        {
            // Act
            var filterVM = new FilterViewModel();

            // Assert
            filterVM.ExpenciesLessThan.Should().BeEmpty();
            filterVM.ExpenciesMoreThan.Should().BeEmpty();
            filterVM.IncomesLessThan.Should().BeEmpty();
            filterVM.IncomesMoreThan.Should().BeEmpty();
            filterVM.ToFind.Should().BeEmpty();
        }

        [Fact]
        public void ExpenciesLessThan_WhenSet_ShouldReturnCorrectValue()
        {
            // Arrange
            var filterVM = new FilterViewModel();

            // Act
            filterVM.ExpenciesLessThan = "1000";

            // Assert
            filterVM.ExpenciesLessThan.Should().Be("1000");
        }

        [Fact]
        public void ExpenciesMoreThan_WhenSet_ShouldReturnCorrectValue()
        {
            // Arrange
            var filterVM = new FilterViewModel();

            // Act
            filterVM.ExpenciesMoreThan = "50";

            // Assert
            filterVM.ExpenciesMoreThan.Should().Be("50");
        }

        [Fact]
        public void IncomesLessThan_WhenSet_ShouldReturnCorrectValue()
        {
            // Arrange
            var filterVM = new FilterViewModel();

            // Act
            filterVM.IncomesLessThan = "5000";

            // Assert
            filterVM.IncomesLessThan.Should().Be("5000");
        }

        [Fact]
        public void IncomesMoreThan_WhenSet_ShouldReturnCorrectValue()
        {
            // Arrange
            var filterVM = new FilterViewModel();

            // Act
            filterVM.IncomesMoreThan = "100";

            // Assert
            filterVM.IncomesMoreThan.Should().Be("100");
        }

        [Fact]
        public void ToFind_WhenSet_ShouldReturnCorrectValue()
        {
            // Arrange
            var filterVM = new FilterViewModel();

            // Act
            filterVM.ToFind = "Grocery";

            // Assert
            filterVM.ToFind.Should().Be("Grocery");
        }

        [Fact]
        public void UserAccounts_CanAddItems()
        {
            // Arrange
            var filterVM = new FilterViewModel();
            var account = new BoolTextCouple(true, "DE123456");

            // Act
            filterVM.UserAccounts.Add(account);

            // Assert
            filterVM.UserAccounts.Should().ContainSingle();
            filterVM.UserAccounts[0].Text.Should().Be("DE123456");
            filterVM.UserAccounts[0].IsSelected.Should().BeTrue();
        }

        [Fact]
        public void BuchungstextValues_CanAddItems()
        {
            // Arrange
            var filterVM = new FilterViewModel();
            var buchungstext = new BoolTextCouple(false, "ÜBERWEISUNG");

            // Act
            filterVM.BuchungstextValues.Add(buchungstext);

            // Assert
            filterVM.BuchungstextValues.Should().ContainSingle();
            filterVM.BuchungstextValues[0].Text.Should().Be("ÜBERWEISUNG");
            filterVM.BuchungstextValues[0].IsSelected.Should().BeFalse();
        }

        [Fact]
        public void IsFilterPrepared_WhenEmpty_ShouldReturnFalse()
        {
            // Arrange
            var filterVM = new FilterViewModel();

            // Act & Assert
            filterVM.IsFilterPrepared().Should().BeFalse();
        }

        [Fact]
        public void IsFilterPrepared_WithUserAccounts_ShouldReturnTrue()
        {
            // Arrange
            var filterVM = new FilterViewModel();
            filterVM.UserAccounts.Add(new BoolTextCouple(true, "DE123"));

            // Act & Assert
            filterVM.IsFilterPrepared().Should().BeTrue();
        }

        [Fact]
        public void IsFilterPrepared_WithBuchungstextValues_ShouldReturnTrue()
        {
            // Arrange
            var filterVM = new FilterViewModel();
            filterVM.BuchungstextValues.Add(new BoolTextCouple(true, "LASTSCHRIFT"));

            // Act & Assert
            filterVM.IsFilterPrepared().Should().BeTrue();
        }

        [Fact]
        public void ResetFilterViewModel_ShouldClearAllValues()
        {
            // Arrange
            var filterVM = new FilterViewModel();
            filterVM.ExpenciesLessThan = "1000";
            filterVM.ExpenciesMoreThan = "50";
            filterVM.ToFind = "Test";
            filterVM.UserAccounts.Add(new BoolTextCouple(true, "DE123"));

            // Act
            filterVM.ResetFilterViewModel();

            // Assert
            filterVM.ExpenciesLessThan.Should().BeEmpty();
            filterVM.ExpenciesMoreThan.Should().BeEmpty();
            filterVM.ToFind.Should().BeEmpty();
            filterVM.UserAccounts.Should().BeEmpty();
            filterVM.BuchungstextValues.Should().BeEmpty();
        }

        [Fact]
        public void IsFilterDirty_InitialState_ShouldBeFalse()
        {
            // Arrange & Act
            var filterVM = new FilterViewModel();

            // Assert
            filterVM.IsFilterDirty().Should().BeFalse();
        }

        [Fact]
        public void FlopDirty_ShouldToggleDirtyState()
        {
            // Arrange
            var filterVM = new FilterViewModel();
            var initialState = filterVM.IsFilterDirty();

            // Act
            filterVM.FlopDirty();

            // Assert
            filterVM.IsFilterDirty().Should().Be(!initialState);
        }
    }
}
