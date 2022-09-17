namespace Enlyn;
using Antlr4.Runtime;

public record struct Location
{
    public string File { get; init; }

    public int Line { get; init; }
    public int Col { get; init; }
}

public interface INode { }
public abstract record LocationNode { public Location Location { get; init; } }

public record ProgramNode : INode { public ClassNode[] Classes { get; init; } = null!; }
public record ClassNode : LocationNode, INode
{
    public TypeNode Identifier { get; init; } = null!;
    public TypeNode? Parent { get; init; } = null;

    public IMemberNode[] Members { get; init; } = null!;
}

public enum Access { Public, Protected, Private }
public interface IMemberNode : INode { public Access Access { get; init; } }

public record FieldNode : LocationNode, IMemberNode
{
    public Access Access { get; init; }

    public IdentifierNode Identifier { get; init; } = null!;
    public ITypeNode Type { get; init; } = null!;

    public IExpressionNode? Expression { get; init; } = null;
}

public record MethodNode : LocationNode, IMemberNode
{
    public Access Access { get; init; }
    public bool Override { get; init; }

    public IIdentifierNode Identifier { get; init; } = null!;
    public ParameterNode[] Parameters { get; init; } = null!;
    public ITypeNode? Return { get; init; } = null;

    public IStatementNode Body { get; init; } = null!;
}

public record ConstructorNode : LocationNode, IMemberNode
{
    public Access Access { get; init; }

    public ParameterNode[] Parameters { get; init; } = null!;
    public IExpressionNode[] Arguments { get; init; } = null!;

    public IStatementNode Body { get; init; } = null!;
}

public record ParameterNode : LocationNode, INode
{
    public IIdentifierNode Identifier { get; init; } = null!;
    public ITypeNode Type { get; init; } = null!;
}


public interface IStatementNode : INode { }

public record LetNode : LocationNode, IStatementNode
{
    public IdentifierNode Identifier { get; init; } = null!;
    public ITypeNode? Type { get; init; } = null;

    public IExpressionNode Expression { get; init; } = null!;
}

public record IfNode : LocationNode, IStatementNode
{
    public IExpressionNode Condition { get; init; } = null!;

    public IStatementNode Then { get; init; } = null!;
    public IStatementNode? Else { get; init; } = null;
}

public record WhileNode : LocationNode, IStatementNode
{
    public IExpressionNode Condition { get; init; } = null!;
    public IStatementNode Body { get; init; } = null!;
}
    
public record ReturnNode : LocationNode, IStatementNode
{
    public IExpressionNode? Expression { get; init; } = null;
}

public record BlockNode : IStatementNode { public IStatementNode[] Statements { get; init; } = null!; }
public record ExpressionStatementNode : LocationNode, IStatementNode
{
    public IExpressionNode Expression { get; init; } = null!;
}


public interface ITypeNode : INode { }
public interface IExpressionNode : INode { }
public interface IIdentifierNode : INode { }

public record OptionNode : ITypeNode { public ITypeNode Type { get; init; } = null!; }
public record TypeNode : ITypeNode { public string Value { get; init; } = null!; }

public record AccessNode : IExpressionNode
{
    public IExpressionNode Target { get; init; } = null!;
    public IdentifierNode Identifier { get; init; } = null!;
}

public record CallNode : IExpressionNode
{
    public IExpressionNode Target { get; init; } = null!;
    public IExpressionNode[] Arguments { get; init; } = null!;
}

public record NewNode : IExpressionNode
{
    public TypeNode Type { get; init; } = null!;
    public IExpressionNode[] Arguments { get; init; } = null!;
}

public record AssertNode : IExpressionNode { public IExpressionNode Expression { get; init; } = null!; }
public record AssignNode : IExpressionNode
{
    public IExpressionNode Target { get; init; } = null!;
    public IExpressionNode Expression { get; init; } = null!;
}

public record InstanceNode : IExpressionNode
{
    public IExpressionNode Expression { get; init; } = null!;
    public ITypeNode Type { get; init; } = null!;
}

public record CastNode : IExpressionNode
{
    public IExpressionNode Expression { get; init; } = null!;
    public ITypeNode Type { get; init; } = null!;
}

public enum Operation
{
    Add, Sub, Mul, Div, Mod,
    And, Or,
    Eq, Neq, Lt, Gt, Le, Ge,
    Neg, Not
}

public record BinaryNode : IExpressionNode
{
    public Operation Operation { get; init; }

    public IExpressionNode Left { get; init; } = null!;
    public IExpressionNode Right { get; init; } = null!;
}

public record UnaryNode : IExpressionNode
{
    public Operation Operation { get; init; }
    public IExpressionNode Expression { get; init; } = null!;
}

public abstract record LiteralNode<T> : IExpressionNode { public T Value { get; init; } = default!; }
public record IdentifierNode : LiteralNode<string>, IIdentifierNode { }

public record BinaryIdentifierNode : IIdentifierNode { public Operation Operation { get; init; } }
public record UnaryIdentifierNode : IIdentifierNode { public Operation Operation { get; init; } }

public record NumberNode : LiteralNode<double> { }
public record StringNode : LiteralNode<string> { }
public record BooleanNode : LiteralNode<bool> { }

public record ThisNode : IExpressionNode { }
public record BaseNode : IExpressionNode { }
public record NullNode : IExpressionNode { }


public abstract class ASTVisitor<T>
{

    public virtual T Visit(ProgramNode node) => default!;
    public virtual T Visit(ClassNode node) => default!;
    public virtual T Visit(FieldNode node) => default!;
    public virtual T Visit(MethodNode node) => default!;
    public virtual T Visit(ConstructorNode node) => default!;
    public virtual T Visit(ParameterNode node) => default!;

    public virtual T Visit(LetNode node) => default!;
    public virtual T Visit(IfNode node) => default!;
    public virtual T Visit(WhileNode node) => default!;
    public virtual T Visit(ReturnNode node) => default!;
    public virtual T Visit(BlockNode node) => default!;
    public virtual T Visit(ExpressionStatementNode node) => default!;

    public virtual T Visit(OptionNode node) => default!;
    public virtual T Visit(TypeNode node) => default!;

    public virtual T Visit(AccessNode node) => default!;
    public virtual T Visit(CallNode node) => default!;
    public virtual T Visit(NewNode node) => default!;
    public virtual T Visit(AssertNode node) => default!;
    public virtual T Visit(AssignNode node) => default!;
    public virtual T Visit(InstanceNode node) => default!;
    public virtual T Visit(CastNode node) => default!;
    public virtual T Visit(BinaryNode node) => default!;
    public virtual T Visit(UnaryNode node) => default!;
    public virtual T Visit(IdentifierNode node) => default!;
    public virtual T Visit(BinaryIdentifierNode node) => default!;
    public virtual T Visit(UnaryIdentifierNode node) => default!;
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

    private readonly string file;

    public ParseTreeVisitor(string file) => this.file = file;


    private Location GetLocation(ParserRuleContext context)
    {
        IToken token = context.Start;
        return new()
        {
            File = file,
            Line = token.Line,
            Col = token.Column
        };
    }

    private T[] VisitList<T>(ParserRuleContext[]? contexts) where T : INode
    {
        if (contexts is null) return new T[0];

        T[] nodes = new T[contexts.Length];
        for (int i = 0; i < nodes.Length; i++) nodes[i] = (T) Visit(contexts[i]);

        return nodes;
    }


    public override ProgramNode VisitProgram(EnlynParser.ProgramContext context) =>
        new() { Classes = VisitList<ClassNode>(context.classList()?.classDefinition()) };

    public override ClassNode VisitClassDefinition(EnlynParser.ClassDefinitionContext context)
    {
        IToken? parent = context.parent;
        return new()
        {
            Identifier = new TypeNode { Value = context.id.Text },
            Parent = parent is null ? null : new TypeNode { Value = parent.Text },

            Members = VisitList<IMemberNode>(context.memberList()?.member()),
            Location = GetLocation(context)
        };
    }

    private Access VisitAccess(EnlynParser.VisibilityContext context) => context.access.Type switch
    {
        EnlynParser.PUBLIC => Access.Public,
        EnlynParser.PROTECTED => Access.Protected,
        EnlynParser.PRIVATE => Access.Private,

        _ => throw new Exception("Invalid access modifier")
    };

    public override FieldNode VisitField(EnlynParser.FieldContext context)
    {
        EnlynParser.ExprContext? expression = context.expr();
        return new()
        {
            Access = VisitAccess(context.visibility()),

            Identifier = new IdentifierNode { Value = context.id.Text },
            Type = VisitType(context.type()),

            Expression = expression is null ? null : VisitExpr(expression),
            Location = GetLocation(context)
        };
    }

    public override MethodNode VisitMethod(EnlynParser.MethodContext context)
    {
        EnlynParser.MethodNameContext name = context.methodName();
        IIdentifierNode identifier;

        if (name.BINARY() is not null) identifier = new BinaryIdentifierNode { Operation = MapBinary(name.op) };
        else if (name.UNARY() is not null) identifier = new UnaryIdentifierNode { Operation = MapUnary(name.op) };
        else identifier = new IdentifierNode { Value = name.id.Text };

        EnlynParser.TypeContext? type = context.type();
        return new()
        {
            Access = VisitAccess(context.visibility()),
            Override = context.OVERRIDE() is not null,

            Identifier = identifier,
            Parameters = VisitList<ParameterNode>(context.paramList()?.param()),
            Return = type is null ? null : VisitType(type),

            Body = VisitBlockStmt(context.block(), context.stmt()),
            Location = GetLocation(context)
        };
    }

    public override ConstructorNode VisitConstructor(EnlynParser.ConstructorContext context) => new()
    {
        Access = VisitAccess(context.visibility()),

        Arguments = VisitList<IExpressionNode>(context.exprList()?.expr()),
        Parameters = VisitList<ParameterNode>(context.paramList()?.param()),

        Body = VisitBlockStmt(context.block(), context.stmt()),
            Location = GetLocation(context)
    };

    public override ParameterNode VisitParam(EnlynParser.ParamContext context) => new()
    {
        Identifier = new IdentifierNode { Value = context.id.Text },
        Type = VisitType(context.type()),

        Location = GetLocation(context)
    };


    public IStatementNode VisitStmt(EnlynParser.StmtContext context) => (IStatementNode) Visit(context);
    private IStatementNode VisitBlockStmt(EnlynParser.BlockContext? a, EnlynParser.StmtContext? b) =>
        (IStatementNode) Visit((ParserRuleContext?) a ?? b);

    public override LetNode VisitLet(EnlynParser.LetContext context)
    {
        EnlynParser.TypeContext? type = context.type();
        return new()
        {
            Identifier = new IdentifierNode { Value = context.id.Text },
            Type = type is null ? null : VisitType(type),
            
            Expression = VisitExpr(context.expr()),
            Location = GetLocation(context)
        };
    }

    public override IfNode VisitIf(EnlynParser.IfContext context)
    {
        EnlynParser.ElseBranchContext? elseBranch = context.elseBranch();
        return new()
        {
            Condition = VisitExpr(context.expr()),

            Then = VisitBlockStmt(context.block(), context.stmt()),
            Else = elseBranch is null ? null : VisitBlockStmt(elseBranch.block(), elseBranch.stmt()),

            Location = GetLocation(context)
        };
    }

    public override WhileNode VisitWhile(EnlynParser.WhileContext context) => new()
    {
        Condition = VisitExpr(context.expr()),
        Body = VisitBlockStmt(context.block(), context.stmt()),

        Location = GetLocation(context)
    };

    public override ReturnNode VisitReturn(EnlynParser.ReturnContext context)
    {
        EnlynParser.ExprContext? expression = context.expr();
        return new()
        {
            Expression = expression is null ? null : VisitExpr(expression),
            Location = GetLocation(context)
        };
    }

    public override BlockNode VisitBlock(EnlynParser.BlockContext context) =>
        new() { Statements = VisitList<IStatementNode>(context.stmtList()?.stmt()) };

    public override ExpressionStatementNode VisitExprStmt(EnlynParser.ExprStmtContext context) => new()
    {
        Expression = VisitExpr(context.expr()),
        Location = GetLocation(context)
    };


    public IExpressionNode VisitExpr(EnlynParser.ExprContext context) => (IExpressionNode) Visit(context);
    public ITypeNode VisitType(EnlynParser.TypeContext context) => (ITypeNode) Visit(context);

    public override OptionNode VisitOption(EnlynParser.OptionContext context) =>
        new() { Type = VisitType(context.type()) };

    public override TypeNode VisitTypeIdentifier(EnlynParser.TypeIdentifierContext context) =>
        new() { Value = context.value.Text };

    public override AccessNode VisitAccess(EnlynParser.AccessContext context) => new()
    {
        Target = VisitExpr(context.expr()),
        Identifier = new IdentifierNode { Value = context.id.Text }
    };

    public override CallNode VisitCall(EnlynParser.CallContext context) => new()
    {
        Target = VisitExpr(context.expr()),
        Arguments = VisitList<IExpressionNode>(context.exprList()?.expr())
    };

    public override NewNode VisitNew(EnlynParser.NewContext context) => new()
    {
        Type = new TypeNode { Value = context.id.Text },
        Arguments = VisitList<IExpressionNode>(context.exprList()?.expr())
    };

    public override AssertNode VisitAssert(EnlynParser.AssertContext context) =>
        new() { Expression = VisitExpr(context.expr()) };

    public override AssignNode VisitAssign(EnlynParser.AssignContext context) => new()
    {
        Target = VisitExpr(context.target),
        Expression = VisitExpr(context.value)
    };

    public override InstanceNode VisitInstance(EnlynParser.InstanceContext context) => new()
    {
        Expression = VisitExpr(context.expr()),
        Type = VisitType(context.type())
    };

    public override CastNode VisitCast(EnlynParser.CastContext context) => new()
    {
        Expression = VisitExpr(context.expr()),
        Type = VisitType(context.type())
    };

    public override IExpressionNode VisitGroup(EnlynParser.GroupContext context) => VisitExpr(context.expr());

    public override BinaryNode VisitBinary(EnlynParser.BinaryContext context) => new()
    {
        Operation = MapBinary(context.op),
        Left = VisitExpr(context.left),
        Right = VisitExpr(context.right)
    };

    public override UnaryNode VisitUnary(EnlynParser.UnaryContext context) => new()
    {
        Operation = MapUnary(context.op),
        Expression = VisitExpr(context.expr())
    };

    private Operation MapBinary(IToken token) => token.Type switch // Convert token to operation enum
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
    };

    private Operation MapUnary(IToken token) => token.Type switch // Convert token to operation enum
    {
        EnlynLexer.MINUS   => Operation.Neg,
        EnlynLexer.EXCLAIM => Operation.Not,

        _ => throw new Exception("Invalid unary operation")
    };

    public override IdentifierNode VisitIdentifier(EnlynParser.IdentifierContext context) =>
        new() { Value = context.value.Text };

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
