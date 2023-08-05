namespace TestMain.BinaryTree.Models
{
	public class TreeNode
	{
		public int Value { get; set; }
		public TreeNode LeftNode { get; set; }
		public TreeNode RightNode { get; set; }

		public static implicit operator TreeNode(int value)
			=> new() { Value = value };

		public override string ToString()
			=> $"{Value}";
	}
}