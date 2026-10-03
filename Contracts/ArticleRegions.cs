namespace Contracts;

public static class ArticleRegions
{
    private static readonly string[] Regions =
    [
        "Europe",
        "Asia",
        "Africa",
        "NorthAmerica",
        "SouthAmerica",
        "Oceania",
        "Antarctica",
        "Global"
    ];

    public static string Normalize(string value)
    {
        var region = Regions.FirstOrDefault(name =>
            string.Equals(
                name,
                value,
                StringComparison.OrdinalIgnoreCase));

        return region
               ?? throw new ArgumentException("Unknown region.");
    }
}