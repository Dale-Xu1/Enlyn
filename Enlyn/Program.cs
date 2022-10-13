using Antlr4.Runtime;
using Enlyn;

string file = args[0];
using StreamReader reader = new(file);

AntlrInputStream stream = new(reader);
EnlynLexerFilter lexer = new(stream);

CommonTokenStream tokens = new(lexer);
EnlynParser parser = new(tokens);

ErrorLogger error = new();
parser.RemoveErrorListeners();
parser.AddErrorListener(new ErrorListener(error, file));

ParseTreeVisitor visitor = new(file);
ProgramNode tree = visitor.VisitProgram(parser.program());

TypeChecker checker = new(error);
checker.Visit(tree);

error.LogErrors();

Compiler compiler = new(checker.Environment, error);
Executable executable = compiler.Compile(tree);

VirtualMachine interpreter = new(executable);
interpreter.Run();
