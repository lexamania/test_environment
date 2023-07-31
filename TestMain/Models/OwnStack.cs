namespace TestMain.Models;

public class OwnStack
{
	private Element<object> LastElement { get; set; }

	public void Push(object value)
	{
		var newelement = new Element<object>()
		{
			Value = value,
			PrevElement = LastElement
		};

		LastElement = newelement;
	}

	public object Pop()
	{
		if (LastElement == null) return "";

		var value = LastElement.Value;
		LastElement = LastElement.PrevElement;

		return value;
	}

	public class Element<TElement>
	{
		public TElement Value { get; set; }
		public Element<TElement> PrevElement { get; set; }
	}
}