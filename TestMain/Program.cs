using TestMain.MusConvParsers.ServicesClients;

namespace Test;

public class Test
{
	public static void Main(string[] args)
	{
		var parser = new ServiceClientParser();
		parser.UpdateFiles();
	}
}