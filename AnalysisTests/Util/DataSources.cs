using System.Collections.Immutable;
using System.Data;
using Analysis;
using Analysis.Types;
using static AnalysisTests.Util.TestDataCombinators;
using Environment = Analysis.Types.Environment;
using Type = Analysis.Types.Type;
using Void = Analysis.Types.Void;

namespace AnalysisTests.Util;

public static class DataSources {

    #region RawSources

    public static IEnumerable<string> ClassNames = ["A", "B", "C", "D"];
    public static IEnumerable<string> VarNames = ["x", "y", "z"];
    public static IEnumerable<string> FieldNames = ["f", "g", "h"];
    public static IEnumerable<int> AbstractClassIDs = [0, 1, 2, 3];
    public static IEnumerable<int> AbstractClosureIDs = AbstractClassIDs.Select(i => -i);
    public static IEnumerable<int> VariableIDs = [0, 1, 2, 3];
    public static IEnumerable<AliasFlag> AliasFlags = [AliasFlag.S, AliasFlag.M];

    #endregion

    #region AbstractObjectIDs

    public static IEnumerable<int> AbstractObjectIDs = AbstractClassIDs.Append(AbstractClosureIDs);
    public static IEnumerable<int[]> ObjectSets = AbstractObjectIDs.FiniteLists();
    public static IEnumerable<(int[], int[])> ObjectSetPairs = CartesianProd(ObjectSets, ObjectSets);
    public static IEnumerable<(int, int)> VariableIDPairs = CartesianProd(VariableIDs, VariableIDs);

    #endregion

    #region Types

    public static IEnumerable<(string, string)> PairClassNames = CartesianProd(ClassNames, ClassNames);
    public static IEnumerable<Class> Classes = ClassNames.Select(n => new Class(n));
    public static IEnumerable<string[]> ArgNameSet = VarNames.FiniteLists();
    public static IEnumerable<(string[], Environment, Environment, ObjectInference)> ArrowArgs => CartesianProd(ArgNameSet, Envs, Envs, objectInferences);
    public static IEnumerable<Arrow> Arrows => ArrowArgs
        .Select(p => new Arrow(p.Item1, p.Item2, p.Item3, p.Item4));
    public static IEnumerable<Type> Types = Classes
        .Select(c => (Type)c)
        .Append([new Void()])
        .Append([new Ok()]);

    #endregion

    #region ObjectInference

    public static IEnumerable<ObjectInference.Literal> ObjectInferenceLiteral = ObjectSets.Select(objs => new ObjectInference.Literal([.. objs]));
    public static IEnumerable<ObjectInference.Var> ObjectInferenceVar = VariableIDs.Select(id => new ObjectInference.Var(id));
    public static IEnumerable<ObjectInference> objectInferences = ObjectInferenceLiteral.Append<ObjectInference>(ObjectInferenceVar);
    public static IEnumerable<(ObjectInference, ObjectInference)> PairObjectInf = CartesianProd(objectInferences, objectInferences);
    public static IEnumerable<(ObjectInference, ObjectInference, ObjectInference)> TripleObjectInf = CartesianProd(objectInferences, objectInferences, objectInferences);

    #endregion

    #region TypeInference

    public static IEnumerable<TypeInference.Var> TypeInfVars = VariableIDs.Select(id => new TypeInference.Var(id));
    public static IEnumerable<TypeInference.Literal> TypeInfLits = Types.FiniteLists().Select(ts => new TypeInference.Literal([.. ts]));
    public static IEnumerable<TypeInference> TypeInfs = TypeInfVars.Append<TypeInference>(TypeInfLits);
    public static IEnumerable<(TypeInference, TypeInference)> PairTypeInf = CartesianProd(TypeInfs, TypeInfs);
    public static IEnumerable<(TypeInference, TypeInference, TypeInference)> TripleTypeInf = CartesianProd(TypeInfs, TypeInfs, TypeInfs);

    #endregion

    #region AliasInference

    public static IEnumerable<AliasInference.Literal> AliasInfLits = AliasFlags.Select(f => new AliasInference.Literal(new AliasData(f)));
    public static IEnumerable<AliasInference.Var> AliasInfVars = VariableIDs.Select(id => new AliasInference.Var(id));
    public static IEnumerable<AliasInference> AliasInf = AliasInfLits.Append<AliasInference>(AliasInfVars);
    public static IEnumerable<(AliasInference, AliasInference)> PairAliasInf = CartesianProd(AliasInf, AliasInf);
    public static IEnumerable<(AliasInference, AliasInference, AliasInference)> TripleAliasInf = CartesianProd(AliasInf, AliasInf, AliasInf);

    #endregion


    #region Environments

    #region StackEnvs

    public static IEnumerable<ImmutableDictionary<string, ObjectInference>> StackMaps = CartesianProd(VarNames, objectInferences)
        .Select(p => new KeyValuePair<string, ObjectInference>(p.Item1, p.Item2)).FiniteLists().Select(l => l.ToImmutableDictionary());

    public static IEnumerable<StackEnv> StackEnvs = StackMaps.FiniteLists().Select(fs => new StackEnv([.. fs]));

    #endregion

    #region HeapEnv

    public static IEnumerable<(int, ImmutableHashSet<string>)> ObjectFieldMapping =
        AbstractClassIDs.Select<int, (int, ImmutableHashSet<string>)>(o => (o, [.. FieldNames.FiniteLists().Select(f => $"{o}#{f}")]));
    public static IEnumerable<ImmutableDictionary<int, ImmutableHashSet<string>>> ObjectFieldEnvironment =
        ObjectFieldMapping.Select(p => new KeyValuePair<int, ImmutableHashSet<string>>(p.Item1, p.Item2)).FiniteLists()
        .Select<KeyValuePair<int, ImmutableHashSet<string>>[], ImmutableDictionary<int, ImmutableHashSet<string>>>(kvs => [.. kvs]);

    public static IEnumerable<ImmutableHashSet<(int, string)>> HeapDomains = ObjectFieldEnvironment.Select(d =>
            d.SelectMany(kv =>
                kv.Value.Select(f => (kv.Key, f))
            ).ToImmutableHashSet()
        );
    public static IEnumerable<ImmutableDictionary<(int, string), ObjectInference>> HeapMappings = HeapDomains.SelectMany(dom =>
        ExponentialProduct(dom.Select(kv => (kv.Item1, kv.Item2)).ToArray(), objectInferences.ToArray())
            .Select(
                ms => ms.Select(m => new KeyValuePair<(int, string), ObjectInference>(m.Item1, m.Item2)).ToImmutableDictionary()
            )
    );
    public static IEnumerable<HeapEnv> HeapEnvs = HeapMappings.Select(mp => new HeapEnv(mp));

    #endregion

    #region TypeMap

    public static IEnumerable<ImmutableDictionary<int, Class>> ClassMappings = AbstractClassIDs.FiniteLists()
            .SelectMany(dom => ExponentialProduct(dom.ToArray(), Classes.ToArray())
                .Select(d => d.Select(p => new KeyValuePair<int, Class>(p.Item1, p.Item2)).ToImmutableDictionary())
            );

    public static IEnumerable<ImmutableDictionary<int, TypeInference>> ClosureMappings = AbstractClosureIDs.FiniteLists()
            .SelectMany(dom => ExponentialProduct(dom.ToArray(), TypeInfVars.ToArray())
                .Select(d => d.Select(p => new KeyValuePair<int, TypeInference>(p.Item1, p.Item2)).ToImmutableDictionary())
            );

    public static IEnumerable<TypeEnv> TypeEnvs = CartesianProd(ClassMappings, ClosureMappings).Select(P => new TypeEnv(P.Item1, P.Item2));

    #endregion

    #region AliasMap

    public static IEnumerable<ImmutableDictionary<int, AliasInference>> AliasMappings = AbstractObjectIDs.FiniteLists().SelectMany(dom =>
        ExponentialProduct(dom, AliasInf.ToArray())
        .Select(d => d.Select(p => new KeyValuePair<int, AliasInference>(p.Item1, p.Item2)).ToImmutableDictionary())
    );

    public static IEnumerable<AliasEnv> AliasEnvs = AliasMappings.Select(d => new AliasEnv(d));

    #endregion

    public static IEnumerable<(StackEnv, HeapEnv, TypeEnv, AliasEnv)> EnvCol = CartesianProd(StackEnvs, HeapEnvs, TypeEnvs, AliasEnvs);

    public static IEnumerable<Environment> Envs = EnvCol.Select(p => new Environment(p.Item1, p.Item2, p.Item3, p.Item4));

    #endregion

    #region Constraints

    public static IEnumerable<InferenceConstraint.ObjectInclusion> ObjectInclusions = PairObjectInf.Select(p => new InferenceConstraint.ObjectInclusion(p.Item1, p.Item2));
    public static IEnumerable<(InferenceConstraint.ObjectInclusion, InferenceConstraint.ObjectInclusion)> PairObjectInclusions = CartesianProd(ObjectInclusions, ObjectInclusions);
    // public static IEnumerable<(InferenceConstraint.ObjectInclusion, InferenceConstraint.ObjectInclusion)> TripleObjectInclusions = CartesianProd(ObjectInclusions, ObjectInclusions);


    public static IEnumerable<InferenceConstraint.AliasBounding> AliasBounds = PairAliasInf.Select(p => new InferenceConstraint.AliasBounding(p.Item1, p.Item2));
    public static IEnumerable<(InferenceConstraint.AliasBounding, InferenceConstraint.AliasBounding)> PairAliasBounds = CartesianProd(AliasBounds, AliasBounds);

    public static IEnumerable<InferenceConstraint.SubTyping> SubTypings = PairTypeInf.Select(p => new InferenceConstraint.SubTyping(p.Item1, p.Item2));
    public static IEnumerable<(InferenceConstraint.SubTyping, InferenceConstraint.SubTyping)> PairSubTypings = CartesianProd(SubTypings, SubTypings);

    public static IEnumerable<(InferenceConstraint.SubTyping, InferenceConstraint)> TypeConditional => CartesianProd(SubTypings, Constraints);
    public static IEnumerable<(InferenceConstraint.AliasBounding, InferenceConstraint)> AliasConditional => CartesianProd(AliasBounds, Constraints);
    public static IEnumerable<(InferenceConstraint.SubTyping, InferenceConstraint.AliasBounding, InferenceConstraint)> TypeAliasConditional => CartesianProd(SubTypings, AliasBounds, Constraints);

    public static IEnumerable<InferenceConstraint> Constraints = ObjectInclusions.Select(c => (InferenceConstraint)c)
        .Append(AliasBounds).Append(SubTypings).Take(10);

    #endregion

}
