using CSharpCodeAnalyzer.Models.Base;

namespace CSharpCodeAnalyzer.Models.Elements
{
	public class PropertyElement : CodeElementBase
	{
		public override ElementType GetElementType()
			=> ElementType.Property;
	}
}