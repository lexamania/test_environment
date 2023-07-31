using CSharpCodeAnalyzer.Helpers;

namespace CSharpCodeAnalyzer.Models.Inners;

public class ContentPartModel
{
	public bool Success { get; set; } = false;

	public int StartLine { get; set; } = 0;
	public int StartIndex { get; set; } = 0;
	public int EndLine { get; set; } = 0;
	public int EndIndex { get; set; } = 0;

	public string[] FullContent { get; set; }
	public string[] Content => FullContent[StartLine..EndLine];


	public override string ToString()
	{
		if (!Success) return string.Empty;
		return JoinManager.JoinLines(Content);
	}
}