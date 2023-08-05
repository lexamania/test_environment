using TestMain.BinaryTree.Models;

namespace TestMain.BinaryTree;

public class BinaryTreeService
{
	private readonly int TreeNodeChance;
	private readonly int MinValue = 1;
	private readonly int MaxValue;

	private readonly Random InnerRandom = new Random(Random.Shared.Next());

	public TreeNode BaseNode { get; private set; }

	public BinaryTreeService(int minValue, int maxValue, int treeNodeChance)
	{
		MinValue = minValue;
		MaxValue = maxValue;
		TreeNodeChance = treeNodeChance;

		GenerateNodes();
	}

	public void GenerateNodes() => BaseNode = GetBinaryTree(TreeNodeChance);
	public void Print() => BinaryTreePrinter.Print(BaseNode);

	private TreeNode GetBinaryTree(int chance)
	{
		TreeNode baseNode = GenerateValue(MinValue, MaxValue);

		var isLeft = IsHit(chance);
		var isRight = IsHit(chance);

		if (isLeft) baseNode.LeftNode = GetBinaryTree(chance - 1);
		if (isRight) baseNode.RightNode = GetBinaryTree(chance - 1);

		return baseNode;
	}

	private bool IsHit(int chance)
		=> GenerateValue(0, 100) < chance;

	private int GenerateValue(int minValue, int maxValue)
		=> InnerRandom.Next(minValue, maxValue);
}