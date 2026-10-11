
namespace VetDesk.Desktop.UnitTests;

public class ServerAddressTests
{
    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    [TestCase("vetdesk.local")]
    [TestCase("/vetdesk")]
    [TestCase("ftp://vetdesk.local")]
    [TestCase("file:///C:/VetDesk/index.html")]
    public void Parse_RejectsAnythingButAnAbsoluteWebAddress(string? setting) =>
        ServerAddress.Parse(setting).ShouldBeNull();

    [TestCase("https://vetdesk.local")]
    [TestCase(" https://vetdesk.local/ ")]
    [TestCase("http://localhost:5180")]
    public void Parse_AcceptsHttpAndHttps(string setting) =>
        ServerAddress.Parse(setting).ShouldNotBeNull();

    [TestCase("https://vetdesk.local/")]
    [TestCase("https://vetdesk.local/patients/42?tab=history")]
    [TestCase("https://VETDESK.local/billing")]
    [TestCase("https://vetdesk.local:443/today")]
    public void Contains_PagesOnTheServer(string address) =>
        ServerAddress.Parse("https://vetdesk.local")!.Contains(address).ShouldBeTrue();

    [TestCase("https://example.com/")]
    [TestCase("https://vetdesk.local.example.com/")]
    [TestCase("http://vetdesk.local/")]
    [TestCase("https://vetdesk.local:8443/")]
    [TestCase("about:blank")]
    [TestCase("not an address")]
    [TestCase(null)]
    public void Contains_NothingElse(string? address) =>
        ServerAddress.Parse("https://vetdesk.local")!.Contains(address).ShouldBeFalse();

    [TestCase("https://example.com", true)]
    [TestCase("http://localhost:5180/", true)]
    [TestCase("about:blank", false)]
    [TestCase("data:text/html,hello", false)]
    [TestCase("blob:https://vetdesk.local/1234", false)]
    [TestCase(null, false)]
    public void IsWebAddress_OnlyHttpAndHttps(string? address, bool expected) =>
        ServerAddress.IsWebAddress(address).ShouldBe(expected);
}
