global using VarName = string;
global using FieldName = string;

global using AbstractObjID = int;
global using ObjectSet = System.Collections.Generic.HashSet<int>;

global using Alias = Analysis.Types.AliasData;

using Type = Analysis.Types.Type;

namespace Analysis.Types;

//using ObjectSet = HashSet<AbstractObjID>;

public class StackEnv
{
    protected Dictionary<VarName, ObjectSet> mapping = [];

    public void AddVar(VarName name)
    {
        if(!mapping.ContainsKey(name))
            mapping.Add(name, []);
    }

    public void IncludeObject(VarName name, AbstractObjID ID)
    {
        if(!mapping.ContainsKey(name))
            AddVar(name);
        mapping[name].Add(ID);
    }

    public ObjectSet GetVar(VarName name)
    {
        if(!mapping.ContainsKey(name))
            AddVar(name);
        return mapping[name];
    }

    public ObjectSet this[VarName name] => GetVar(name);
}

public class HeapEnv {
    protected Dictionary<(AbstractObjID, FieldName), ObjectSet> mapping = [];

    public void AddVar(AbstractObjID obj, FieldName name) {
        if (!mapping.ContainsKey((obj, name)))
            mapping.Add((obj, name), []);
    }

    public void IncludeObject(AbstractObjID obj, FieldName name, AbstractObjID ID) {
        if (!mapping.ContainsKey((obj, name)))
            AddVar(obj, name);
        mapping[(obj, name)].Add(ID);
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

    public void AddType(AbstractObjID ID, Type type) {
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
        set => AddType(ID, value);
    }
}

public class AliasEnv {
    protected Dictionary<AbstractObjID, Alias> mapping = [];

    public void AddType(AbstractObjID ID, Alias alias) {
        mapping[ID] = alias;
    }

    public Alias GetVar(AbstractObjID ID) {
        mapping.TryGetValue(ID, out Alias ret);
        return ret;
    }

    public Alias this[AbstractObjID ID] {
        get => GetVar(ID);
        set => AddType(ID, value);
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