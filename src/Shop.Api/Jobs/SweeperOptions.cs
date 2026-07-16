using System.ComponentModel.DataAnnotations;

namespace Shop.Api.Jobs;

public class SweeperOptions
{
    public const string SectionName = "Sweeper";

    [Range(1, int.MaxValue)]
    public int IntervalSeconds { get; set; } = 60;

    [Range(1, int.MaxValue)]
    public int DraftExpiryMinutes { get; set; } = 30;
}
