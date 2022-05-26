namespace SIL.Machine.WebApi.Utils;

internal class ParameterRebinder : System.Linq.Expressions.ExpressionVisitor
{
    private readonly ParameterExpression _parameter;

    public ParameterRebinder(ParameterExpression parameter)
    {
        _parameter = parameter;
    }

    protected override Expression VisitParameter(ParameterExpression p)
    {
        return base.VisitParameter(_parameter);
    }
}
