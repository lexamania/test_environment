using TestMain.BinaryTree;

namespace Test;

public class Test
{
	public static void Main(string[] args)
	{
		var bts = new BinaryTreeService(1, 20, 60);
		string result = "yes";

		Console.SetBufferSize(600, 400);
		while (result != "no")
		{
			bts.GenerateNodes();
			Console.Clear();
			try
			{
				bts.Print();
			} catch { }

			Console.Write("Do you want to repeat?\nAnswer: ");
			result = Console.ReadLine();
		}
	}
}