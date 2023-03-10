namespace CSharpCodeAnalyzer.Models.Elements
{
	public class RegionContent
	{
		public string Title { get; set; }
		public string Content { get; set; }

		public RegionContent(string title, string content)
		{
			Title = title;
			Content = content;
		}
	}
}