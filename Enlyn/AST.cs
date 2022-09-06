namespace Enlyn;
using Antlr4.Runtime;

public interface INode { }
public class ProgramNode : INode { public ClassNode[] Classes { get; set; } = null!; }

public class ClassNode : INode
{
    public TypeNode Identifier { get; set; } = null!;
    public TypeNode? Parent { get; set; } = null;

    public MemberNode[] Members { get; set; } = null!;
}

public enum Access { Public, Protected, Private }
public abstract class MemberNode : INode { public Access Access { get; set; } }

public class FieldNode : MemberNode { }
public class MethodNode : MemberNode { }
public class ConstructorNode : MemberNode { }


public interface IStatementNode : INode { }

public class ExpressionStatementNode : IStatementNode { public IExpressionNode Expression { get; set; } = null!; }


public interface ITypeNode : INode { }
public interface IExpressionNode : INode { }

public class OptionNode : ITypeNode { public ITypeNode Type { get; set; } = null!; }
public class TypeNode : ITypeNode
{
    public string Value { get; set; } 

    public TypeNode(IToken token) => Value = token.Text;
}

public class AccessNode : IExpressionNode
{
    public IExpressionNode Target { get; set; } = null!;
    public IdentifierNode Identifier { get; set; } = null!;
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
public class IdentifierNode : LiteralNode<string>
{
    public IdentifierNode(IToken token) => Value = token.Text;
}

public class NumberNode : LiteralNode<double> { }
public class StringNode : LiteralNode<string> { }
public class BooleanNode : LiteralNode<bool> { }

public class ThisNode : IExpressionNode { }
public class BaseNode : IExpressionNode { }
public class NullNode : IExpressionNode { }


public abstract class ASTVisitor<T>
{

    public virtual T Visit(ProgramNode node) => default!;
    public virtual T Visit(ClassNode node) => default!;
    public virtual T Visit(FieldNode node) => default!;
    public virtual T Visit(MethodNode node) => default!;
    public virtual T Visit(ConstructorNode node) => default!;

    public virtual T Visit(ExpressionStatementNode node) => default!;

    public virtual T Visit(OptionNode node) => default!;
    public virtual T Visit(TypeNode node) => default!;

    public virtual T Visit(AccessNode node) => default!;
    public virtual T Visit(CallNode node) => default!;
    public virtual T Visit(NewNode node) => default!;
    public virtual T Visit(AssertNode node) => default!;
    public virtual T Visit(AssignNode node) => default!;
    public virtual T Visit(BinaryNode node) => default!;
    public virtual T Visit(UnaryNode node) => default!;
    public virtual T Visit(IdentifierNode node) => default!;
    public virtual T Visit(NumberNode node) => default!;
    public virtual T Visit(StringNode node) => default!;
    public virtual T Visit(BooleanNode node) => default!;
    public virtual T Visit(ThisNode node) => default!;
    public virtual T Visit(BaseNode node) => default!;
    public virtual T Visit(NullNode node) => default!;

    public T Visit(INode node) => Visit((dynamic) node);

}

public class ParseTreeVisitor : EnlynBaseVisitor<INode>
{

    private T[] VisitList<T>(ParserRuleContext[]? contexts) where T : INode
    {
        if (contexts is null) return new T[0];

        T[] nodes = new T[contexts.Length];
        for (int i = 0; i < nodes.Length; i++) nodes[i] = (T) Visit(contexts[i]);

        return nodes;
    }

    public override ProgramNode VisitProgram(EnlynParser.ProgramContext context) =>
        new() { Classes = VisitList<ClassNode>(context.classes?.classDefinition()) };

    public override ClassNode VisitClassDefinition(EnlynParser.ClassDefinitionContext context) => new()
    {
        Identifier = new TypeNode(context.id),
        Parent = context.parent is null ? null : new TypeNode(context.parent),

        Members = VisitList<MemberNode>(context.members?.member())
    };


    public IStatementNode VisitStmt(EnlynParser.StmtContext context) => (IStatementNode) Visit(context);
    
    public override ExpressionStatementNode VisitExprStmt(EnlynParser.ExprStmtContext context) =>
        new() { Expression = VisitExpr(context.expr()) };


    public IExpressionNode VisitExpr(EnlynParser.ExprContext context) => (IExpressionNode) Visit(context);
    public ITypeNode VisitTypeExpr(EnlynParser.TypeExprContext context) => (ITypeNode) Visit(context);

    public override OptionNode VisitOption(EnlynParser.OptionContext context) =>
        new() { Type = VisitTypeExpr(context.typeExpr()) };

    public override TypeNode VisitType(EnlynParser.TypeContext context) => new(context.value);

    public override AccessNode VisitAccess(EnlynParser.AccessContext context) => new()
    {
        Target = VisitExpr(context.expr()),
        Identifier = new IdentifierNode(context.id)
    };

    public override CallNode VisitCall(EnlynParser.CallContext context) => new()
    {
        Target = VisitExpr(context.expr()),
        Arguments = VisitList<IExpressionNode>(context.args?.expr())
    };

    public override NewNode VisitNew(EnlynParser.NewContext context) => new()
    {
        Type = new TypeNode(context.type),
        Arguments = VisitList<IExpressionNode>(context.args?.expr())
    };

    public override AssertNode VisitAssert(EnlynParser.AssertContext context) =>
        new() { Expression = VisitExpr(context.expr()) };

    public override AssignNode VisitAssign(EnlynParser.AssignContext context) => new()
    {
        Target = VisitExpr(context.target),
        Expression = VisitExpr(context.value)
    };

    public override BinaryNode VisitBinary(EnlynParser.BinaryContext context) => new()
    {
        // Convert token to operation enum
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

    public override UnaryNode VisitUnary(EnlynParser.UnaryContext context) => new()
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

    public override IdentifierNode VisitIdentifier(EnlynParser.IdentifierContext context) => new(context.value);
    public override NumberNode VisitNumber(EnlynParser.NumberContext context) =>
        new() { Value = double.Parse(context.value.Text) };

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
        new() { Value = bool.Parse(context.value.Text) };

    public override ThisNode VisitThis(EnlynParser.ThisContext _) => new();
    public override BaseNode VisitBase(EnlynParser.BaseContext _) => new();
    public override NullNode VisitNull(EnlynParser.NullContext _) => new();

}
