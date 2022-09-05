using Antlr4.Runtime;
using Enlyn;

using StreamReader reader = new(args[0]);
AntlrInputStream stream = new(reader);

EnlynLexerFilter lexer = new(stream);

CommonTokenStream tokens = new(lexer);
EnlynParser parser = new(tokens);

EnlynParser.ExprContext context = parser.expr();
ParseTreeVisitor visitor = new();

IExpressionNode tree = visitor.VisitExpr(context);
