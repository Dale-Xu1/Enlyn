namespace Enlyn;

public class Map<K, V> : Dictionary<K, V> where K : notnull
{

    public Map<K, V>? Parent { get; internal set; }

    public Map(Map<K, V> parent) => Parent = parent;
    public Map() { }


    public new V this[K key]
    {
        get
        {
            if (ContainsKey(key)) return base[key];
            if (Parent is not null) return Parent[key];

            throw new EnlynError($"{key} not found");
        }
        set
        {
            if (ContainsKey(key)) throw new EnlynError($"Redefinition of {key}");
            base[key] = value;
        }
    }

    public bool Exists(K key) => ContainsKey(key) || (Parent?.Exists(key) ?? false);

}

public class MemberMap<K, V> : Map<K, V> where K : notnull where V : IMember
{

    private readonly Type type;

    public MemberMap(Type type) => this.type = type;


    public V Get(K key, Type current)
    {
        if (ContainsKey(key))
        {
            V value = this[key];
            switch (value.Access) // Public case falls through
            {
                case Access.Private when current != type: throw new EnlynError($"Member {key} is private");
                case Access.Protected: Environment.Test(type, current); break;
            }

            return value;
        }

        if (base.Parent is MemberMap<K, V> parent) return parent.Get(key, current);
        throw new EnlynError($"Member {key} not found");
    }

}

public class Type
{

    public TypeNode Name { get; init; }
    private Type? parent;

    public MemberMap<IdentifierNode, Field> Fields { get; }
    public MemberMap<IIdentifierNode, Method> Methods { get; }


    public Type(string name) : this() => Name = new TypeNode { Value = name };
    public Type()
    {
        Fields = new(this);
        Methods = new(this);
    }


    public Type? Parent
    {
        get => parent;
        internal set
        {
            parent = value;

            Fields.Parent = parent!.Fields;
            Methods.Parent = parent!.Methods;
        }
    }

}

public class Null : Type { public Null() : base("null") { } }
public class Option : Type
{

    public Type Type { get; init; } = null!;
    public Option() : base("option") { }

}

public interface IMember { public Access Access { get; } }
public class Field : IMember
{

    public Access Access { get; init; }
    public Type Type { get; init; } = null!;

}

public class Method : IMember
{

    public Access Access { get; init; }

    public Type[] Parameters { get; internal set; } = null!;
    public Type Return { get; init; } = null!;

}

internal static class Standard
{

    public static Type Unit { get; } = new("unit");
    public static Null Null { get; } = new();

    public static Type Any { get; } = new("any");
    public static Type Number { get; } = new("number") { Parent = Any };
    public static Type String { get; } = new("string") { Parent = Any };
    public static Type Boolean { get; } = new("boolean") { Parent = Any };

    private static readonly Type io = new("IO") { Parent = Any };

    public static Map<TypeNode, Type> Classes => new()
    {
        [Unit.Name   ] = Unit,
        [Any.Name    ] = Any,
        [Number.Name ] = Number,
        [String.Name ] = String,
        [Boolean.Name] = Boolean,

        [io.Name     ] = io
    };

    internal static Method equality = new()
    {
        Access = Access.Public,
        Parameters = new[] { Any },
        Return = Boolean
    };

    internal static Method arithmetic = new()
    {
        Access = Access.Public,
        Parameters = new[] { Number },
        Return = Number
    };

    internal static Method comparison = new()
    {
        Access = Access.Public,
        Parameters = new[] { Number },
        Return = Boolean
    };

    internal static Method logical = new()
    {
        Access = Access.Public,
        Parameters = new[] { Boolean },
        Return = Boolean
    };


    static Standard()
    {
        Any.Methods[Environment.constructor] = new Method
        {
            Access = Access.Public,
            Parameters = new Type[0], Return = Unit
        };
        Any.Methods[new BinaryIdentifierNode { Operation = Operation.Eq  }] = equality;
        Any.Methods[new BinaryIdentifierNode { Operation = Operation.Neq }] = equality;

        Number.Methods[new BinaryIdentifierNode { Operation = Operation.Add }] = arithmetic;
        Number.Methods[new BinaryIdentifierNode { Operation = Operation.Sub }] = arithmetic;
        Number.Methods[new BinaryIdentifierNode { Operation = Operation.Mul }] = arithmetic;
        Number.Methods[new BinaryIdentifierNode { Operation = Operation.Div }] = arithmetic;
        Number.Methods[new BinaryIdentifierNode { Operation = Operation.Mod }] = arithmetic;
        Number.Methods[new BinaryIdentifierNode { Operation = Operation.Lt  }] = comparison;
        Number.Methods[new BinaryIdentifierNode { Operation = Operation.Gt  }] = comparison;
        Number.Methods[new BinaryIdentifierNode { Operation = Operation.Le  }] = comparison;
        Number.Methods[new BinaryIdentifierNode { Operation = Operation.Ge  }] = comparison;
        Number.Methods[new UnaryIdentifierNode  { Operation = Operation.Neg }] = new Method
        {
            Access = Access.Public,
            Parameters = new Type[0], Return = Number
        };

        String.Fields[new IdentifierNode { Value = "length" }] = new Field { Access = Access.Public, Type = Number };
        String.Methods[new BinaryIdentifierNode { Operation = Operation.Add }] = new Method
        {
            Access = Access.Public,
            Parameters = new[] { String }, Return = String
        };

        Boolean.Methods[new BinaryIdentifierNode { Operation = Operation.And }] = logical;
        Boolean.Methods[new BinaryIdentifierNode { Operation = Operation.Or  }] = logical;
        Boolean.Methods[new UnaryIdentifierNode  { Operation = Operation.Not }] = new Method
        {
            Access = Access.Public,
            Parameters = new Type[0], Return = Boolean
        };

        io.Methods[new IdentifierNode { Value = "out" }] = new Method
        {
            Access = Access.Public,
            Parameters = new[] { new Option { Type = Any } },
            Return = Unit
        };
        io.Methods[new IdentifierNode { Value = "in" }] = new Method
        {
            Access = Access.Public,
            Parameters = new Type[0], Return = String
        };
    }

}

public class Environment
{

    public static readonly IdentifierNode constructor = new() { Value = "new" };

    public static void Test(Type expected, Type target)
    {
        if (expected is Option e) switch (target)
        {
            case Option t: Test(e.Type, t.Type); break;
            case Null: break;

            default: Test(e.Type, target); break;
        }
        else if (target is Option or Null) throw new EnlynError($"Type {expected.Name} is not an option");
        else if (!Check(expected, target))
            throw new EnlynError($"Type {target.Name} is not compatible with {expected.Name}");

        bool Check(Type expected, Type target)
        {
            if (expected == target) return true;
            if (target.Parent is null) return false;

            // Check if expected type is a parent of the target
            return Check(expected, target.Parent);
        }
    }

    public static void TestValue(Type type)
    {
        if (type == Standard.Unit || type == Standard.Number || type == Standard.String || type == Standard.Boolean)
            throw new EnlynError("Invalid value type");
    }


    private Map<IdentifierNode, Type> scope = new();
    public Map<TypeNode, Type> Classes { get; } = Standard.Classes;


    public Type this[IdentifierNode name]
    {
        get => scope[name];
        set => scope[name] = value;
    }

    public Type This { get; set; } = null!;
    public Type Return { get; set; } = null!;

    public void Enter() => scope = new Map<IdentifierNode, Type>(scope);
    public void Exit() => scope = scope.Parent!;

}

public abstract class EnvironmentVisitor<T> : ASTVisitor<T>
{

    public Environment Environment { get; }

    protected EnvironmentVisitor(Environment environment) => Environment = environment;

}

public class TypeChecker : EnvironmentVisitor<object?>
{

    private readonly ErrorLogger error;

    public TypeChecker(ErrorLogger error) : base(new Environment()) => this.error = error;


    private record struct ClassData(ClassNode node, Type type);
    public override object? Visit(ProgramNode program)
    {
        // Initialize type objects
        List<ClassData> nodes = new();
        foreach (ClassNode node in program.Classes) error.Catch(node.Location, () =>
        {
            Type type = new() { Name = node.Identifier };

            Environment.Classes[node.Identifier] = type;
            nodes.Add(new ClassData(node, type));
        });

        // Initialize parent graph and check for cycles
        foreach ((ClassNode node, Type type) in nodes) error.Catch(node.Location, () =>
        {
            Type parent = node.Parent is TypeNode name ? Environment.Classes[name] : Standard.Any;

            Environment.TestValue(parent);
            type.Parent = parent;
        });
        CheckCycles(program.Classes, from data in nodes select data.type);

        // Initialize and check members
        foreach ((ClassNode node, Type type) in nodes) foreach (IMemberNode member in node.Members)
            InitializeMember(type, (dynamic) member);
        foreach ((ClassNode node, Type type) in nodes)
        {
            Environment.Enter();
            Environment.This = type;

            foreach (IMemberNode member in node.Members) Visit(member);
            Environment.Exit();
        }

        // Check for Main class
        return error.Catch(program.Location, () =>
        {
            Type main = Environment.Classes[new TypeNode { Value = "Main" }];

            Method method = main.Methods[Environment.constructor];
            if (method.Parameters.Length > 0) throw new EnlynError("Main class constructor cannot have arguments");
        });
    }

    private void CheckCycles(ClassNode[] nodes, IEnumerable<Type> types)
    {
        // Perform depth-first search starting at each node
        List<Type> visited = new();
        foreach (Type type in types) Check(type, new List<Type>());

        void Check(Type type, List<Type> children)
        {
            if (visited.Contains(type)) return;
            visited.Add(type);
            children.Add(type);

            if (type.Parent is not Type parent) return;

            // Cycle is detected if parent has already been traversed
            if (!children.Contains(parent)) Check(parent, children);
            else foreach (ClassNode node in nodes) if (node.Identifier == parent.Name)
            {
                error.Report($"Cyclic inheritance found at {parent.Name}", node.Location);
                return;
            }
        }
    }

    private void InitializeMember(Type type, FieldNode node) => error.Catch(node.Location, () =>
        type.Fields[node.Identifier] = new Field
        {
            Access = node.Access,
            Type = new TypeVisitor(Environment).Visit(node.Type)
        });

    private void InitializeMember(Type type, MethodNode node) => error.Catch(node.Location, () =>
    {
        // Get return and parameter types
        Type returnType = node.Return switch  
        {
            ITypeNode typeNode => new TypeVisitor(Environment).Visit(typeNode),
            null => Standard.Unit // Defaults to unit if type is unspecified
        };

        // Check operator cases
        int length = node.Parameters.Length;
        IIdentifierNode identifier = node.Identifier switch
        {
            BinaryIdentifierNode when length != 1 =>
                throw new EnlynError("Binary operation must be defined with 1 parameter"),
            UnaryIdentifierNode when length != 0 =>
                throw new EnlynError("Unary operation cannot be defined with parameter"),

            IIdentifierNode node => node
        };

        type.Methods[identifier] = new Method
        {
            Access = node.Access,
            Parameters = InitializeParameters(node.Parameters),
            Return = returnType
        };
    });

    private void InitializeMember(Type type, ConstructorNode node) => error.Catch(node.Location, () =>
        type.Methods[Environment.constructor] = new Method
        {
            Access = node.Access,
            Parameters = InitializeParameters(node.Parameters),
            Return = Standard.Unit
        });

    private Type[] InitializeParameters(ParameterNode[] parameters)
    {
        TypeVisitor visitor = new TypeVisitor(Environment);
        IEnumerable<Type> types =
            from parameter in parameters
            select visitor.Visit(parameter.Type);

        return types.ToArray();
    }

    public override object? Visit(FieldNode node) => error.Catch(node.Location, () =>
    {
        Field field = Environment.This.Fields[node.Identifier];
        if (node.Expression is IExpressionNode expression)
        {
            Type type = new ExpressionVisitor(Environment).Visit(expression);
            Environment.Test(field.Type, type);
        }
    });

    public override object? Visit(MethodNode node) => error.Catch(node.Location, () =>
    {
        Method method = Environment.This.Methods[node.Identifier];
        CheckOverride(method, node);

        // Check control flow
        if (method.Return != Standard.Unit && !new ControlVisitor().Visit(node.Body))
            throw new EnlynError("Method does not always return a value");

        Environment.Enter();
        Environment.Return = method.Return;

        foreach (ParameterNode parameter in node.Parameters) Visit(parameter);
        Visit(node.Body);

        Environment.Exit();
    });

    private void CheckOverride(Method method, MethodNode node)
    {
        // Test if parent has a method by the same name
        Type parent = Environment.This.Parent!;
        if (!parent.Methods.Exists(node.Identifier))
        {
            if (node.Override) throw new EnlynError($"No method {node.Identifier} found to override");
            return;
        }

        // Check override rules
        if (!node.Override) throw new EnlynError($"Method {node.Identifier} must declare the override modifier");
        Method previous = parent.Methods[node.Identifier];

        if (method.Access > previous.Access)
            throw new EnlynError("Method cannot be less accessible than the overridden method");
        if (method.Parameters.Length != previous.Parameters.Length)
            throw new EnlynError("Invalid number of parameters");

        // Overridden parameters can be more generic
        foreach ((Type m, Type p) in method.Parameters.Zip(previous.Parameters)) Environment.Test(m, p);
        Environment.Test(previous.Return, method.Return); // Return type must be more specific
    }

    public override object? Visit(ConstructorNode node) => error.Catch(node.Location, () =>
    {
        Environment.Enter();
        Environment.Return = Standard.Unit;

        foreach (ParameterNode parameter in node.Parameters) Visit(parameter);

        // Check base call
        Type parent = Environment.This.Parent!;
        Method method = parent.Methods.Get(Environment.constructor, Environment.This);

        new ExpressionVisitor(Environment).CheckSignature(method, node.Arguments);
        Visit(node.Body);

        Environment.Exit();
    });

    public override object? Visit(ParameterNode node)
    {
        Environment[node.Identifier] = new TypeVisitor(Environment).Visit(node.Type);
        return null;
    }


    public override object? Visit(LetNode node) => error.Catch(node.Location, () =>
    {
        Type type = new ExpressionVisitor(Environment).Visit(node.Expression);
        if (node.Type is not null) // Use inferred type from expression if no type is declared
        {
            Type expected = new TypeVisitor(Environment).Visit(node.Type);
            Environment.Test(expected, type);

            type = expected;
        }

        // Infer type any? if expression is null
        if (type is Null) type = new Option { Type = Standard.Any };
        Environment[node.Identifier] = type;
    });

    public override object? Visit(IfNode node) => error.Catch(node.Location, () =>
    {
        // Condition must be a boolean
        Type type = new ExpressionVisitor(Environment).Visit(node.Condition);
        Environment.Test(Standard.Boolean, type);

        // Enter new scopes for branches
        Environment.Enter();
        Visit(node.Then);
        Environment.Exit();

        if (node.Else is not null)
        {
            Environment.Enter();
            Visit(node.Else);
            Environment.Exit();
        }
    });

    public override object? Visit(WhileNode node) => error.Catch(node.Location, () =>
    {
        // Condition must be a boolean
        Type type = new ExpressionVisitor(Environment).Visit(node.Condition);
        Environment.Test(Standard.Boolean, type);

        Environment.Enter();
        Visit(node.Body);
        Environment.Exit();
    });

    public override object? Visit(ReturnNode node) => error.Catch(node.Location, () =>
    {
        // Empty return value can only be used if method has no return type
        if (Environment.Return == Standard.Unit)
        {
            if (node.Expression is null) return;
            throw new EnlynError("Method cannot return a value");
        }
        else if (node.Expression is null) throw new EnlynError("Method cannot return unit");

        // Verify expression type
        Type type = new ExpressionVisitor(Environment).Visit(node.Expression);
        Environment.Test(Environment.Return, type);
    });

    public override object? Visit(BlockNode node)
    {
        foreach (IStatementNode statement in node.Statements) Visit(statement);
        return null;
    }

    public override object? Visit(ExpressionStatementNode node) => error.Catch(node.Location, () =>
        new ExpressionVisitor(Environment).Visit(node.Expression));

}

internal class ControlVisitor : ASTVisitor<bool>
{

    public override bool Visit(BlockNode node)
    {
        bool result = false;
        foreach (IStatementNode statement in node.Statements) result |= Visit(statement);

        return result;
    }

    public override bool Visit(IfNode node)
    {
        // Can't guarantee if statement will always return if no else branch is specified
        if (node.Else is null) return false;
        return Visit(node.Then) && Visit(node.Else);
    }

    public override bool Visit(ReturnNode _) => true;

}

internal class TypeVisitor : EnvironmentVisitor<Type>
{

    public TypeVisitor(Environment environment) : base(environment) { }


    public override Type Visit(TypeNode node) => Environment.Classes[node];
    public override Type Visit(OptionNode node) => new Option { Type = Visit(node.Type) };

}

internal class ExpressionVisitor : EnvironmentVisitor<Type>
{

    public ExpressionVisitor(Environment environment) : base(environment) { }


    public override Type Visit(AccessNode node)
    {
        Type type = Visit(node.Target);
        Field field = type.Fields.Get(node.Identifier, Environment.This);

        return field.Type;
    }

    public override Type Visit(CallNode node)
    {
        // Call target must be a method
        if (node.Target is not AccessNode target) throw new EnlynError("Invalid call target");

        Type type = Visit(target.Target);
        Method method = type.Methods.Get(target.Identifier, Environment.This);

        return CheckSignature(method, node.Arguments);
    }

    public override Type Visit(NewNode node)
    {
        Type type = Environment.Classes[node.Type];
        Environment.TestValue(type);

        Method method = type.Methods.Get(Environment.constructor, Environment.This);
        CheckSignature(method, node.Arguments);

        return type;
    }

    public Type CheckSignature(Method method, IExpressionNode[] arguments)
    {
        // Test if arguments match signature types
        if (method.Parameters.Length != arguments.Length) throw new EnlynError("Invalid number of arguments");
        foreach ((Type expected, IExpressionNode argument) in method.Parameters.Zip(arguments))
        {
            Type type = Visit(argument);
            Environment.Test(expected, type);
        }

        return method.Return;
    }

    public override Type Visit(AssertNode node) => Visit(node.Expression) switch
    {
        Option option => option.Type,
        _ => throw new EnlynError("Invalid assertion target")
    };

    public override Type Visit(AssignNode node)
    {
        Type expected = node.Target switch
        {
            IdentifierNode or AccessNode => Visit(node.Target)!,
            _ => throw new EnlynError("Invalid assignment target")
        };
        Type type = Visit(node.Expression);

        Environment.Test(expected, type);
        return expected;
    }

    public override Type Visit(InstanceNode node)
    {
        if (node.Type is null) Visit(node.Expression);
        else TestInstance(node.Expression, node.Type);

        return Standard.Boolean;
    }

    public override Type Visit(CastNode node) => TestInstance(node.Expression, node.Type);
    private Type TestInstance(IExpressionNode expression, ITypeNode type)
    {
        Type target = Visit(expression);
        Type result = new TypeVisitor(Environment).Visit(type);

        // Result should be more specific
        Environment.Test(target, result);
        return result;
    }

    public override Type Visit(BinaryNode node)
    {
        Type left = Visit(node.Left);
        BinaryIdentifierNode identifier = new() { Operation = node.Operation };

        Method method = left.Methods.Get(identifier, Environment.This);
        return CheckSignature(method, new IExpressionNode[] { node.Right });
    }

    public override Type Visit(UnaryNode node)
    {
        Type expression = Visit(node.Expression);
        UnaryIdentifierNode identifier = new() { Operation = node.Operation };

        Method method = expression.Methods.Get(identifier, Environment.This);
        return CheckSignature(method, new IExpressionNode[0]);
    }

    public override Type Visit(IdentifierNode node) => Environment[node];

    public override Type Visit(NumberNode node) => Standard.Number;
    public override Type Visit(StringNode node) => Standard.String;
    public override Type Visit(BooleanNode _) => Standard.Boolean;

    public override Type Visit(ThisNode _) => Environment.This;
    public override Type Visit(BaseNode _) => Environment.This.Parent!;
    public override Type Visit(NullNode _) => Standard.Null;

}
