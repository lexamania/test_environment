namespace CSharpCodeAnalyzer.Helpers;

public static class StringExtenstions
{
	public static IEnumerable<int> EveryIndexOf(this string text, string value)
	{
		var lastIndex = -1;

		while (true)
		{
			lastIndex = text.IndexOf(value, lastIndex + 1);
			if (lastIndex == -1) break;
			yield return lastIndex;
		}
	}

	public static bool StartsWith(this string text, string[] values)
	{
		foreach (var value in values)
		{
			if (text.StartsWith(value))
				return true;
		}

		return false;
	}

	public static string ReadTo(this string text, string value)
	{
		var indx = text.IndexOf(value);
		if (indx == -1) return text;

		return text[..(indx)];
	}

	public static string ReadFrom(this string text, string value)
	{
		var indx = text.IndexOf(value);
		if (indx == -1) return text;

		return text[indx..];
	}
}