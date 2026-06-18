global using ClassName = string;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;

namespace Analysis.Types;


public interface Type {
    public static bool IsSubtype(Type l, Type r, AbstractObjectIDAssigner Delta) => (l, r) switch {
        (Void, _) => true,
        (_, Ok) => true,
        (Class cl, Class cr) => Delta.IsClassSubtype(cl.Name, cr.Name),
        (Arrow al, Arrow ar) => IsArrowSubtype(al, ar, Delta),
        _ => false
    };
    private static bool IsArrowSubtype(Arrow l, Arrow r, AbstractObjectIDAssigner Delta) {
        if (!l.IsOnlyLiteral() || !r.IsOnlyLiteral())
            return false;
        foreach (var o in ((ObjectInference.Literal)l.Return).Objects) {
            if (!((ObjectInference.Literal)r.Return).Objects.Contains(o))
                return false;
        }
        if ((l.Pre <= r.Pre).Any(c => c.IsTrivialUnsat(Delta)))
            return false;
        if ((l.Post <= r.Post).Any(c => c.IsTrivialUnsat(Delta)))
            return false;
        return true;
    }
}
public record struct Class(ClassName Name) : Type;
public record class Arrow(VarName[] Args, Environment Pre, Environment Post, ObjectSet Return) : Type {
    public bool IsOnlyLiteral() {
        if (Return is ObjectInference.Var)
            return false;

        foreach (var frame in Pre.StackMap.Mappings) {
            foreach (var val in frame.Values) {
                if (val is ObjectInference.Var)
                    return false;
            }
        }
        foreach (var frame in Post.StackMap.Mappings) {
            foreach (var val in frame.Values) {
                if (val is ObjectInference.Var)
                    return false;
            }
        }

        foreach (var val in Pre.HeapMap.Mapping.Values) {
            if (val is ObjectInference.Var)
                return false;
        }
        foreach (var val in Post.HeapMap.Mapping.Values) {
            if (val is ObjectInference.Var)
                return false;
        }

        foreach (var val in Pre.AliasMap.Mapping.Values) {
            if (val is AliasInference.Var)
                return false;
        }
        foreach (var val in Post.AliasMap.Mapping.Values) {
            if (val is AliasInference.Var)
                return false;
        }

        foreach (var val in Pre.TypeMap.ClosureMapping.Values) {
            if (val is TypeInference.Var)
                return false;
        }
        foreach (var val in Post.TypeMap.ClosureMapping.Values) {
            if (val is TypeInference.Var)
                return false;
        }

        return true;
    }
}
public record struct Void : Type;
public record struct Ok : Type;


public record struct AliasData(AliasFlag Flag);
public enum AliasFlag { S, M }

public interface InferenceVariable {
    InferenceVariable ApplySolution(InferenceVariableSolution Sol);
}
public interface ObjectInference : InferenceVariable {
    public record class Literal(ImmutableHashSet<AbstractObjID> Objects) : ObjectInference {
        public Literal ApplySolution(InferenceVariableSolution Sol) => this;

        InferenceVariable InferenceVariable.ApplySolution(InferenceVariableSolution Sol) {
            return ApplySolution(Sol);
        }
    }

    public record class Var(int ID) : ObjectInference {
        public Literal ApplySolution(InferenceVariableSolution Sol) =>
            new Literal(Sol.ObjSol[this]);

        InferenceVariable InferenceVariable.ApplySolution(InferenceVariableSolution Sol) {
            return ApplySolution(Sol);
        }
    }

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

    new Literal ApplySolution(InferenceVariableSolution Sol);
}
public interface TypeInference : InferenceVariable {
    public record class Literal(ImmutableHashSet<Type> Types) : TypeInference {
        public Literal ApplySolution(InferenceVariableSolution Sol) => this;

        InferenceVariable InferenceVariable.ApplySolution(InferenceVariableSolution Sol) {
            return ApplySolution(Sol);
        }
    }

    public record class Var(int ID) : TypeInference {
        public Literal ApplySolution(InferenceVariableSolution Sol) =>
            new Literal(Sol.TypeSol[this]);

        InferenceVariable InferenceVariable.ApplySolution(InferenceVariableSolution Sol) {
            return ApplySolution(Sol);
        }
    }

    public static TypeInference Create(params Type[] Types) => new Literal([.. Types]);
    public static TypeInference Create(IEnumerable<Type> Types) => new Literal([.. Types]);
    private static int currentID = 0;
    public static Var Create() => new Var(++currentID);

    static InferenceConstraint operator <=(TypeInference l, TypeInference r) => new InferenceConstraint.SubTyping(l, r);
    static InferenceConstraint operator >=(TypeInference l, TypeInference r) => new InferenceConstraint.SubTyping(r, l);

    static InferenceConstraint.SubTyping operator <=(TypeInference l, Type r) => new InferenceConstraint.SubTyping(l, new Literal([r]));
    static InferenceConstraint.SubTyping operator >=(TypeInference l, Type r) => new InferenceConstraint.SubTyping(new Literal([r]), l);

    static InferenceConstraint.SubTyping operator <=(Type l, TypeInference r) => new InferenceConstraint.SubTyping(new Literal([l]), r);
    static InferenceConstraint.SubTyping operator >=(Type l, TypeInference r) => new InferenceConstraint.SubTyping(r, new Literal([l]));

    new Literal ApplySolution(InferenceVariableSolution Sol);
}
public interface AliasInference : InferenceVariable {
    public record class Literal(Alias Flag) : AliasInference {
        public Literal ApplySolution(InferenceVariableSolution Sol) => this;

        InferenceVariable InferenceVariable.ApplySolution(InferenceVariableSolution Sol) {
            return ApplySolution(Sol);
        }
    }

    public record class Var(int ID) : AliasInference {
        public Literal ApplySolution(InferenceVariableSolution Sol) =>
            new Literal(new AliasData(Sol.AliasSol[this]));

        InferenceVariable InferenceVariable.ApplySolution(InferenceVariableSolution Sol) {
            return ApplySolution(Sol);
        }
    }

    public static AliasInference Create(Alias Flag) => new Literal(Flag);
    private static int currentID = 0;
    public static AliasInference Create() => new Var(++currentID);
    public static AliasInference Single { get; } = new Literal(new Alias(AliasFlag.S));
    public static AliasInference Multiple { get; } = new Literal(new Alias(AliasFlag.M));

    static InferenceConstraint operator <=(AliasInference l, AliasInference r) => new InferenceConstraint.AliasBounding(l, r);
    static InferenceConstraint operator >=(AliasInference l, AliasInference r) => new InferenceConstraint.AliasBounding(r, l);

    new Literal ApplySolution(InferenceVariableSolution Sol);
}


public interface InferenceConstraint {
    InferenceConstraint Substitute(IDictionary<InferenceVariable, InferenceVariable> mapping);
    IEnumerable<InferenceConstraint> Normalise();
    bool IsTrivialUnsat(AbstractObjectIDAssigner Delta);

    public interface PartialOrder<T, L> : InferenceConstraint where T : PartialOrder<T, L> where L : InferenceVariable {
        L l { get; init; }
        L r { get; init; }
        static abstract T Transitivity(T l, T r);
        static virtual bool isTransitive(T l, T r) => l.r.Equals(r.l);
        InferenceConstraint ApplySolution(InferenceVariableSolution Sol);
    }

    public record class ObjectInclusion(ObjectInference l, ObjectInference r) : PartialOrder<ObjectInclusion, ObjectInference> {
        public static ObjectInclusion Transitivity(ObjectInclusion l, ObjectInclusion r) =>
            new ObjectInclusion(l.l, r.r);

        public InferenceConstraint ApplySolution(InferenceVariableSolution Sol) =>
            new ObjectInclusion(l.ApplySolution(Sol), r.ApplySolution(Sol));

        public bool IsTrivialUnsat(AbstractObjectIDAssigner Delta) {
            if (l is ObjectInference.Literal ll && r is ObjectInference.Literal rr) {
                foreach (var o in ll.Objects) {
                    if (!rr.Objects.Contains(o))
                        return true;
                }
            }
            return false;
        }

        public IEnumerable<InferenceConstraint> Normalise() => [this];

        public InferenceConstraint Substitute(IDictionary<InferenceVariable, InferenceVariable> mapping) =>
        new ObjectInclusion((ObjectInference)mapping.GetOrDefault(l, l), (ObjectInference)mapping.GetOrDefault(r, r));
    }
    public record class AliasBounding(AliasInference l, AliasInference r) : PartialOrder<AliasBounding, AliasInference> {
        public static AliasBounding Transitivity(AliasBounding l, AliasBounding r) =>
            new AliasBounding(l.l, r.r);

        public InferenceConstraint ApplySolution(InferenceVariableSolution Sol) =>
            new AliasBounding(l.ApplySolution(Sol), r.ApplySolution(Sol));

        public bool IsTrivialUnsat(AbstractObjectIDAssigner Delta) =>
        l is AliasInference.Literal ll && r is AliasInference.Literal rr && ll.Flag.Flag.Equals(AliasFlag.M) && rr.Flag.Flag.Equals(AliasFlag.S);


        public IEnumerable<InferenceConstraint> Normalise() => [this];

        public InferenceConstraint Substitute(IDictionary<InferenceVariable, InferenceVariable> mapping) =>
        new AliasBounding((AliasInference)mapping.GetOrDefault(l, l), (AliasInference)mapping.GetOrDefault(r, r));
    }
    public record class SubTyping(TypeInference l, TypeInference r) : PartialOrder<SubTyping, TypeInference> {
        public static SubTyping Transitivity(SubTyping l, SubTyping r) =>
            new SubTyping(l.l, r.r);

        public InferenceConstraint ApplySolution(InferenceVariableSolution Sol) =>
            new SubTyping(l.ApplySolution(Sol), r.ApplySolution(Sol));

        public bool IsTrivialUnsat(AbstractObjectIDAssigner Delta) {
            if (l is TypeInference.Literal ll && r is TypeInference.Literal rr) {
                foreach (var tl in ll.Types) {
                    bool foundRHS = false;
                    foreach (var tr in rr.Types) {
                        if (Type.IsSubtype(tl, tr, Delta))
                            foundRHS = true;
                        if (foundRHS)
                            break;
                    }
                    if (!foundRHS)
                        return false; //If t in LHS s.t. all r in RHS s.t. l </= r then not subtypes
                }
                return true; //If all t in LHS have some r in RHS s.t. l <= r then subtypes
            }
            return false; //If not literals then not (trivial) subtypes
        }

        public IEnumerable<InferenceConstraint> Normalise() => [this];

        public InferenceConstraint Substitute(IDictionary<InferenceVariable, InferenceVariable> mapping) =>
        new SubTyping((TypeInference)mapping.GetOrDefault(l, l), (TypeInference)mapping.GetOrDefault(l, l));
    }
    public record class HeapLookup(ObjectInference.Var Out, Environment Env, ObjectInference.Var Obj, FieldName Name) : InferenceConstraint {

        public bool IsTrivialUnsat(AbstractObjectIDAssigner Delta) => false;

        public IEnumerable<InferenceConstraint> Normalise() => [this];

        public InferenceConstraint Substitute(IDictionary<InferenceVariable, InferenceVariable> mapping) =>
            new HeapLookup((ObjectInference.Var)mapping.GetOrDefault(Out, Out), Env.Substitute(mapping), (ObjectInference.Var)mapping.GetOrDefault(Obj, Obj), Name);
    }

    public record class HeapUpdate(Environment Out, Environment In, ObjectInference.Var ObjIn, FieldName Name, ObjectInference ObjTo) : InferenceConstraint {
        public bool IsTrivialUnsat(AbstractObjectIDAssigner Delta) => false;

        public IEnumerable<InferenceConstraint> Normalise() => [this];

        public InferenceConstraint Substitute(IDictionary<InferenceVariable, InferenceVariable> mapping) =>
        new HeapUpdate(Out.Substitute(mapping), In.Substitute(mapping), (ObjectInference.Var)mapping.GetOrDefault(ObjIn, ObjIn), Name, (ObjectInference)mapping.GetOrDefault(ObjTo, ObjTo));
    }

    public record class TypeLookup(TypeInference.Var TypeOut, Environment Env, ObjectInference.Var Objs) : InferenceConstraint {
        public bool IsTrivialUnsat(AbstractObjectIDAssigner Delta) => false;

        public IEnumerable<InferenceConstraint> Normalise() => [this];

        public InferenceConstraint Substitute(IDictionary<InferenceVariable, InferenceVariable> mapping) =>
        new TypeLookup((TypeInference.Var)mapping.GetOrDefault(TypeOut, TypeOut), Env.Substitute(mapping), (ObjectInference.Var)mapping.GetOrDefault(Objs, Objs));
    }

    public record class Restriction(ObjectInference.Var Out, Environment Env, ObjectInference.Var In, Type Tau) : InferenceConstraint {
        public bool IsTrivialUnsat(AbstractObjectIDAssigner Delta) => false;

        public IEnumerable<InferenceConstraint> Normalise() => [this];

        public InferenceConstraint Substitute(IDictionary<InferenceVariable, InferenceVariable> mapping) =>
        new Restriction((ObjectInference.Var)mapping.GetOrDefault(Out, Out), Env.Substitute(mapping), (ObjectInference.Var)mapping.GetOrDefault(In, In), Tau);
    }

    public record class ApplicationResolution(
        Environment EnvOut, ObjectInference.Var ObjOut,
        TypeInference.Var TypeInternal, Environment EnvInternal,
        Environment EnvIn, ObjectInference.Var Funcs, ImmutableArray<ObjectInference.Var> Arguments
    ) : InferenceConstraint {
        public bool IsTrivialUnsat(AbstractObjectIDAssigner Delta) => false;

        public IEnumerable<InferenceConstraint> Normalise() => [this];

        public InferenceConstraint Substitute(IDictionary<InferenceVariable, InferenceVariable> mapping) =>
        new ApplicationResolution(
            EnvOut.Substitute(mapping), (ObjectInference.Var)mapping.GetOrDefault(ObjOut, ObjOut),
            (TypeInference.Var)mapping.GetOrDefault(TypeInternal, TypeInternal), EnvInternal.Substitute(mapping),
            EnvIn.Substitute(mapping), (ObjectInference.Var)mapping.GetOrDefault(Funcs, Funcs), [.. Arguments.Select(a => (ObjectInference.Var)mapping.GetOrDefault(a, a))]
        );
    }

    public record class Conditional(ImmutableHashSet<SubTyping> GuardType, ImmutableHashSet<AliasBounding> GuardAlias, ImmutableHashSet<InferenceConstraint> Body) : InferenceConstraint {
        public InferenceConstraint Substitute(IDictionary<InferenceVariable, InferenceVariable> mapping) =>
            new Conditional([.. GuardType.Select(c => (SubTyping)c.Substitute(mapping))],
                [.. GuardAlias.Select(c => (AliasBounding)c.Substitute(mapping))],
             [.. Body.Select(c => c.Substitute(mapping))]);

        public IEnumerable<InferenceConstraint> Normalise() {
            if (GuardType.Count + GuardAlias.Count == 0)
                return Body;
            List<InferenceConstraint> norms = [];
            List<Conditional> others = [];
            foreach (InferenceConstraint constraint in Body.SelectMany(c => c.Normalise())) {
                if (constraint is Conditional c)
                    others.Add(new Conditional([.. GuardType.Append(c.GuardType)], [.. GuardAlias.Append(c.GuardAlias)], c.Body));
                else
                    norms.Add(constraint);
            }
            return [new Conditional(GuardType, GuardAlias, [.. norms]), .. others];
        }

        public bool IsTrivialUnsat(AbstractObjectIDAssigner Delta) => false;
    }

}

public record class AnalysisResult(ImmutableHashSet<InferenceConstraint> Constraints, ObjectInference Return, Environment EndEnv);

public record class MethodSummary(
    ImmutableHashSet<InferenceVariable> InferenceVariables, //Contains all inference variables involved in the analysis of a method; including 'ThisVariable' and 'MethodType'
    ObjectInference.Var ThisVariable,
    ImmutableHashSet<InferenceConstraint> Constraints,
    TypeInference MethodType);

public record class InferenceVariableSolution(
    ImmutableDictionary<ObjectInference.Var, ImmutableHashSet<AbstractObjID>> ObjSol,
    ImmutableDictionary<TypeInference.Var, ImmutableHashSet<Type>> TypeSol,
    ImmutableDictionary<AliasInference.Var, AliasFlag> AliasSol
);
