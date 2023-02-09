namespace SIL.DataAccess;

internal class FieldExpressionFlattener : System.Linq.Expressions.ExpressionVisitor
{
    private readonly Stack<Expression> _nodes = new();
    private readonly HashSet<Expression> _argExprs = new();

    public IEnumerable<Expression> Nodes => _nodes;

    protected override Expression VisitMember(MemberExpression node)
    {
        if (node.Member is PropertyInfo && !_argExprs.Contains(node))
            _nodes.Push(node);
        return base.VisitMember(node);
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        _argExprs.UnionWith(node.Arguments);
        _nodes.Push(node);
        return base.VisitMethodCall(node);
    }
}
