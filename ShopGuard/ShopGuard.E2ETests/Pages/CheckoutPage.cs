using Microsoft.Playwright;

namespace ShopGuard.E2ETests.Pages;

/// <summary>
/// Page object for steps 2-4 of the Toolshop checkout wizard:
/// sign-in confirmation, billing address and payment.
/// </summary>
public sealed class CheckoutPage(IPage page) : BasePage(page)
{
    private ILocator ProceedFromSignInButton => Page.Locator("[data-test='proceed-2']");
    private ILocator ProceedFromAddressButton => Page.Locator("[data-test='proceed-3']");
    private ILocator PaymentMethodSelect => Page.Locator("[data-test='payment-method']");
    private ILocator FinishButton => Page.Locator("[data-test='finish']");
    private ILocator PaymentSuccessMessage => Page.Locator("[data-test='payment-success-message']");
    private ILocator ConfirmationText => Page.GetByText("Thanks for your order!");

    /// <summary>Confirms the sign-in step (customer is already logged in).</summary>
    public async Task ConfirmSignedInAsync()
    {
        await ProceedFromSignInButton.WaitForAsync();
        await ProceedFromSignInButton.ClickAsync();
    }

    /// <summary>
    /// Confirms the billing address step. The form is prefilled from the customer profile and
    /// nothing is typed here on purpose: the button stays disabled while a required field is
    /// empty, so an incomplete prefill fails the scenario instead of being repaired silently.
    /// </summary>
    public Task ConfirmBillingAddressAsync() => ProceedFromAddressButton.ClickAsync();

    /// <summary>Pays via "Cash on Delivery" so no real payment data is required.</summary>
    public async Task PayWithCashOnDeliveryAsync()
    {
        await PaymentMethodSelect.SelectOptionAsync(new SelectOptionValue { Value = "cash-on-delivery" });
        await FinishButton.ClickAsync();
        await PaymentSuccessMessage.First.WaitForAsync();

        // After successful payment the same button turns into "Confirm" and places the order;
        // wait for the relabel before clicking, otherwise the click hits the old handler.
        var confirmButton = FinishButton.Filter(new LocatorFilterOptions { HasTextString = "Confirm" });
        await confirmButton.WaitForAsync();
        await confirmButton.ClickAsync();
        await ConfirmationText.WaitForAsync(new LocatorWaitForOptions { Timeout = 60_000 });
    }

    /// <summary>Extracts the invoice number (e.g. "INV-2026000018") from the confirmation.</summary>
    public async Task<string> GetInvoiceNumberAsync()
    {
        var confirmation = await Page.Locator("#order-confirmation, app-root").First.InnerTextAsync();
        var match = System.Text.RegularExpressions.Regex.Match(confirmation, @"INV-\d+");
        return match.Success ? match.Value : string.Empty;
    }
}
