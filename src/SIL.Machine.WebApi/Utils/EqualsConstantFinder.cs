namespace SIL.Machine.WebApi.Utils;

internal class EqualsConstantFinder<T, V> : System.Linq.Expressions.ExpressionVisitor
{
	private readonly MemberInfo _member;

	public EqualsConstantFinder(Expression<Func<T, V>> selector)
	{
		var memberExpr = (MemberExpression)selector.Body;
		_member = memberExpr.Member;
	}

	public V? Value { get; private set; }

	protected override Expression VisitBinary(BinaryExpression node)
	{
		if (node.Method?.Name == "op_Equality" && node.Left is MemberExpression memberExpr
			&& memberExpr.Member.Name == _member.Name)
		{
			Value = (V?)ExpressionHelper.FindConstantValue(node.Right);
		}
		return base.VisitBinary(node);
	}
}
