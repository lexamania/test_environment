using TestMain.MusConvParsers.ServicesClients.Models.Enum;

namespace TestMain.MusConvParsers.ServicesClients.Models.Elements;

public class UsingModel
{
	public string Path { get; set; }
	public string TempName { get; set; }
	public UsingModifier AccessModifier { get; set; }
}
