namespace Enlyn;

public class Map<K, V> : Dictionary<K, V> where K : notnull
{

    public Map<K, V>? Parent { get; protected set; }

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

    public new MemberMap<K, V> Parent { set => base.Parent = value; }
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

public interface IType { }

public class Option : IType { public IType Type { get; init; } = null!; }
public class Type : IType
{

    public TypeNode Name { get; init; }

    private Type? parent;
    public Type? Parent
    {
        get => parent;
        internal set
        {
            parent = value;
            if (parent is null) return;

            Fields.Parent = parent.Fields;
            Methods.Parent = parent.Methods;
        }
    }

    public MemberMap<IdentifierNode, Field> Fields { get; }
    public MemberMap<IIdentifierNode, Method> Methods { get; }


    public Type(string name) : this() => Name = new TypeNode { Value = name };
    public Type()
    {
        Fields = new MemberMap<IdentifierNode, Field>(this);
        Methods = new MemberMap<IIdentifierNode, Method>(this);
    }

}

public interface IMember { public Access Access { get; } }
public class Field : IMember
{

    public Access Access { get; init; }
    public IType Type { get; init; } = null!;

}

public class Method : IMember
{

    public Access Access { get; init; }

    public IType[] Parameters { get; init; } = null!;
    public IType Return { get; init; } = null!;

}

internal static class Standard
{

    public static readonly Type unit = new("unit");

    public static readonly Type any = new("any");
    public static readonly Type number = new("number") { Parent = any };
    public static readonly Type strType = new("string") { Parent = any };
    public static readonly Type boolean = new("boolean") { Parent = any };

    static Standard()
    {
        Method equality = new() // TODO: Add operators to standard library
        {
            Access = Access.Public
        };

        any.Methods[Environment.constructor] = new Method
        {
            Access = Access.Public,
            Parameters = new IType[0], Return = unit
        };
    }


    public static Map<TypeNode, Type> Classes => new()
    {
        [unit.Name   ] = unit,
        [any.Name    ] = any,
        [number.Name ] = number,
        [strType.Name] = strType,
        [boolean.Name] = boolean
    };

}

public class Environment
{

    public static readonly IdentifierNode constructor = new() { Value = "new" };

    public static void Test(IType expected, IType? target)
    {
        if (expected is Option option) switch (target) // Null case falls through
        {
            case Option t: Test(option.Type, t.Type); break;
            case Type: Test(option.Type, target); break;
        }
        else if (expected is Type type) switch (target)
        {
            case Option or null: throw new EnlynError($"Type {type.Name} is not an option");
            case Type t:
            {
                if (Check(type, t)) break;
                throw new EnlynError($"Type {t.Name} is not compatible with {type.Name}");
            }
        }

        bool Check(Type expected, Type target)
        {
            if (expected == target) return true;
            if (target.Parent is null) return false;

            // Check if expected type is a parent of the target
            return Check(expected, target.Parent);
        }
    }


    private Map<IdentifierNode, IType> scope = new();
    public Map<TypeNode, Type> Classes { get; } = Standard.Classes;


    public IType this[IdentifierNode name]
    {
        get => scope[name];
        set => scope[name] = value;
    }

    public void Enter() => scope = new Map<IdentifierNode, IType>(scope);
    public void Exit() => scope = scope.Parent!;

}

public abstract class EnvironmentVisitor<T> : ASTVisitor<T>
{

    public Environment Environment { get; }

    protected EnvironmentVisitor(Environment environment) => Environment = environment;


    private static readonly IdentifierNode current = new() { Value = "this" };
    protected Type This
    {
        get => (Type) Environment[current];
        set => Environment[current] = value;
    }

    private static readonly IdentifierNode method = new() { Value = "return" };
    protected IType Return
    {
        get => Environment[method];
        set => Environment[method] = value;
    }

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
        foreach (ClassNode node in program.Classes)
        {
            Type type = new() { Name = node.Identifier };
            error.Catch(node.Location, () =>
            {
                Environment.Classes[node.Identifier] = type;
                nodes.Add(new ClassData(node, type));
            });
        }

        // Initialize parent graph and check for cycles
        foreach ((ClassNode node, Type type) in nodes) error.Catch(node.Location, () =>
        {
            Type parent = node.Parent is TypeNode name ? Environment.Classes[name] : Standard.any;
            type.Parent = parent;
        });
        CheckCycles(program.Classes, from data in nodes select data.type);

        // Initialize and check members
        foreach ((ClassNode node, Type type) in nodes) foreach (IMemberNode member in node.Members)
            InitializeMember(type, (dynamic) member);
        foreach ((ClassNode node, Type type) in nodes)
        {
            Environment.Enter();
            This = type;

            foreach (IMemberNode member in node.Members) Visit(member);
            Environment.Exit();
        }

        return null;
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
        IType returnType = node.Return switch  
        {
            ITypeNode typeNode => new TypeVisitor(Environment).Visit(typeNode),
            null => Standard.unit // Defaults to unit if type is unspecified
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
            Return = Standard.unit
        });

    private IType[] InitializeParameters(ParameterNode[] parameters)
    {
        TypeVisitor visitor = new TypeVisitor(Environment);
        IEnumerable<IType> types =
            from parameter in parameters
            select visitor.Visit(parameter.Type);

        return types.ToArray();
    }

    public override object? Visit(FieldNode node) => error.Catch(node.Location, () =>
    {
        Field field = This.Fields[node.Identifier];
        if (node.Expression is IExpressionNode expression)
        {
            IType? type = new ExpressionVisitor(Environment).Visit(expression);
            Environment.Test(field.Type, type);
        }
    });

    public override object? Visit(MethodNode node) => error.Catch(node.Location, () =>
    {
        Method method = This.Methods[node.Identifier];
        CheckOverride(method, node);

        // Check control flow
        if (method.Return != Standard.unit && !new ControlVisitor().Visit(node.Body))
            throw new EnlynError("Method does not always return a value");

        Environment.Enter();
        Return = method.Return;

        foreach (ParameterNode parameter in node.Parameters) Visit(parameter);
        Visit(node.Body);

        Environment.Exit();
    });

    private void CheckOverride(Method method, MethodNode node)
    {
        // Test if parent has a method by the same name
        Type parent = This.Parent!;
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
        foreach ((IType m, IType p) in method.Parameters.Zip(previous.Parameters)) Environment.Test(m, p);
        Environment.Test(previous.Return, method.Return); // Return type must be more specific
    }

    public override object? Visit(ConstructorNode node) => error.Catch(node.Location, () =>
    {
        Environment.Enter();
        Return = Standard.unit;

        foreach (ParameterNode parameter in node.Parameters) Visit(parameter);

        // Check base call
        Method parent = This.Parent!.Methods.Get(Environment.constructor, This);
        new ExpressionVisitor(Environment).CheckSignature(parent, node.Arguments);

        Visit(node.Body);
        Environment.Exit();
    });

    public override object? Visit(ParameterNode node)
    {
        Environment[node.Identifier] = new TypeVisitor(Environment).Visit(node.Type);
        return null;
    }


    // TODO: Statement checking
    public override object? Visit(ExpressionStatementNode node)
    {
        new ExpressionVisitor(Environment).Visit(node.Expression);
        return null;
    }

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

internal class TypeVisitor : EnvironmentVisitor<IType>
{

    public TypeVisitor(Environment environment) : base(environment) { }


    public override IType Visit(TypeNode node) => Environment.Classes[node];
    public override IType Visit(OptionNode node) => new Option { Type = Visit(node.Type) };

}

internal class ExpressionVisitor : EnvironmentVisitor<IType?>
{

    public ExpressionVisitor(Environment environment) : base(environment) { }


    public override IType Visit(AccessNode node)
    {
        if (Visit(node.Target) is not Type expression) throw new EnlynError("Cannot access from nullable type");

        Field field = expression.Fields.Get(node.Identifier, This);
        return field.Type;
    }

    public override IType Visit(CallNode node)
    {
        // Call target must be a method
        if (node.Target is not AccessNode target || Visit(target.Target) is not Type expression)
            throw new EnlynError("Invalid call target");

        Method method = expression.Methods.Get(target.Identifier, This);
        return CheckSignature(method, node.Arguments);
    }

    public override IType Visit(NewNode node)
    {
        Type type = Environment.Classes[node.Type];
        Method method = type.Methods.Get(Environment.constructor, This);

        return CheckSignature(method, node.Arguments);
    }

    public IType CheckSignature(Method method, IExpressionNode[] arguments)
    {
        // Test if arguments match signature types
        if (method.Parameters.Length != arguments.Length) throw new EnlynError("Invalid number of arguments");
        foreach ((IType expected, IExpressionNode argument) in method.Parameters.Zip(arguments))
        {
            IType? type = Visit(argument);
            Environment.Test(expected, type);
        }

        return method.Return;
    }

    public override IType Visit(AssertNode node) => Visit(node.Expression) switch
    {
        Option option => option.Type,
        _ => throw new EnlynError("Invalid assertion target")
    };

    public override IType Visit(AssignNode node)
    {
        IType expected = node.Target switch
        {
            IdentifierNode or AccessNode => Visit(node.Target)!,
            _ => throw new EnlynError("Invalid assignment target")
        };
        IType? type = Visit(node.Expression);

        Environment.Test(expected, type);
        return expected;
    }

    public override IType Visit(InstanceNode node)
    {
        TestInstance(node.Expression, node.Type);
        return Standard.boolean;
    }

    public override IType Visit(CastNode node) => TestInstance(node.Expression, node.Type);
    private IType TestInstance(IExpressionNode expression, ITypeNode type)
    {
        IType? target = Visit(expression);
        IType result = new TypeVisitor(Environment).Visit(type);

        if (target is null) throw new EnlynError("Null literal has no type");
        Environment.Test(target, result); // Result should be more specific

        return result;
    }

    public override IType Visit(BinaryNode node)
    {
        if (Visit(node.Left) is not Type left) throw new EnlynError("Left operand cannot be null");
        BinaryIdentifierNode identifier = new() { Operation = node.Operation };

        Method method = left.Methods.Get(identifier, This);
        return CheckSignature(method, new IExpressionNode[] { node.Right });
    }

    public override IType Visit(UnaryNode node)
    {
        if (Visit(node.Expression) is not Type expression) throw new EnlynError("Unary operand cannot be null");
        UnaryIdentifierNode identifier = new() { Operation = node.Operation };

        Method method = expression.Methods.Get(identifier, This);
        return CheckSignature(method, new IExpressionNode[0]);
    }

    public override IType Visit(IdentifierNode node) => Environment[node];

    public override IType Visit(NumberNode node) => Standard.number;
    public override IType Visit(StringNode node) => Standard.strType;
    public override IType Visit(BooleanNode _) => Standard.boolean;

    public override IType Visit(ThisNode _) => This;
    public override IType? Visit(BaseNode _) => This.Parent;
    public override IType? Visit(NullNode _) => null;

}
