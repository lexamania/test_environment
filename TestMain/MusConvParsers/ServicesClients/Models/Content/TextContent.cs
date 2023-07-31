using CSharpCodeAnalyzer.Helpers;
using TestMain.MusConvParsers.ServicesClients.Models.Base;

namespace TestMain.MusConvParsers.ServicesClients.Models.Content;

public class TextContent : ContentBase
{
	public override string[] ContentByLines => JoinManager.SplitByLines(Content);

	public TextContent(string content)
	{
		Content = content;
	}
}