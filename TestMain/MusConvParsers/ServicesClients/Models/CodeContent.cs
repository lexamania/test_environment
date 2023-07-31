using CSharpCodeAnalyzer.Helpers;
using TestMain.MusConvParsers.ServicesClients.Models.Base;
using TestMain.MusConvParsers.ServicesClients.Models.Enum;

namespace TestMain.MusConvParsers.ServicesClients.Models;

public class CodeContent
{
	private ContentBase BaseContent { get; }

	public Dictionary<CodeElementType, Range> CodeElements { get; } = new();
	public Range ContentRange => CountContentRange();

	public CodeContent(ContentBase baseContent)
	{
		BaseContent = baseContent;
	}

	public string this[Range range] => GetContent(range);
	public string this[CodeElementType type] => this[CodeElements.GetValueOrDefault(type)];

	public override string ToString() => this[ContentRange];

	private string GetContent(Range range)
		=> JoinManager.JoinLines(BaseContent.ContentByLines[range]);

	private Range CountContentRange()
	{
		var min = CodeElements.Select(x => x.Value.Start).Min();
		var max = CodeElements.Select(x => x.Value.End).Max();
		return new Range(min, max);
	}
}

public static class CodeContentExtenstion
{
	public static CodeContent SetCodeElement(this CodeContent content, CodeElementType type, Range range)
	{
		content.CodeElements[type] = range;
		return content;
	}

	public static CodeContent RemoveCodeElement(this CodeContent content, CodeElementType type)
	{
		content.CodeElements.Remove(type);
		return content;
	}
}
