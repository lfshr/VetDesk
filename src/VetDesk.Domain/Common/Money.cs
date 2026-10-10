namespace VetDesk.Domain.Common;

public static class Money
{
    /// <summary>Rounds to whole pence, halves away from zero (the way HMRC and the till round).</summary>
    public static decimal Round(decimal amount) => Math.Round(amount, 2, MidpointRounding.AwayFromZero);
}
