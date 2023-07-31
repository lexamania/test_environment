namespace Test.MusConv.Models;

public class ParserModel
{
	public const string SourceFolderPath = "G:/Job/musconv/musconv/MusConv.ViewModels/Models";
	public const string DestinationFolderPath = "./MusConv/Mappers";

	public string SourceClassName { get; set; }
	public string SourceFileName => $"{SourceClassName}.cs";
	public string SourceFilePath => $"{SourceFolderPath}/{SourceFileName}";
	public string DestinationClassName { get; set; }
	public string DestinationFileName => $"{DestinationClassName}.cs";
	public string DestinationFilePath => $"{DestinationFolderPath}/{DestinationFileName}";
}