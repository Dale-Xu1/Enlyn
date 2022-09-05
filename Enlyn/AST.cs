namespace Enlyn;

public interface INode { }
public class ProgramNode : INode { }

public enum Access { Public, Protected, Private }
public abstract class MemberNode : INode { public Access Access { get; set; } }


public interface IStatementNode : INode { }


public interface IExpressionNode : INode { }

public class AccessNode : IExpressionNode
{
    public IExpressionNode Target { get; set; } = null!;
    public IdentifierNode Member { get; set; } = null!;
}

public class CallNode : IExpressionNode
{
    public IExpressionNode Target { get; set; } = null!;
    public IExpressionNode[] Arguments { get; set; } = null!;
}

public class NewNode : IExpressionNode
{
    public TypeNode Type { get; set; } = null!;
    public IExpressionNode[] Arguments { get; set; } = null!;
}

public class AssertNode : IExpressionNode { public IExpressionNode Expression { get; set; } = null!; }
public class AssignNode : IExpressionNode
{
    public IExpressionNode Target { get; set; } = null!;
    public IExpressionNode Expression { get; set; } = null!;
}

public enum Operation
{
    Add, Sub, Mul, Div, Mod,
    And, Or,
    Eq, Neq, Lt, Gt, Le, Ge,
    Neg, Not
}

public class BinaryNode : IExpressionNode
{
    public Operation Operation { get; set; }

    public IExpressionNode Left { get; set; } = null!;
    public IExpressionNode Right { get; set; } = null!;
}

public class UnaryNode : IExpressionNode
{
    public Operation Operation { get; set; }
    public IExpressionNode Expression { get; set; } = null!;
}

public abstract class LiteralNode<T> : IExpressionNode { public T Value { get; set; } = default!; }

public class IdentifierNode : LiteralNode<string> { }
public class TypeNode : LiteralNode<string> { }
public class NumberNode : LiteralNode<double> { }
public class StringNode : LiteralNode<string> { }
public class BooleanNode : LiteralNode<bool> { }
public class NullNode : IExpressionNode { }


public abstract class ASTVisitor<T>
{

    public virtual T Visit(ProgramNode node) => default!;

    public virtual T Visit(AccessNode node) => default!;
    public virtual T Visit(CallNode node) => default!;
    public virtual T Visit(NewNode node) => default!;
    public virtual T Visit(AssertNode node) => default!;
    public virtual T Visit(AssignNode node) => default!;

    public virtual T Visit(BinaryNode node) => default!;
    public virtual T Visit(UnaryNode node) => default!;

    public virtual T Visit(IdentifierNode node) => default!;
    public virtual T Visit(TypeNode node) => default!;
    public virtual T Visit(NumberNode node) => default!;
    public virtual T Visit(StringNode node) => default!;
    public virtual T Visit(BooleanNode node) => default!;
    public virtual T Visit(NullNode node) => default!;

    public T Visit(INode node) => Visit((dynamic) node);
    
}

public class ParseTreeVisitor : EnlynBaseVisitor<INode>
{

    public IExpressionNode VisitExpr(EnlynParser.ExprContext context) => (IExpressionNode) Visit(context);
    private new IExpressionNode[] VisitExprList(EnlynParser.ExprListContext context)
    {
        EnlynParser.ExprContext[] contexts = context.expr();
        IExpressionNode[] expressions = new IExpressionNode[contexts.Length];

        for (int i = 0; i < expressions.Length; i++)
        {
            EnlynParser.ExprContext expression = contexts[i];
            expressions[i] = VisitExpr(expression);
        }

        return expressions;
    }

    public override AccessNode VisitAccess(EnlynParser.AccessContext context) =>
        new AccessNode
        {
            Target = VisitExpr(context.expr()),
            Member = new IdentifierNode { Value = context.member.Text }
        };

    public override CallNode VisitCall(EnlynParser.CallContext context) =>
        new CallNode
        {
            Target = VisitExpr(context.expr()),
            Arguments = VisitExprList(context.arguments)
        };

    public override NewNode VisitNew(EnlynParser.NewContext context) =>
        new NewNode
        {
            Type = new TypeNode { Value = context.type.Text },
            Arguments = VisitExprList(context.arguments)
        };

    public override AssertNode VisitAssert(EnlynParser.AssertContext context) =>
        new AssertNode { Expression = VisitExpr(context.expr()) };

    public override AssignNode VisitAssign(EnlynParser.AssignContext context) =>
        new AssignNode
        {
            Target = VisitExpr(context.target),
            Expression = VisitExpr(context.value)
        };

    public override BinaryNode VisitBinary(EnlynParser.BinaryContext context) =>
        new BinaryNode
        {
            Operation = context.op.Type switch
            {
                EnlynLexer.PLUS    => Operation.Add,
                EnlynLexer.MINUS   => Operation.Sub,
                EnlynLexer.STAR    => Operation.Mul,
                EnlynLexer.SLASH   => Operation.Div,
                EnlynLexer.PERCENT => Operation.Mod,

                EnlynLexer.AND     => Operation.And,
                EnlynLexer.OR      => Operation.Or,
                
                EnlynLexer.DEQUAL  => Operation.Eq,
                EnlynLexer.NEQUAL  => Operation.Neq,
                EnlynLexer.LESS    => Operation.Lt,
                EnlynLexer.GREATER => Operation.Gt,
                EnlynLexer.LEQUAL  => Operation.Le,
                EnlynLexer.GEQUAL  => Operation.Ge,

                _ => throw new Exception("Invalid binary operation")
            },

            Left = VisitExpr(context.left),
            Right = VisitExpr(context.right)
        };

    public override UnaryNode VisitUnary(EnlynParser.UnaryContext context) =>
        new UnaryNode
        {
            Operation = context.op.Type switch
            {
                EnlynLexer.MINUS   => Operation.Neg,
                EnlynLexer.EXCLAIM => Operation.Not,
                _ => throw new Exception("Invalid unary operation")
            },
            Expression = VisitExpr(context.expr())
        };

    public override IExpressionNode VisitGroup(EnlynParser.GroupContext context) => VisitExpr(context.expr());

    public override IdentifierNode VisitIdentifier(EnlynParser.IdentifierContext context) =>
        new IdentifierNode { Value = context.value.Text };

    public override NumberNode VisitNumber(EnlynParser.NumberContext context) =>
        new NumberNode { Value = double.Parse(context.value.Text) };

    public override StringNode VisitString(EnlynParser.StringContext context)
    {
        string text = context.value.Text;
        text = text.Substring(1, text.Length - 2)
            .Replace("\\t", "\t") // Replace escape sequences with actual characters
            .Replace("\\n", "\n")
            .Replace("\\r", "\r")
            .Replace("\\b", "\b")
            .Replace("\\f", "\f")
            .Replace("\\\"", "\"")
            .Replace("\\\\", "\\");

        return new StringNode { Value = text };
    }

    public override BooleanNode VisitBoolean(EnlynParser.BooleanContext context) =>
        new BooleanNode { Value = bool.Parse(context.value.Text) };

    public override NullNode VisitNull(EnlynParser.NullContext context) => new NullNode();

}
