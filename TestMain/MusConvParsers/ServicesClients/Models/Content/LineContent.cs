using CSharpCodeAnalyzer.Helpers;
using TestMain.MusConvParsers.ServicesClients.Models.Base;

namespace TestMain.MusConvParsers.ServicesClients.Models.Content;

public class LineContent : ContentBase
{
	public override string Content => JoinManager.JoinLines(ContentByLines);

	public LineContent(string[] content)
	{
		ContentByLines = content;
	}
}