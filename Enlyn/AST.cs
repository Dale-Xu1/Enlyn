namespace Enlyn;

public interface INode { }
public class ProgramNode : INode { }

public enum Access { Public, Protected, Private }
public abstract class MemberNode : INode { public Access Access { get; set; } }

public interface IStatementNode : INode { }

public interface IExpressionNode : INode { }
public abstract class LiteralNode<T> : IExpressionNode { public T Value { get; set; } = default!; }

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

    public IExpressionNode Left { get; set; } = default!;
    public IExpressionNode Right { get; set; } = default!;
}

public class UnaryNode : IExpressionNode
{
    public Operation Operation { get; set; }
    public IExpressionNode Expression { get; set; } = default!;
}

public class IdentifierNode : LiteralNode<string> { }
public class NumberNode : LiteralNode<double> { }
public class StringNode : LiteralNode<string> { }
public class BooleanNode : LiteralNode<bool> { }
public class NullNode : IExpressionNode { }

public class ParseTreeVisitor : EnlynBaseVisitor<INode>
{

    public override INode VisitProgram(EnlynParser.ProgramContext context)
    {
        return Visit(context.expr());
        // return new ProgramNode { };
    }

    public override INode VisitBinary(EnlynParser.BinaryContext context) =>
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
                EnlynLexer.NEQUAL  => Operation.Or,
                EnlynLexer.LESS    => Operation.Or,
                EnlynLexer.GREATER => Operation.Or,
                EnlynLexer.LEQUAL  => Operation.Or,
                EnlynLexer.GEQUAL  => Operation.Or,

                _ => throw new Exception("Invalid binary operation")
            },

            Left = (IExpressionNode) Visit(context.left),
            Right = (IExpressionNode) Visit(context.right)
        };

    public override INode VisitUnary(EnlynParser.UnaryContext context) =>
        new UnaryNode
        {
            Operation = context.op.Type switch
            {
                EnlynLexer.MINUS   => Operation.Neg,
                EnlynLexer.EXCLAIM => Operation.Not,
                _ => throw new Exception("Invalid unary operation")
            },
            Expression = (IExpressionNode) Visit(context.expr())
        };

    public override INode VisitGroup(EnlynParser.GroupContext context) => Visit(context.expr());

    public override INode VisitIdentifier(EnlynParser.IdentifierContext context) =>
        new IdentifierNode { Value = context.value.Text };

    public override INode VisitNumber(EnlynParser.NumberContext context) =>
        new NumberNode { Value = double.Parse(context.value.Text) };

    public override INode VisitString(EnlynParser.StringContext context)
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

    public override INode VisitBoolean(EnlynParser.BooleanContext context) =>
        new BooleanNode { Value = bool.Parse(context.value.Text) };

    public override INode VisitNull(EnlynParser.NullContext context) => new NullNode();

}

public abstract class ASTVisitor<T>
{

    public virtual T Visit(IdentifierNode node) => default!;
    public virtual T Visit(NumberNode node) => default!;
    public virtual T Visit(StringNode node) => default!;
    public virtual T Visit(BooleanNode node) => default!;

    public T Visit(INode node) => Visit((dynamic) node);
    
}
