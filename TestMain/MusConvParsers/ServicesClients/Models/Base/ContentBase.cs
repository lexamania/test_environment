namespace TestMain.MusConvParsers.ServicesClients.Models.Base;

public abstract class ContentBase
{
	public virtual string Content { get; init; }
	public virtual string[] ContentByLines { get; init; }
}