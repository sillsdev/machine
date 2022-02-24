namespace SIL.Machine.WebApi.Utils;

internal class ConstantFinder : System.Linq.Expressions.ExpressionVisitor
{
	private readonly Stack<MemberExpression> _nodes = new();

	public object? Value { get; private set; }

	protected override Expression VisitMember(MemberExpression node)
	{
		_nodes.Push(node);
		return base.VisitMember(node);
	}

	protected override Expression VisitConstant(ConstantExpression node)
	{
		object? val = node.Value;
		while (_nodes.Count > 0)
		{
			MemberExpression prevNode = _nodes.Peek();
			var fieldInfo = prevNode.Member as FieldInfo;
			if (fieldInfo != null)
				val = fieldInfo.GetValue(val);
			var propertyInfo = prevNode.Member as PropertyInfo;
			if (propertyInfo != null)
				val = propertyInfo.GetValue(val);
			_nodes.Pop();
		}
		Value = val;
		return base.VisitConstant(node);
	}
}
