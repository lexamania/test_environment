using System.Collections.Generic;
using System.Linq;
using System.Text;
using CSharpCodeAnalyzer.Models.Base;
using CSharpCodeAnalyzer.Models.Elements;

namespace CSharpCodeAnalyzer.Helpers
{
	public static class JoinManager
	{
		public static string[] SplitByLines(this string value)
			=> value.Split("\n");

		public static string JoinLines(this string[] value)
			=> string.Join("\n", value.Where(x => x != null));

		public static string JoinLines(this string[] value, int start, int end)
			=> JoinLines(value[start..(end+1)]);

		public static string[] SplitByElements(this string value)
			=> value.Trim().Split(' ');

		public static string JoinElements(this List<CodeElementBase> elements)
		{
			var str = new StringBuilder();

			for(var i = 0; i < elements.Count; ++i)
			{
				if (i == 0) 
				{
					str.Append(elements[i].Content.Content);
					continue;
				}

				if (elements[i] is PropertyElement && elements[i-1] is PropertyElement)
				{
					str.Append($"\n{elements[i].Content.Content}");
				}
				else
				{
					str.Append($"\n\n{elements[i].Content.Content}");
				}
			}

			return str.ToString();
		}
	}
}