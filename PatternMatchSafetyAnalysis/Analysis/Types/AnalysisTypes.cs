global using ClassName = string;
using System.Collections.Immutable;

namespace Analysis.Types;


public interface Type {}
public record struct Class(ClassName Name) : Type;
public record class Arrow(VarName[] Args, Environment Pre, Environment Post, ObjectSet Return) : Type;


public record struct AliasData(AliasFlag Flag);
public enum AliasFlag { S, M }

public interface InferenceVariable {}
public interface ObjectInference : InferenceVariable {
    public record class Literal(ImmutableHashSet<AbstractObjID> Objects) : ObjectInference;
    public record class Var(int ID) : ObjectInference;

    public static Literal Create(params AbstractObjID[] Objs) => new Literal([.. Objs]);
    public static Literal Create(IEnumerable<AbstractObjID> Objs) => new Literal([.. Objs]);
    private static int currentID = 0;
    public static Var Create() => new Var(++currentID);
    public static Literal Empty { get; } = new Literal([]);

    static InferenceConstraint.ObjectInclusion operator <=(ObjectInference l, ObjectInference r) => new InferenceConstraint.ObjectInclusion(l, r);
    static InferenceConstraint.ObjectInclusion operator >=(ObjectInference l, ObjectInference r) => new InferenceConstraint.ObjectInclusion(r, l);

    static InferenceConstraint.ObjectInclusion operator <=(ObjectInference l, AbstractObjID r) => new InferenceConstraint.ObjectInclusion(l, new Literal([r]));
    static InferenceConstraint.ObjectInclusion operator >=(ObjectInference l, AbstractObjID r) => new InferenceConstraint.ObjectInclusion(new Literal([r]), l);

    static InferenceConstraint.ObjectInclusion operator <=(AbstractObjID l, ObjectInference r) => new InferenceConstraint.ObjectInclusion(new Literal([l]), r);
    static InferenceConstraint.ObjectInclusion operator >=(AbstractObjID l, ObjectInference r) => new InferenceConstraint.ObjectInclusion(r, new Literal([l]));
}
public interface TypeInference : InferenceVariable {
    public record class Literal(ImmutableHashSet<Type> Types) : TypeInference;
    public record class Var(int ID) : TypeInference;

    public static TypeInference Create(params Type[] Types) => new Literal([.. Types]);
    public static TypeInference Create(IEnumerable<Type> Types) => new Literal([.. Types]);
    private static int currentID = 0;
    public static TypeInference.Var Create() => new Var(++currentID);

    static InferenceConstraint operator <=(TypeInference l, TypeInference r) => new InferenceConstraint.SubTyping(l, r);
    static InferenceConstraint operator >=(TypeInference l, TypeInference r) => new InferenceConstraint.SubTyping(r, l);

    static InferenceConstraint.SubTyping operator <=(TypeInference l, Type r) => new InferenceConstraint.SubTyping(l, new Literal([r]));
    static InferenceConstraint.SubTyping operator >=(TypeInference l, Type r) => new InferenceConstraint.SubTyping(new Literal([r]), l);

    static InferenceConstraint.SubTyping operator <=(Type l, TypeInference r) => new InferenceConstraint.SubTyping(new Literal([l]), r);
    static InferenceConstraint.SubTyping operator >=(Type l, TypeInference r) => new InferenceConstraint.SubTyping(r, new Literal([l]));
}
public interface AliasInference : InferenceVariable {
    public record class Literal(Alias Flag) : AliasInference;
    public record class Var(int ID) : AliasInference;

    public static AliasInference Create(Alias Flag) => new Literal(Flag);
    private static int currentID = 0;
    public static AliasInference Create() => new Var(++currentID);
    public static AliasInference Single { get; } = new Literal(new Alias(AliasFlag.S));
    public static AliasInference Multiple { get; } = new Literal(new Alias(AliasFlag.M));

    static InferenceConstraint operator <=(AliasInference l, AliasInference r) => new InferenceConstraint.AliasBounding(l, r);
    static InferenceConstraint operator >=(AliasInference l, AliasInference r) => new InferenceConstraint.AliasBounding(r, l);
}


public interface InferenceConstraint {
    InferenceConstraint Substitute(IDictionary<InferenceVariable, InferenceVariable> mapping);

    public record class ObjectInclusion(ObjectInference l, ObjectInference r) : InferenceConstraint {
        public InferenceConstraint Substitute(IDictionary<InferenceVariable, InferenceVariable> mapping) =>
            new ObjectInclusion((ObjectInference)mapping.GetOrDefault(l, l), (ObjectInference)mapping.GetOrDefault(r, r));
    }
    public record class AliasBounding(AliasInference l, AliasInference r) : InferenceConstraint {
        public InferenceConstraint Substitute(IDictionary<InferenceVariable, InferenceVariable> mapping) =>
            new AliasBounding((AliasInference)mapping.GetOrDefault(l, l), (AliasInference)mapping.GetOrDefault(r, r));
    }
    public record class SubTyping(TypeInference l, TypeInference r) : InferenceConstraint {
        public InferenceConstraint Substitute(IDictionary<InferenceVariable, InferenceVariable> mapping) =>
        new SubTyping((TypeInference)mapping.GetOrDefault(l, l), (TypeInference)mapping.GetOrDefault(l, l));
    }
    public record class HeapLookup(ObjectInference.Var Out, Environment Env, ObjectInference.Var Obj, FieldName Name) : InferenceConstraint {
        public InferenceConstraint Substitute(IDictionary<InferenceVariable, InferenceVariable> mapping) => 
            new HeapLookup((ObjectInference.Var)mapping.GetOrDefault(Out, Out), Env.Substitute(mapping), (ObjectInference.Var)mapping.GetOrDefault(Obj, Obj), Name);
    }

    public record class HeapUpdate(Environment Out, Environment In, ObjectInference.Var ObjIn, FieldName Name, ObjectInference ObjTo) : InferenceConstraint {
        public InferenceConstraint Substitute(IDictionary<InferenceVariable, InferenceVariable> mapping) =>
            new HeapUpdate(Out.Substitute(mapping), In.Substitute(mapping), (ObjectInference.Var)mapping.GetOrDefault(ObjIn, ObjIn), Name, (ObjectInference)mapping.GetOrDefault(ObjTo, ObjTo));
    }

    public record class TypeLookup(TypeInference.Var TypeOut, Environment Env, ObjectInference.Var Objs) : InferenceConstraint {
        public InferenceConstraint Substitute(IDictionary<InferenceVariable, InferenceVariable> mapping) =>
            new TypeLookup((TypeInference.Var)mapping.GetOrDefault(TypeOut, TypeOut), Env.Substitute(mapping), (ObjectInference.Var)mapping.GetOrDefault(Objs, Objs));
    }

    public record class Restriction(ObjectInference.Var Out, Environment Env, ObjectInference.Var In, Type Tau) : InferenceConstraint {
        public InferenceConstraint Substitute(IDictionary<InferenceVariable, InferenceVariable> mapping) =>
            new Restriction((ObjectInference.Var)mapping.GetOrDefault(Out, Out), Env.Substitute(mapping), (ObjectInference.Var)mapping.GetOrDefault(In, In), Tau);
    }

    public record class ApplicationResolution(
        Environment EnvOut, ObjectInference.Var ObjOut,
        TypeInference.Var TypeInternal, Environment EnvInternal,
        Environment EnvIn, ObjectInference.Var Funcs, ImmutableArray<ObjectInference.Var> Arguments
    ) : InferenceConstraint {
        public InferenceConstraint Substitute(IDictionary<InferenceVariable, InferenceVariable> mapping) =>
            new ApplicationResolution(
                EnvOut.Substitute(mapping), (ObjectInference.Var)mapping.GetOrDefault(ObjOut, ObjOut),
                (TypeInference.Var)mapping.GetOrDefault(TypeInternal, TypeInternal), EnvInternal.Substitute(mapping),
                EnvIn.Substitute(mapping), (ObjectInference.Var)mapping.GetOrDefault(Funcs, Funcs), [..Arguments.Select(a => (ObjectInference.Var)mapping.GetOrDefault(a, a))]
            );
    }

}

public record class AnalysisResult(ImmutableHashSet<InferenceConstraint> Constraints, ObjectInference Return, Environment EndEnv);

public record class MethodSummary(
    ImmutableHashSet<InferenceVariable> InferenceVariables, //Contains all inference variables involved in the analysis of a method; including 'ThisVariable' and 'MethodType'
    ObjectInference.Var ThisVariable,
    ImmutableHashSet<InferenceConstraint> Constraints,
    TypeInference MethodType);
