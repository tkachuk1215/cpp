namespace Core.Reports;

public static class EnumerableExtensions
{
    // Топ-N: основний ключ за спаданням + другий унікальний ключ для стабільного порядку
    public static IEnumerable<T> TopBy<T>(
        this IEnumerable<T> source, Func<T, int> key, int n, Func<T, string> tieBreaker)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(tieBreaker);

        return source.OrderByDescending(key)
                     .ThenBy(tieBreaker, StringComparer.Ordinal)
                     .Take(n);
    }
}
