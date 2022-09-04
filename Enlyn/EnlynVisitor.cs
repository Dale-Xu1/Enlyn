namespace Enlyn;

public class EnlynVisitor : EnlynBaseVisitor<object>
{

    public override object VisitExpr(EnlynParser.ExprContext context)
    {
        Console.WriteLine(context.GetText());
        VisitChildren(context);

        return null!;
    }

    public override object VisitLiteral(EnlynParser.LiteralContext context)
    {
        Console.WriteLine(context.GetText());
        return null!;
    }

}
