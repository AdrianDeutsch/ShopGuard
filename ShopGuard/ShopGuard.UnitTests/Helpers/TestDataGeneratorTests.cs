using AwesomeAssertions;
using ShopGuard.Core.Helpers;

namespace ShopGuard.UnitTests.Helpers;

[TestFixture]
public sealed class TestDataGeneratorTests
{
    [Test]
    public void UniqueEmail_GeneratesDistinctAddresses()
    {
        var emails = Enumerable.Range(0, 50).Select(_ => TestDataGenerator.UniqueEmail()).ToList();

        emails.Should().OnlyHaveUniqueItems();
    }

    [Test]
    public void UniqueEmail_UsesReservedExampleDomain()
    {
        TestDataGenerator.UniqueEmail("checkout").Should()
            .StartWith("checkout.").And.EndWith("@example.com");
    }

    [Test]
    public void NewCustomer_GetsAUniqueEmailWithTheGivenPrefix()
    {
        var first = TestDataGenerator.NewCustomer("checkout");
        var second = TestDataGenerator.NewCustomer("checkout");

        first.Email.Should().StartWith("checkout.").And.EndWith("@example.com");
        second.Email.Should().NotBe(first.Email);
    }

    [Test]
    public void NewCustomer_HasACompleteAddressAndAPasswordTheShopAccepts()
    {
        var customer = TestDataGenerator.NewCustomer();

        customer.Address.Street.Should().NotBeNullOrWhiteSpace();
        customer.Address.HouseNumber.Should().NotBeNullOrWhiteSpace();
        customer.Address.PostalCode.Should().NotBeNullOrWhiteSpace();
        customer.Address.Country.Should().HaveLength(2, "the shop expects an ISO country code");
        // Shop rule: at least 8 characters with upper case, lower case, digit and symbol.
        customer.Password.Should().MatchRegex(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\w\s]).{8,}$");
    }

    [Test]
    public void DefaultBooking_HasCheckOutAfterCheckIn()
    {
        var booking = TestDataGenerator.DefaultBooking();

        booking.BookingDates.CheckOut.Should().BeAfter(booking.BookingDates.CheckIn);
        booking.BookingDates.CheckIn.Should().BeAfter(DateOnly.FromDateTime(DateTime.UtcNow));
    }

    [Test]
    public void DefaultBooking_UsesProvidedFirstName()
    {
        TestDataGenerator.DefaultBooking("Maria").FirstName.Should().Be("Maria");
    }

    [Test]
    public void DefaultBooking_HasPositivePrice()
    {
        TestDataGenerator.DefaultBooking().TotalPrice.Should().BePositive();
    }
}
