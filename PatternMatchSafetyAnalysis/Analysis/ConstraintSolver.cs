using System.Collections.Immutable;
using System.Data;
using Analysis.Types;

namespace Analysis;


public class ConstraintSolver {
    protected AbstractObjectIDAssigner Delta;
    protected HashSet<InferenceConstraint> Constraints;

    public ConstraintSolver(AbstractObjectIDAssigner Delta, HashSet<InferenceConstraint> Constraints) {
        this.Delta = Delta;
        this.Constraints = Constraints;
    }

    protected IEnumerable<T> GetConstraints<T>() where T : InferenceConstraint => Constraints.Where(c => c is T).Select(c => (T)c);

    protected bool Transitivity<T, L>() where T : InferenceConstraint.PartialOrder<T, L> where L : InferenceVariable {
        bool added = false;
        IEnumerable<T> objIncl = Constraints.Where(con => con is T).Select(con => (T)con);
        foreach (var l in objIncl) {
            foreach (var r in objIncl) {
                if (T.isTransitive(l, r))
                    added = added || Constraints.Add(T.Transitivity(l, r));
            }
        }
        return added;
    }

    protected bool TransitivityObj() => Transitivity<InferenceConstraint.ObjectInclusion, ObjectInference>();
    protected bool TransitivityType() => Transitivity<InferenceConstraint.SubTyping, TypeInference>();
    protected bool TransitivityAlias() => Transitivity<InferenceConstraint.AliasBounding, AliasInference>();

    protected bool Satisfaction() {
        bool added = false;
        IEnumerable<InferenceConstraint.Conditional> conds = GetConstraints<InferenceConstraint.Conditional>();
        HashSet<InferenceConstraint.SubTyping> tys = [.. GetConstraints<InferenceConstraint.SubTyping>()];
        HashSet<InferenceConstraint.AliasBounding> als = [.. GetConstraints<InferenceConstraint.AliasBounding>()];

        foreach (var c in conds) {
            ImmutableHashSet<InferenceConstraint.SubTyping> guardT = [.. c.GuardType.Where(t => !tys.Contains(t))];
            ImmutableHashSet<InferenceConstraint.AliasBounding> guardA = [.. c.GuardAlias.Where(t => !als.Contains(t))];
            if (guardA.Count == 0 && guardT.Count == 0) {
                foreach (var b in c.Body) {
                    added = added || Constraints.Add(b);
                }
            }
            else
                added = added || Constraints.Add(new InferenceConstraint.Conditional(guardT, guardA, c.Body));
        }

        return added;
    }

    protected bool Weakening() {
        bool added = false;
        IEnumerable<InferenceConstraint.Conditional> conds = GetConstraints<InferenceConstraint.Conditional>();
        IEnumerable<InferenceConstraint.SubTyping> tys = GetConstraints<InferenceConstraint.SubTyping>();
        IEnumerable<InferenceConstraint.AliasBounding> als = GetConstraints<InferenceConstraint.AliasBounding>();

        foreach (var c in conds) {
            foreach (var guardA in c.GuardAlias) {
                foreach (var weak in als.Where(a => a.r.Equals(guardA.r))) {
                    added = added || Constraints.Add(new InferenceConstraint.Conditional(
                        c.GuardType,
                        [.. c.GuardAlias.Select(a => a == guardA ? new InferenceConstraint.AliasBounding(a.l, weak.l) : a)],
                        c.Body));
                }
            }
            foreach (var guardT in c.GuardType) {
                foreach (var weak in tys.Where(a => a.r.Equals(guardT.r))) {
                    added = added || Constraints.Add(new InferenceConstraint.Conditional(
                        [.. c.GuardType.Select(a => a == guardT ? new InferenceConstraint.SubTyping(a.l, weak.l) : a)],
                        c.GuardAlias,
                        c.Body));
                }
            }
        }
        return added;
    }

    protected bool HeapUpdate() {
        bool added = false;
        IEnumerable<InferenceConstraint.HeapUpdate> HUs = GetConstraints<InferenceConstraint.HeapUpdate>();
        Dictionary<AbstractObjID, InferenceConstraint.ObjectInclusion> objIncl = GetConstraints<InferenceConstraint.ObjectInclusion>().Where(c => c.l is ObjectInference.Literal && c.r is ObjectInference.Var)
            .Select(c => new KeyValuePair<AbstractObjID, InferenceConstraint.ObjectInclusion>(((ObjectInference.Var)c.r).ID, c)).ToDictionary();
        Dictionary<int, InferenceConstraint.AliasBounding> alsIncl = GetConstraints<InferenceConstraint.AliasBounding>().Where(c => c.l is AliasInference.Literal && c.r is AliasInference.Var)
            .Select(c => new KeyValuePair<int, InferenceConstraint.AliasBounding>(((AliasInference.Var)c.r).ID, c)).ToDictionary(); ;

        foreach (var hu in HUs) {
            if (objIncl.TryGetValue(hu.ObjIn.ID, out InferenceConstraint.ObjectInclusion? value)) {
                foreach (var o in ((ObjectInference.Literal)value.l).Objects) {
                    Constraints.Add(new InferenceConstraint.ObjectInclusion(hu.ObjTo, hu.Out[o, hu.Name])); //Update-Inclusion
                    foreach (var dom in hu.In.HeapMap.Mapping.Keys) {
                        if ((hu.In.AliasMap[o] is AliasInference.Var v && alsIncl.ContainsKey(v.ID)) || (hu.In.AliasMap[o] is AliasInference.Literal l && l.Flag.Equals(AliasFlag.M))) {
                            added = added || Constraints.Add(new InferenceConstraint.ObjectInclusion(hu.In[dom.Item1, dom.Item2], hu.Out[dom.Item1, dom.Item2])); //Alias-M-Bound
                        }
                        if (o != dom.Item1) {
                            added = added || Constraints.Add(new InferenceConstraint.ObjectInclusion(hu.In[dom.Item1, dom.Item2], hu.Out[dom.Item1, dom.Item2])); //WU-Passthrough
                        }
                        if (!hu.Name.Equals(dom.Item2)) {
                            added = added || Constraints.Add(new InferenceConstraint.ObjectInclusion(hu.In[dom.Item1, dom.Item2], hu.Out[dom.Item1, dom.Item2])); //Field-Passthrough
                        }
                    }
                }
            }
        }
        return added;
    }

    protected bool HeapLookup() {
        bool added = false;
        IEnumerable<InferenceConstraint.HeapLookup> HLs = GetConstraints<InferenceConstraint.HeapLookup>();
        Dictionary<AbstractObjID, InferenceConstraint.ObjectInclusion> objIncl = GetConstraints<InferenceConstraint.ObjectInclusion>().Where(c => c.l is ObjectInference.Literal && c.r is ObjectInference.Var)
            .Select(c => new KeyValuePair<AbstractObjID, InferenceConstraint.ObjectInclusion>(((ObjectInference.Var)c.r).ID, c)).ToDictionary();

        foreach (var hl in HLs) {
            if (objIncl.TryGetValue(hl.Obj.ID, out InferenceConstraint.ObjectInclusion? value)) {
                foreach (var o in ((ObjectInference.Literal)value.l).Objects) {
                    added = added || Constraints.Add(new InferenceConstraint.ObjectInclusion(hl.Env[o, hl.Name], hl.Out)); //HL-Inclusion
                }
            }
        }
        return added;
    }

    protected bool TypeLookup() {
        bool added = false;
        IEnumerable<InferenceConstraint.TypeLookup> TLs = GetConstraints<InferenceConstraint.TypeLookup>();
        Dictionary<AbstractObjID, InferenceConstraint.ObjectInclusion> objIncl = GetConstraints<InferenceConstraint.ObjectInclusion>().Where(c => c.l is ObjectInference.Literal && c.r is ObjectInference.Var)
            .Select(c => new KeyValuePair<AbstractObjID, InferenceConstraint.ObjectInclusion>(((ObjectInference.Var)c.r).ID, c)).ToDictionary();

        foreach (var tl in TLs) {
            if (objIncl.TryGetValue(tl.Objs.ID, out InferenceConstraint.ObjectInclusion? value)) {
                foreach (var o in ((ObjectInference.Literal)value.l).Objects) {
                    added = added || Constraints.Add(new InferenceConstraint.SubTyping(tl.Env.TypeMap[o], tl.TypeOut)); //TL-Inclusion
                }
            }
        }
        return added;
    }

    protected bool Restrict() {
        bool added = false;
        IEnumerable<InferenceConstraint.Restriction> Rs = GetConstraints<InferenceConstraint.Restriction>();
        Dictionary<AbstractObjID, InferenceConstraint.ObjectInclusion> objIncl = GetConstraints<InferenceConstraint.ObjectInclusion>().Where(c => c.l is ObjectInference.Literal && c.r is ObjectInference.Var)
            .Select(c => new KeyValuePair<AbstractObjID, InferenceConstraint.ObjectInclusion>(((ObjectInference.Var)c.r).ID, c)).ToDictionary();

        foreach (var r in Rs) {
            added = added || Constraints.Add(new InferenceConstraint.ObjectInclusion(r.Out, r.In)); //RT-Bound
            if (objIncl.TryGetValue(r.In.ID, out InferenceConstraint.ObjectInclusion? value)) {
                foreach (var o in ((ObjectInference.Literal)value.l).Objects) {
                    added = added || Constraints.Add(new InferenceConstraint.Conditional(
                        [new InferenceConstraint.SubTyping(r.Env.TypeMap[o], new TypeInference.Literal([r.Tau]))],
                        [],
                        [new InferenceConstraint.ObjectInclusion(new ObjectInference.Literal([o]), r.Out)]
                    )); //RT-Inclusion
                }
            }
        }
        return added;
    }

    protected bool ApplicationResolution() {
        bool added = false;
        IEnumerable<InferenceConstraint.ApplicationResolution> Apps = GetConstraints<InferenceConstraint.ApplicationResolution>();
        Dictionary<AbstractObjID, InferenceConstraint.ObjectInclusion> objIncl = GetConstraints<InferenceConstraint.ObjectInclusion>().Where(c => c.l is ObjectInference.Literal && c.r is ObjectInference.Var)
            .Select(c => new KeyValuePair<AbstractObjID, InferenceConstraint.ObjectInclusion>(((ObjectInference.Var)c.r).ID, c)).ToDictionary();
        Dictionary<int, InferenceConstraint.SubTyping> subTy = GetConstraints<InferenceConstraint.SubTyping>().Where(c => c.l is TypeInference.Literal && c.r is TypeInference.Var)
            .Select(c => new KeyValuePair<int, InferenceConstraint.SubTyping>(((ObjectInference.Var)c.r).ID, c)).ToDictionary();

        foreach (var ap in Apps) {
            added = added || AppTL(ap);

            if (subTy.TryGetValue(ap.TypeInternal.ID, out InferenceConstraint.SubTyping? value)) {
                foreach (var ty in ((TypeInference.Literal)value.l).Types) {
                    if (ty is Arrow arr) {
                        added = added || Constraints.Add(new InferenceConstraint.ObjectInclusion(arr.Return, ap.ObjOut));
                        foreach (var c in arr.Post <= ap.EnvOut) {
                            added = added || Constraints.Add(c);
                        }

                        if (arr.Pre.StackMap.Mappings.Length > 1)
                            throw new ArgumentException("Arrow type's Pre-Environment's Stack contatains more than 1 scope frame.");
                        foreach (var x in arr.Pre.StackMap.Mappings[0].Keys) {
                            if (arr.Args.Contains(x)) { //AppResolve (argument clause)
                                int i = arr.Args.IndexOf(x);
                                added = added || Constraints.Add(new InferenceConstraint.ObjectInclusion(ap.Arguments[i], arr.Pre[x]));
                            }
                            else { //AppCapture
                                ImmutableHashSet<AbstractObjID> objs = ImmutableHashSet<AbstractObjID>.Empty;
                                if (arr.Pre[x] is ObjectInference.Var v && objIncl.TryGetValue(v.ID, out InferenceConstraint.ObjectInclusion? incl) && incl.l is ObjectInference.Literal ol)
                                    objs = ol.Objects;
                                else if (arr.Pre[x] is ObjectInference.Literal l)
                                    objs = l.Objects;

                                foreach (var o in objs) {
                                    foreach (var f in arr.Pre.HeapMap.Mapping.Keys.Where(kv => kv.Item1 == o)) {
                                        added = added || Constraints.Add(new InferenceConstraint.ObjectInclusion(ap.EnvIn[o, f.Item2], arr.Pre[o, f.Item2]));
                                    }
                                }
                            }
                        }
                    }
                    else if (ty is Class)
                        throw new NotImplementedException("Using a class object as a function is not supported.");
                }
            }
        }

        return added;
    }

    protected bool AppTL(InferenceConstraint.ApplicationResolution ap) {
        bool added = false;

        added = added || Constraints.Add(new InferenceConstraint.TypeLookup(ap.TypeInternal, ap.EnvIn, ap.Funcs));
        (var e, var cons) = ap.EnvIn.Compose(ap.EnvInternal);
        foreach (var c in cons) {
            added = added || Constraints.Add(c);
        }
        IEnumerable<InferenceConstraint> consMerge = [.. e <= ap.EnvOut, .. ap.EnvOut <= e];
        foreach (var c in consMerge) {
            added = added || Constraints.Add(c);
        }

        return added;
    }

    protected void FindFixpoint() {
        bool ConstraintsChanged = true;
        while (ConstraintsChanged) {
            ConstraintsChanged = RunRules();
        }
    }

    protected bool RunRules() =>
           TransitivityObj()
        || TransitivityType()
        || TransitivityAlias()
        || Satisfaction()
        || Weakening()
        || HeapUpdate()
        || HeapLookup()
        || TypeLookup()
        || Restrict()
        || ApplicationResolution();

    protected InferenceVariableSolution ReadSolutionFromConstraints() {
        IEnumerable<InferenceConstraint.ObjectInclusion> objIncl = GetConstraints<InferenceConstraint.ObjectInclusion>().Where(c => c.l is ObjectInference.Literal && c.r is ObjectInference.Var);
        IEnumerable<InferenceConstraint.SubTyping> subTy = GetConstraints<InferenceConstraint.SubTyping>().Where(c => c.l is TypeInference.Literal && c.r is TypeInference.Var);
        IEnumerable<InferenceConstraint.AliasBounding> als = GetConstraints<InferenceConstraint.AliasBounding>().Where(c => c.l is AliasInference.Literal && c.r is AliasInference.Var);

        Dictionary<ObjectInference.Var, HashSet<AbstractObjID>> ObjSol = new Dictionary<ObjectInference.Var, HashSet<int>>();
        Dictionary<TypeInference.Var, HashSet<Types.Type>> TypeSol = new Dictionary<TypeInference.Var, HashSet<Types.Type>>();
        Dictionary<AliasInference.Var, AliasFlag> AliasSol = new Dictionary<AliasInference.Var, AliasFlag>();

        foreach (var c in objIncl) {
            ObjectInference.Var k = (ObjectInference.Var)c.r;
            ObjectInference.Literal v = (ObjectInference.Literal)c.l;

            if (!ObjSol.ContainsKey(k))
                ObjSol.Add(k, new HashSet<int>());

            foreach (var o in v.Objects) {
                ObjSol[k].Add(o);
            }
        }

        foreach (var c in subTy) {
            TypeInference.Var k = (TypeInference.Var)c.r;
            TypeInference.Literal v = (TypeInference.Literal)c.l;

            if (!TypeSol.ContainsKey(k))
                TypeSol.Add(k, new HashSet<Types.Type>());

            foreach (var o in v.Types) {
                TypeSol[k].Add(o);
            }
        }

        foreach (var c in als) {
            AliasInference.Var k = (AliasInference.Var)c.r;
            AliasInference.Literal v = (AliasInference.Literal)c.l;

            AliasSol.Add(k, v.Flag.Flag);
        }

        return new InferenceVariableSolution(
            ObjSol.Select(kv => new KeyValuePair<ObjectInference.Var, ImmutableHashSet<int>>(kv.Key, [.. kv.Value])).ToImmutableDictionary(),
            TypeSol.Select(kv => new KeyValuePair<TypeInference.Var, ImmutableHashSet<Types.Type>>(kv.Key, [.. kv.Value])).ToImmutableDictionary(),
            [.. AliasSol]
        );
    }

    protected bool IsTypeSafe() {
        InferenceVariableSolution Sol = ReadSolutionFromConstraints();

        IEnumerable<InferenceConstraint> cons = GetConstraints<InferenceConstraint.ObjectInclusion>().Select(c => c.ApplySolution(Sol))
            .Append(GetConstraints<InferenceConstraint.SubTyping>().Select(c => c.ApplySolution(Sol)))
            .Append(GetConstraints<InferenceConstraint.AliasBounding>().Select(c => c.ApplySolution(Sol)));

        return cons.Any(c => c.IsTrivialUnsat(Delta));
    }


}
