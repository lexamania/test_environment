using System.Collections.Generic;
using CSharpCodeAnalyzer.Models.Base;

namespace CSharpCodeAnalyzer.Models.Elements
{
	public class ClassElement : CodeElementBase
	{
		public List<ConstructorElement> Constructors { get; set; } = new();
		public List<PropertyElement> Properties { get; set; } = new();
		public List<MethodElement> Methods { get; set; } = new();

		public List<CodeElementBase> Elements 
		{
			get
			{
				var result = new List<CodeElementBase>();
				result.AddRange(Properties);
				result.AddRange(Constructors);
				result.AddRange(Methods);
				return result;
			}
		}

		public string FileName { get; set; }
		public string FilePath { get; set; }
		public string[] FileLines { get; set; }
		public int ContentStart { get; set; }
		public int ContentEnd { get; set; }

		public override ElementType GetElementType()
			=> ElementType.Class;
	}
}