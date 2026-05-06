global using VarName = string;
global using FieldName = string;

global using AbstractObjID = int;
global using ObjectSet = Analysis.Types.ObjectInference;

global using Alias = Analysis.Types.AliasData;

using Type = Analysis.Types.Type;
using System.Collections.Immutable;

namespace Analysis.Types;

//using ObjectSet = HashSet<AbstractObjID>;

public record class StackEnv(ImmutableArray<ImmutableDictionary<VarName, ObjectSet>> Mappings) { //TODO: Should be a stack of maps

    public bool ContainsKey(VarName name) {
        foreach (var item in Mappings) {
            if (item.ContainsKey(name))
                return true;
        }
        return false;
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
            return ReplaceStackFrame(GetTopFrameIndex(), Mappings[GetTopFrameIndex()].Add(name, ObjectSet.Create()));
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
                [..f.Keys.Select(n => new KeyValuePair<VarName, ObjectSet>(n, ObjectSet.Create()))]
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
}

public record class HeapEnv(ImmutableDictionary<(AbstractObjID, FieldName), ObjectSet> Mapping) {
    public HeapEnv AddVar(AbstractObjID obj, FieldName name) {
        if (!Mapping.ContainsKey((obj, name)))
            return new HeapEnv(Mapping.Add((obj, name), ObjectSet.Create()));
        return this;
    }

    public HeapEnv SetObject(AbstractObjID obj, FieldName name, ObjectSet ID) {
        if (!Mapping.ContainsKey((obj, name)))
            return new(Mapping.Add((obj, name), ObjectSet.Create()));
        else
            return new(Mapping.Remove((obj, name)).Add((obj, name), ID));
    }

    public ObjectSet GetVar(AbstractObjID obj, FieldName name) {
        if (!Mapping.ContainsKey((obj, name)))
            throw new ArgumentException("Getting non-existent variable from heap environment.");
        return Mapping[(obj, name)];
    }

    public ObjectSet this[AbstractObjID obj, FieldName name] => GetVar(obj, name);

    public HeapEnv GetFresh() => new([.. this.Mapping.Keys.Select(n => new KeyValuePair<(AbstractObjID, FieldName), ObjectSet>(n, ObjectSet.Create()))]);

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
}

public record class TypeEnv(ImmutableDictionary<AbstractObjID, Type> Mapping) {
    public TypeEnv SetType(AbstractObjID ID, Type type) {
        if (!Mapping.ContainsKey(ID))
            return new(Mapping.Add(ID, type));
        else
            return new (Mapping.Remove(ID).Add(ID, type));
    }

    public Type GetVar(AbstractObjID ID) {
        Mapping.TryGetValue(ID, out Type? ret);
        if (ret is not null)
            return ret;
        else
            throw new ArgumentException($"Getting the type of an abstract object ID that does not exist; ID: {ID}");
    }

    public Type this[AbstractObjID ID] {
        get => GetVar(ID);
        //set => SetType(ID, value);
    }
}

public record class AliasEnv(ImmutableDictionary<AbstractObjID, AliasInference> Mapping) {
    public AliasEnv SetAlias(AbstractObjID ID, AliasInference alias) {
        if (!Mapping.ContainsKey(ID))
            return new(Mapping.Add(ID, alias));
        else
            return new(Mapping.Remove(ID).Add(ID, alias));
    }
    public AliasEnv SetAlias(AbstractObjID ID, Alias alias) => SetAlias(ID, new AliasInference.Literal(alias));

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
}

public record class Environment(StackEnv StackMap, HeapEnv HeapMap, TypeEnv TypeMap, AliasEnv AliasMap) {
    public Environment() : this(new([]), new([]), new([]), new([])) { }

    public ObjectSet this[VarName name] => StackMap[name];
    public ObjectSet this[AbstractObjID obj, FieldName name] => HeapMap[obj, name];
    public Type this[AbstractObjID ID] {
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
        => [.. l.StackMap <= r.StackMap, .. l.HeapMap <= r.HeapMap, .. l.AliasMap <= r.AliasMap];
    /// <summary>
    /// Assumes that the domains of the given environments are equal.
    /// </summary>
    public static IEnumerable<InferenceConstraint> operator >=(Environment l, Environment r) => r <= l;

    public Environment GetFresh() => new(StackMap.GetFresh(), HeapMap.GetFresh(), TypeMap, AliasMap.GetFresh());

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

}