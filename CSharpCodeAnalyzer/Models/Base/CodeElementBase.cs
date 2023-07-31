using CSharpCodeAnalyzer.Models.Inners;

namespace CSharpCodeAnalyzer.Models.Base;

public class CodeElementBase
{
	public CodeElementContent Content { get; set; }
	public string Title { get; set; }
	public string ReturnType { get; set; }
	public string AccessModifier { get; set; }

	public virtual ElementType GetElementType()
		=> throw new NotImplementedException();
}