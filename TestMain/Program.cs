using TestMain.MusConvParsers.ServicesClients;

namespace Test;

public class Test
{
	public static void Main(string[] args)
	{
		var test = new ServiceClientParser();
		test.UpdateFiles();
	}
}