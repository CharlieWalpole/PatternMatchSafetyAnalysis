global using VarName = string;
global using FieldName = string;

global using AbstractObjID = int;
global using ObjectSet = Analysis.Types.ObjectInference;

global using Alias = Analysis.Types.AliasData;

using Type = Analysis.Types.Type;
using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Analysis.Types;

//using ObjectSet = HashSet<AbstractObjID>;

public record class StackEnv(ImmutableArray<ImmutableDictionary<VarName, ObjectSet>> Mappings) {

    public IEnumerable<InferenceVariable> GetInferenceVariables() => Mappings.SelectMany(frame => frame.Values).Where(v => v is ObjectInference.Var);

    public bool ContainsKey(VarName name) {
        foreach (var item in Mappings) {
            if (item.ContainsKey(name))
                return true;
        }
        return false;
    }

    public override string ToString() {
        StringBuilder sb = new StringBuilder();

        sb.Append('[');
        for (int i = 0; i < Mappings.Length; i++) {
            sb.Append('[');
            foreach (var kv in Mappings[i]) {
                sb.Append(kv.Key);
                sb.Append(" -> ");
                sb.Append(kv.Value);
                sb.Append(", ");
            }
            sb.Append(']');
            if (i != Mappings.Length - 1)
                sb.Append(", ");
        }
        sb.Append(']');

        return sb.ToString();
    }

    public StackEnv Push() => Push([]);
    public StackEnv Push(ImmutableDictionary<VarName, ObjectSet> frame) => new StackEnv(Mappings.Add(frame));
    public StackEnv Pop() => new StackEnv(Mappings.RemoveAt(GetTopFrameIndex()));

    protected StackEnv ReplaceStackFrame(int i, ImmutableDictionary<VarName, ObjectSet> frame) =>
        new(Mappings.Replace(Mappings[i], frame));

    protected int GetTopFrameIndex() => Mappings.Length - 1;

    protected int GetTopFrameIndex(VarName name) {
        for (int i = 0; i < Mappings.Length; i++) {
            if (Mappings[i].ContainsKey(name))
                return i;
        }
        throw new ArgumentException("Given name does not exist on the stack.");
    }

    public StackEnv AddVar(VarName name) {
        if (!ContainsKey(name))
            return ReplaceStackFrame(GetTopFrameIndex(), Mappings[GetTopFrameIndex()].Add(name, ObjectSet.Create(new Optional<(VarName, SyntaxNode)>())));
        return this;
    }

    public StackEnv SetVar(VarName name, ObjectSet val) {
        if (!ContainsKey(name))
            return AddVar(name);
        else {
            int index = GetTopFrameIndex(name);
            return ReplaceStackFrame(index, Mappings[index].Remove(name).Add(name, val));
        }
    }

    public ObjectSet GetVar(VarName name) {
        for (int i = 0; i < Mappings.Length; i++) {
            if (Mappings[i].TryGetValue(name, out ObjectSet? value))
                return value;
        }
        throw new ArgumentException("Getting non-existent variable from stack environment.");
    }

    public ObjectSet this[VarName name] => GetVar(name);

    public StackEnv GetFresh() => new(
            [.. Mappings.Select<ImmutableDictionary<VarName, ObjectSet>, ImmutableDictionary<VarName, ObjectSet>>(f =>
                [..f.Keys.Select(n => new KeyValuePair<VarName, ObjectSet>(n, ObjectSet.Create(new Optional<(VarName, SyntaxNode)>())))]
            )]
        );

    /// <summary>
    /// Assumes that the domains of the given stack environments are equal.
    /// </summary>
    public static IEnumerable<InferenceConstraint> operator <=(StackEnv l, StackEnv r)
        => l.Mappings.SelectMany(f => f.Select(kv => new InferenceConstraint.ObjectInclusion(kv.Value, r.Mappings[l.Mappings.IndexOf(f)][kv.Key])));
    /// <summary>
    /// Assumes that the domains of the given stack environments are equal.
    /// </summary>
    public static IEnumerable<InferenceConstraint> operator >=(StackEnv l, StackEnv r) => r <= l;

    /// <summary>
    /// Assumes that the domains of the given stack environments are equal.
    /// </summary>
    public StackEnv Compose(StackEnv r) => r;

    public StackEnv Substitute(IDictionary<InferenceVariable, InferenceVariable> mapping) =>
        new([..Mappings.Select<ImmutableDictionary<VarName, ObjectSet>, ImmutableDictionary<VarName, ObjectSet>>(Mapping =>
            [..Mapping.Select(kv => new KeyValuePair<VarName, ObjectSet>(kv.Key, (ObjectSet)mapping.GetOrDefault(kv.Value, kv.Value)))]
        )]);

    public StackEnv ApplySolution(InferenceVariableSolution Sol) =>
        new([..Mappings.Select<ImmutableDictionary<VarName, ObjectSet>, ImmutableDictionary<VarName, ObjectSet>>(Mapping =>
            [..Mapping.Select(kv => new KeyValuePair<VarName, ObjectSet>(kv.Key, kv.Value.ApplySolution(Sol)))]
        )]);
}

public record class HeapEnv(ImmutableDictionary<(AbstractObjID, FieldName), ObjectSet> Mapping) {

    public IEnumerable<InferenceVariable> GetInferenceVariables() => Mapping.Values.Where(v => v is ObjectInference.Var);

    public HeapEnv AddVar(AbstractObjID obj, FieldName name) {
        if (!Mapping.ContainsKey((obj, name)))
            return new HeapEnv(Mapping.Add((obj, name), ObjectSet.Create(new Optional<(VarName, SyntaxNode)>())));
        return this;
    }

    public override string ToString() {
        StringBuilder sb = new StringBuilder();

        sb.Append('[');
        sb.AppendJoin(", ", Mapping.Select(kv => $"({kv.Key.Item1}, {kv.Key.Item2}) -> {kv.Value}"));
        sb.Append(']');

        return sb.ToString();
    }

    public HeapEnv SetObject(AbstractObjID obj, FieldName name, ObjectSet ID) {
        if (!Mapping.ContainsKey((obj, name)))
            return new(Mapping.Add((obj, name), ID));
        else
            return new(Mapping.Remove((obj, name)).Add((obj, name), ID));
    }

    public ObjectSet GetVar(AbstractObjID obj, FieldName name) {
        if (!Mapping.ContainsKey((obj, name)))
            throw new ArgumentException("Getting non-existent variable from heap environment.");
        return Mapping[(obj, name)];
    }

    public ObjectSet this[AbstractObjID obj, FieldName name] => GetVar(obj, name);

    public HeapEnv GetFresh() => new([.. this.Mapping.Keys.Select(n => new KeyValuePair<(AbstractObjID, FieldName), ObjectSet>(n, ObjectSet.Create(new Optional<(VarName, SyntaxNode)>())))]);

    /// <summary>
    /// Assumes that the domains of the given heap environments are equal.
    /// </summary>
    public static IEnumerable<InferenceConstraint> operator <=(HeapEnv l, HeapEnv r)
        => l.Mapping.Select(kv => new InferenceConstraint.ObjectInclusion(kv.Value, r.Mapping[kv.Key]));
    /// <summary>
    /// Assumes that the domains of the given heap environments are equal.
    /// </summary>
    public static IEnumerable<InferenceConstraint> operator >=(HeapEnv l, HeapEnv r)
        => r.Mapping.Select(kv => new InferenceConstraint.ObjectInclusion(kv.Value, l.Mapping[kv.Key]));

    /// <summary>
    /// Assumes that the domains of the given heap environments are equal.
    /// </summary>
    public HeapEnv Compose(HeapEnv r) => r;

    public HeapEnv Substitute(IDictionary<InferenceVariable, InferenceVariable> mapping) =>
        new([.. Mapping.Select(kv => new KeyValuePair<(AbstractObjID, FieldName), ObjectSet>(kv.Key, (ObjectSet)mapping.GetOrDefault(kv.Value, kv.Value)))]);

    public HeapEnv ApplySolution(InferenceVariableSolution Sol) =>
        new([.. Mapping.Select(kv => new KeyValuePair<(AbstractObjID, FieldName), ObjectSet>(kv.Key, kv.Value.ApplySolution(Sol)))]);
}

public record class TypeEnv(ImmutableDictionary<AbstractObjID, (Class, SyntaxNode)> ClassMapping, ImmutableDictionary<AbstractObjID, TypeInference> ClosureMapping) {

    public IEnumerable<InferenceVariable> GetInferenceVariables() => ClosureMapping.Values.Where(v => v is TypeInference.Var);


    public TypeEnv SetTypeClass(AbstractObjID ID, Class type, SyntaxNode Source) {
        if (!ClassMapping.ContainsKey(ID))
            return this with { ClassMapping = ClassMapping.Add(ID, (type, Source)) };
        else
            return this with { ClassMapping = ClassMapping.Remove(ID).Add(ID, (type, Source)) };
    }

    public override string ToString() {
        StringBuilder sb = new StringBuilder();

        sb.Append('[');
        sb.AppendJoin(", ", ClassMapping.Select(kv => $"{kv.Key} -> {kv.Value}"));
        if (ClassMapping.Count > 0 && ClosureMapping.Count > 0)
            sb.Append(", ");
        sb.AppendJoin(", ", ClosureMapping.Select(kv => $"{kv.Key} -> {kv.Value}"));
        sb.Append(']');

        return sb.ToString();
    }

    public TypeEnv SetTypeArrow(AbstractObjID ID, Arrow type, SyntaxNode Source) {
        if (!ClosureMapping.ContainsKey(ID))
            return this with { ClosureMapping = ClosureMapping.Add(ID, new TypeInference.Literal([type], new Optional<(VarName, SyntaxNode)>((Source.SyntaxTree.FilePath, Source)))) };
        else
            return this with { ClosureMapping = ClosureMapping.Remove(ID).Add(ID, new TypeInference.Literal([type], new Optional<(VarName, SyntaxNode)>((Source.SyntaxTree.FilePath, Source)))) };
    }

    public TypeEnv SetTypeArrow(AbstractObjID ID, TypeInference type) {
        if (!ClosureMapping.ContainsKey(ID))
            return this with { ClosureMapping = ClosureMapping.Add(ID, type) };
        else
            return this with { ClosureMapping = ClosureMapping.Remove(ID).Add(ID, type) };
    }

    public bool isClassObj(AbstractObjID ID) => ClassMapping.ContainsKey(ID);

    public Class GetVarClass(AbstractObjID ID) {
        if (ClassMapping.TryGetValue(ID, out (Class, SyntaxNode) value))
            return value.Item1;
        else
            throw new ArgumentException($"Getting the type of an abstract object ID that does not exist; ID: {ID}");
    }

    public TypeInference GetVarArrow(AbstractObjID ID) {
        if (ClosureMapping.TryGetValue(ID, out TypeInference? value1))
            return value1;
        else
            throw new ArgumentException($"Getting the type of an abstract object ID that does not exist; ID: {ID}");
    }

    public TypeInference GetVar(AbstractObjID ID) => isClassObj(ID) ?
        TypeInference.Create(ClassMapping[ID].Item2, GetVarClass(ID)) :
        GetVarArrow(ID);

    public TypeInference this[AbstractObjID ID] {
        get => GetVar(ID);
        //set => SetType(ID, value);
    }

    public TypeEnv GetFresh() => new(ClassMapping,
        [.. this.ClosureMapping.Keys.Select(n => new KeyValuePair<AbstractObjID, TypeInference>(n, TypeInference.Create(new Optional<(VarName, SyntaxNode)>())))]);

    /// <summary>
    /// Assumes that the domains of the given alias environments are equal.
    /// </summary>
    public static IEnumerable<InferenceConstraint> operator <=(TypeEnv l, TypeEnv r)
        => l.ClosureMapping.Select(kv => new InferenceConstraint.SubTyping(kv.Value, r.ClosureMapping[kv.Key]));
    /// <summary>
    /// Assumes that the domains of the given alias environments are equal.
    /// </summary>
    public static IEnumerable<InferenceConstraint> operator >=(TypeEnv l, TypeEnv r)
        => r.ClosureMapping.Select(kv => new InferenceConstraint.SubTyping(kv.Value, l.ClosureMapping[kv.Key]));

    public TypeEnv Substitute(IDictionary<InferenceVariable, InferenceVariable> mapping) =>
        new(ClassMapping, [.. ClosureMapping.Select(kv => new KeyValuePair<AbstractObjID, TypeInference>(kv.Key, (TypeInference)mapping.GetOrDefault(kv.Value, kv.Value)))]);

    public TypeEnv ApplySolution(InferenceVariableSolution Sol) =>
        new(ClassMapping, [.. ClosureMapping.Select(kv => new KeyValuePair<AbstractObjID, TypeInference>(kv.Key, kv.Value.ApplySolution(Sol)))]);
}

public record class AliasEnv(ImmutableDictionary<AbstractObjID, AliasInference> Mapping) {

    public IEnumerable<InferenceVariable> GetInferenceVariables() => Mapping.Values.Where(v => v is AliasInference.Var);


    public AliasEnv SetAlias(AbstractObjID ID, AliasInference alias) {
        if (!Mapping.ContainsKey(ID))
            return new(Mapping.Add(ID, alias));
        else
            return new(Mapping.Remove(ID).Add(ID, alias));
    }
    public AliasEnv SetAlias(AbstractObjID ID, Alias alias) => SetAlias(ID, new AliasInference.Literal(alias));

    public override string ToString() {
        StringBuilder sb = new StringBuilder();

        sb.Append('[');
        sb.AppendJoin(", ", Mapping.Select(kv => $"{kv.Key} -> {kv.Value}"));
        sb.Append(']');

        return sb.ToString();
    }

    public AliasInference GetVar(AbstractObjID ID) => Mapping[ID];

    public AliasInference this[AbstractObjID ID] {
        get => GetVar(ID);
        //set => SetAlias(ID, value);
    }

    public AliasEnv GetFresh() => new([.. this.Mapping.Keys.Select(n => new KeyValuePair<AbstractObjID, AliasInference>(n, AliasInference.Create()))]);

    /// <summary>
    /// Assumes that the domains of the given alias environments are equal.
    /// </summary>
    public static IEnumerable<InferenceConstraint> operator <=(AliasEnv l, AliasEnv r)
        => l.Mapping.Select(kv => new InferenceConstraint.AliasBounding(kv.Value, r.Mapping[kv.Key]));
    /// <summary>
    /// Assumes that the domains of the given alias environments are equal.
    /// </summary>
    public static IEnumerable<InferenceConstraint> operator >=(AliasEnv l, AliasEnv r)
        => r.Mapping.Select(kv => new InferenceConstraint.AliasBounding(kv.Value, l.Mapping[kv.Key]));

    public (AliasEnv, IEnumerable<InferenceConstraint>) Compose(AliasEnv r) => (r, this <= r);

    public (AliasEnv, IEnumerable<InferenceConstraint>) AliasAdd(AbstractObjID ID) {
        AliasInference X = AliasInference.Create();
        if (!Mapping.ContainsKey(ID))
            return (new AliasEnv(Mapping.Add(ID, X)), [AliasInference.Single <= X, X <= AliasInference.Single]);
        return (new AliasEnv(Mapping.Remove(ID).Add(ID, X)), [AliasInference.Multiple <= X]);
    }

    public AliasEnv Substitute(IDictionary<InferenceVariable, InferenceVariable> mapping) =>
        new([.. Mapping.Select(kv => new KeyValuePair<AbstractObjID, AliasInference>(kv.Key, (AliasInference)mapping.GetOrDefault(kv.Value, kv.Value)))]);

    public AliasEnv ApplySolution(InferenceVariableSolution Sol) =>
        new([.. Mapping.Select(kv => new KeyValuePair<AbstractObjID, AliasInference>(kv.Key, kv.Value.ApplySolution(Sol)))]);
}

public record class Environment(StackEnv StackMap, HeapEnv HeapMap, TypeEnv TypeMap, AliasEnv AliasMap) {
    public Environment() : this(new([]), new([]), new([], []), new([])) { }

    public override string ToString() => $"{StackMap};{HeapMap};{TypeMap};{AliasMap}";

    public ObjectSet this[VarName name] => StackMap[name];
    public ObjectSet this[AbstractObjID obj, FieldName name] => HeapMap[obj, name];
    public TypeInference this[AbstractObjID ID] {
        get => TypeMap[ID];
        //set => TypeMap[ID] = value;
    }
    // public Alias this[AbstractObjID ID]
    // {
    //     get => AliasMap[ID];
    //     set => AliasMap[ID] = value;
    // }

    /// <summary>
    /// Assumes that the domains of the given environments are equal.
    /// </summary>
    public static IEnumerable<InferenceConstraint> operator <=(Environment l, Environment r)
        => [.. l.StackMap <= r.StackMap, .. l.HeapMap <= r.HeapMap, .. l.TypeMap <= r.TypeMap, .. l.AliasMap <= r.AliasMap];
    /// <summary>
    /// Assumes that the domains of the given environments are equal.
    /// </summary>
    public static IEnumerable<InferenceConstraint> operator >=(Environment l, Environment r) => r <= l;

    public Environment GetFresh() => new(StackMap.GetFresh(), HeapMap.GetFresh(), TypeMap.GetFresh(), AliasMap.GetFresh());

    public Environment Push() => this with { StackMap = StackMap.Push() };
    public Environment Push(ImmutableDictionary<VarName, ObjectSet> frame) => this with { StackMap = StackMap.Push(frame) };
    public Environment Pop() => this with { StackMap = StackMap.Pop() };

    public (Environment, IEnumerable<InferenceConstraint>) Compose(Environment r) {
        StackEnv s = StackMap.Compose(r.StackMap);
        HeapEnv h = HeapMap.Compose(r.HeapMap);
        (AliasEnv, IEnumerable<InferenceConstraint>) p = AliasMap.Compose(r.AliasMap);
        return (new Environment(s, h, r.TypeMap, p.Item1), p.Item2);
    }

    public (Environment, IEnumerable<InferenceConstraint>) AliasAdd(AbstractObjID ID) {
        (AliasEnv, IEnumerable<InferenceConstraint>) p = AliasMap.AliasAdd(ID);
        return (this with { AliasMap = p.Item1 }, p.Item2);
    }

    public Environment Substitute(IDictionary<InferenceVariable, InferenceVariable> mapping) =>
        new Environment(StackMap.Substitute(mapping), HeapMap.Substitute(mapping), TypeMap.Substitute(mapping), AliasMap.Substitute(mapping));

    public Environment ApplySolution(InferenceVariableSolution Sol) =>
        new Environment(StackMap.ApplySolution(Sol), HeapMap.ApplySolution(Sol), TypeMap.ApplySolution(Sol), AliasMap.ApplySolution(Sol));

    public IEnumerable<InferenceVariable> GetInferenceVariables() =>
        StackMap.GetInferenceVariables()
        .Append(HeapMap.GetInferenceVariables())
        .Append(TypeMap.GetInferenceVariables())
        .Append(AliasMap.GetInferenceVariables());
}