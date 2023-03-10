using CSharpCodeAnalyzer.Helpers;

namespace CSharpCodeAnalyzer.Models.Inners
{
	public class CodeElementContent
	{
		public string Content { get; set; } = string.Empty;
		public int StartLine { get; set; } = 0;
		public int EndLine { get; set; } = 0;

		public string Comment { get; set; } = string.Empty;
		public string Attributes { get; set; } = string.Empty;
		public string Header { get; set; } = string.Empty;
		public string Body { get; set; } = string.Empty;

		public override string ToString()
		{
			var lines = new string[] { Comment, Attributes, Header, Body };
			return JoinManager.JoinLines(lines);
		}
	}
}