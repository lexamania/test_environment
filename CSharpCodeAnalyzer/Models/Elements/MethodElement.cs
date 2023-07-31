using CSharpCodeAnalyzer.Models.Base;

namespace CSharpCodeAnalyzer.Models.Elements;

public class MethodElement : CodeElementBase
{
	public override ElementType GetElementType()
		=> ElementType.Method;
}