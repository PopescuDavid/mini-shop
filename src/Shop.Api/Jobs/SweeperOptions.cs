namespace Shop.Api.Jobs;

public class SweeperOptions
{
    public const string SectionName = "Sweeper";

    public int IntervalSeconds { get; set; } = 60;
    public int DraftExpiryMinutes { get; set; } = 30;
}
