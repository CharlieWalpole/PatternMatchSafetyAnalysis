global using ClassName = string;
using System.Collections.Immutable;

namespace Analysis.Types;


public interface Type {}
public record struct Class(ClassName Name) : Type;
public record class Arrow(VarName[] Args, Environment Pre, Environment Post, ObjectSet Return);


public record struct AliasData(AliasFlag Flag);
public enum AliasFlag { S, M }


public interface ObjectInference {
    public record class Literal(ImmutableHashSet<AbstractObjID> Objects) : ObjectInference;
    public record class Var(int ID) : ObjectInference;

    public static ObjectInference Create(params AbstractObjID[] Objs) => new Literal([.. Objs]);
    public static ObjectInference Create(IEnumerable<AbstractObjID> Objs) => new Literal([.. Objs]);
    private static int currentID = 0;
    public static ObjectInference Create() => new Var(++currentID);
}
public interface TypeInference {
    public record class Literal(ImmutableHashSet<Type> Types) : TypeInference;
    public record class Var(int ID) : TypeInference;

    public static TypeInference Create(params Type[] Types) => new Literal([.. Types]);
    public static TypeInference Create(IEnumerable<Type> Types) => new Literal([.. Types]);
    private static int currentID = 0;
    public static TypeInference Create() => new Var(++currentID);
}
public interface AliasInference {
    public record class Literal(Alias Flag) : AliasInference;
    public record class Var(int ID) : AliasInference;

    public static AliasInference Create(Alias Flag) => new Literal(Flag);
    private static int currentID = 0;
    public static AliasInference Create() => new Var(++currentID);
}
