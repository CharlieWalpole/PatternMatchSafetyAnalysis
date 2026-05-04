global using VarName = string;
global using FieldName = string;

global using AbstractObjID = int;
global using ObjectSet = Analysis.Types.ObjectInference;

global using Alias = Analysis.Types.AliasData;

using Type = Analysis.Types.Type;

namespace Analysis.Types;

//using ObjectSet = HashSet<AbstractObjID>;

public class StackEnv {
    protected Dictionary<VarName, ObjectSet> mapping = [];

    public void AddVar(VarName name) {
        if (!mapping.ContainsKey(name))
            mapping.Add(name, ObjectSet.Create());
    }

    public void SetVar(VarName name, ObjectSet val) {
        if (!mapping.TryAdd(name, val))
            mapping[name] = val;
    }

    public ObjectSet GetVar(VarName name) {
        if (!mapping.ContainsKey(name))
            AddVar(name);
        return mapping[name];
    }

    public ObjectSet this[VarName name] => GetVar(name);
}

public class HeapEnv {
    protected Dictionary<(AbstractObjID, FieldName), ObjectSet> mapping = [];

    public void AddVar(AbstractObjID obj, FieldName name) {
        if (!mapping.ContainsKey((obj, name)))
            mapping.Add((obj, name), ObjectSet.Create());
    }

    public void SetObject(AbstractObjID obj, FieldName name, ObjectSet ID) {
        if (!mapping.TryAdd((obj, name), ID))
            mapping[(obj, name)] = ID;
    }

    public ObjectSet GetVar(AbstractObjID obj, FieldName name) {
        if (!mapping.ContainsKey((obj, name)))
            AddVar(obj, name);
        return mapping[(obj, name)];
    }

    public ObjectSet this[AbstractObjID obj, FieldName name] => GetVar(obj, name);
}

public class TypeEnv {
    protected Dictionary<AbstractObjID, Type> mapping = [];

    public void SetType(AbstractObjID ID, Type type) {
        if(!mapping.TryAdd(ID, type))
            mapping[ID] = type;
    }

    public Type GetVar(AbstractObjID ID) {
        mapping.TryGetValue(ID, out Type? ret);
        if (ret is not null)
            return ret;
        else
            throw new ArgumentException($"Getting the type of an abstract object ID that does not exist; ID: {ID}");
    }

    public Type this[AbstractObjID ID] {
        get => GetVar(ID);
        set => SetType(ID, value);
    }
}

public class AliasEnv {
    protected Dictionary<AbstractObjID, Alias> mapping = [];

    public void SetType(AbstractObjID ID, Alias alias) {
        if(!mapping.TryAdd(ID, alias))
            mapping[ID] = alias;
    }

    public Alias GetVar(AbstractObjID ID) {
        mapping.TryGetValue(ID, out Alias ret);
        return ret;
    }

    public Alias this[AbstractObjID ID] {
        get => GetVar(ID);
        set => SetType(ID, value);
    }
}

public class Environment {
    protected StackEnv StackMap = new();
    protected HeapEnv HeapMap = new();
    protected TypeEnv TypeMap = new();
    protected AliasEnv AliasMap = new();

    public ObjectSet this[VarName name] => StackMap[name];
    public ObjectSet this[AbstractObjID obj, FieldName name] => HeapMap[obj, name];
    public Type this[AbstractObjID ID] {
        get => TypeMap[ID];
        set => TypeMap[ID] = value;
    }
    // public Alias this[AbstractObjID ID]
    // {
    //     get => AliasMap[ID];
    //     set => AliasMap[ID] = value;
    // }

}