namespace CSharpCodeAnalyzer.Helpers;

public static class EnumerableExtension
{
	public static IEnumerable<(int Index, T Value)> GetWithIndex<T>(this IEnumerable<T> list)
		=> list.Select((x, i) => (i, x));
}