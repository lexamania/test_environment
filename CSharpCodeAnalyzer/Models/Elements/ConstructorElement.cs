using CSharpCodeAnalyzer.Models.Base;

namespace CSharpCodeAnalyzer.Models.Elements
{
	public class ConstructorElement : CodeElementBase
	{
		public override ElementType GetElementType()
			=> ElementType.Constructor;
	}
}