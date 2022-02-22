namespace SIL.Machine.WebApi.Utils;

public static class ExpressionHelper
{
	public static Expression<Func<T, bool>> ChangePredicateType<T>(LambdaExpression predicate)
	{
		ParameterExpression param = Expression.Parameter(typeof(T), "x");
		var body = RebindParameter(param, predicate);
		return Expression.Lambda<Func<T, bool>>(body, param);
	}

	public static Expression RebindParameter(ParameterExpression param, LambdaExpression lambda)
	{
		var e = new ParameterRebinder(param);
		return e.Visit(lambda.Body);
	}

	public static IEnumerable<Expression> Flatten(LambdaExpression field)
	{
		var flattener = new FieldExpressionFlattener();
		flattener.Visit(field);
		return flattener.Nodes;
	}

	public static object FindConstantValue(Expression expression)
	{
		var finder = new ConstantFinder();
		finder.Visit(expression);
		return finder.Value;
	}
}
