using System.Collections.Immutable;
using System.Data;
using Analysis.Types;
using Microsoft.CodeAnalysis;

namespace Analysis;


public class ConstraintSolver {
    protected AbstractObjectIDAssigner Delta;
    public ResolutionConstraintHandler Constraints;


    public ImmutableHashSet<InferenceConstraint> InferenceConstraints => [..Constraints.Constraints];

    public ImmutableHashSet<InferenceConstraint.PartialOrder> UnSatConstraints(InferenceVariableSolution Sol) => [..Constraints.PartialOrders
        .Where(c => c.BothLiteral() && c.ApplySolution(Sol).IsTrivialUnsat(Delta))];

    public string PrintConstraints() => Constraints.PrintConstraints();

    public ConstraintSolver(AbstractObjectIDAssigner Delta, IEnumerable<InferenceConstraint> Constraints) {
        this.Delta = Delta;
        this.Constraints = new ResolutionConstraintHandler(Constraints);
    }

    protected bool Transitivity<T, L>(IEnumerable<T> cons) where T : InferenceConstraint.PartialOrder<T, L> where L : InferenceVariable {
        bool added = false;
        ImmutableHashSet<T> objIncl = [..cons];//[..Constraints.Where(con => con is T).Select(con => (T)con)];
        foreach (var l in objIncl) {
            foreach (var r in objIncl) {
                if (T.isTransitive(l, r))
                    added = added || Constraints.Add(T.Transitivity(l, r));
            }
        }
        return added;
    }

    protected bool TransitivityObj() => Transitivity<InferenceConstraint.ObjectInclusion, ObjectInference>(Constraints.ObjectInclusions);
    protected bool TransitivityType() => Transitivity<InferenceConstraint.SubTyping, TypeInference>(Constraints.SubTypings);
    protected bool TransitivityAlias() => Transitivity<InferenceConstraint.AliasBounding, AliasInference>(Constraints.AliasBoundings);

    protected bool Satisfaction() {
        bool added = false;
        ImmutableHashSet<InferenceConstraint.Conditional> conds = [..Constraints.Conditionals];
        ImmutableHashSet<InferenceConstraint.SubTyping> tys = [.. Constraints.SubTypings];
        ImmutableHashSet<InferenceConstraint.AliasBounding> als = [.. Constraints.AliasBoundings];

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
        ImmutableHashSet<InferenceConstraint.Conditional> conds = [..Constraints.Conditionals];
        ImmutableHashSet<InferenceConstraint.SubTyping> tys = [..Constraints.SubTypings];
        ImmutableHashSet<InferenceConstraint.AliasBounding> als = [..Constraints.AliasBoundings];

        foreach (var c in conds) {
            foreach (var guardA in c.GuardAlias) {
                ImmutableHashSet<InferenceConstraint.AliasBounding> tmp = [.. als.Where(a => a.r.Equals(guardA.r))];
                foreach (var weak in tmp) {
                    added = added || Constraints.Add(new InferenceConstraint.Conditional(
                        c.GuardType,
                        [.. c.GuardAlias.Select(a => a == guardA ? new InferenceConstraint.AliasBounding(a.l, weak.l) : a)],
                        c.Body));
                }
            }
            foreach (var guardT in c.GuardType) {
                ImmutableHashSet<InferenceConstraint.SubTyping> tmp = [.. tys.Where(a => a.r.Equals(guardT.r))];
                foreach (var weak in tmp) {
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
        ImmutableHashSet<InferenceConstraint.HeapUpdate> HUs = [..Constraints.HeapUpdates];
        ImmutableDictionary<int, ImmutableHashSet<AbstractObjID>> objSol = [..ReadObjSolution()
            .Select(kv => new KeyValuePair<int, ImmutableHashSet<AbstractObjID>>(kv.Key.ID, kv.Value))];
        ImmutableDictionary<int, AliasFlag> aliasSol = [..ReadAliasSolution()
            .Select(kv => new KeyValuePair<int, AliasFlag>(kv.Key.ID, kv.Value))];

        foreach (var hu in HUs) {
            if (objSol.TryGetValue(hu.ObjIn.ID, out ImmutableHashSet<AbstractObjID>? value)) {
                foreach (var o in value) {
                    Constraints.Add(new InferenceConstraint.ObjectInclusion(hu.ObjTo, hu.Out[o, hu.Name])); //Update-Inclusion
                    foreach (var dom in hu.In.HeapMap.Mapping.Keys) {
                        if ((hu.In.AliasMap[o] is AliasInference.Var v && aliasSol.ContainsKey(v.ID) && aliasSol[v.ID] == AliasFlag.M) || (hu.In.AliasMap[o] is AliasInference.Literal l && l.Flag.Equals(AliasFlag.M))) {
                            added = added || Constraints.Add(new InferenceConstraint.ObjectInclusion(hu.In[dom.Item1, dom.Item2], hu.Out[dom.Item1, dom.Item2])); //Alias-M-Bound
                        }
                        if (!o.Equals(dom.Item1)) {
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
        ImmutableHashSet<InferenceConstraint.HeapLookup> HLs = [..Constraints.HeapLookups];
        ImmutableDictionary<int, ImmutableHashSet<AbstractObjID>> objSol = [..ReadObjSolution()
            .Select(kv => new KeyValuePair<int, ImmutableHashSet<AbstractObjID>>(kv.Key.ID, kv.Value))];

        foreach (var hl in HLs) {
            if (objSol.TryGetValue(hl.Obj.ID, out ImmutableHashSet<AbstractObjID>? value)) {
                foreach (var o in value) {
                    added = added || Constraints.Add(new InferenceConstraint.ObjectInclusion(hl.Env[o, hl.Name], hl.Out)); //HL-Inclusion
                }
            }
        }
        return added;
    }

    protected bool TypeLookup() {
        bool added = false;
        ImmutableHashSet<InferenceConstraint.TypeLookup> TLs = [..Constraints.TypeLookups];
        ImmutableDictionary<int, ImmutableHashSet<AbstractObjID>> objSol = [..ReadObjSolution()
            .Select(kv => new KeyValuePair<int, ImmutableHashSet<AbstractObjID>>(kv.Key.ID, kv.Value))];

        foreach (var tl in TLs) {
            if (objSol.TryGetValue(tl.Objs.ID, out ImmutableHashSet<AbstractObjID>? value)) {
                foreach (var o in value) {
                    added = added || Constraints.Add(new InferenceConstraint.SubTyping(tl.Env.TypeMap[o], tl.TypeOut)); //TL-Inclusion
                }
            }
        }
        return added;
    }

    protected bool Restrict() {
        bool added = false;
        ImmutableHashSet<InferenceConstraint.Restriction> Rs = [..Constraints.Restrictions];
        ImmutableDictionary<int, ImmutableHashSet<AbstractObjID>> objSol = [..ReadObjSolution()
            .Select(kv => new KeyValuePair<int, ImmutableHashSet<AbstractObjID>>(kv.Key.ID, kv.Value))];

        foreach (var r in Rs) {
            added = added || Constraints.Add(new InferenceConstraint.ObjectInclusion(r.Out, r.In)); //RT-Bound
            if (objSol.TryGetValue(r.In.ID, out ImmutableHashSet<AbstractObjID>? value)) {
                foreach (var o in value) {
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
        ImmutableHashSet<InferenceConstraint.ApplicationResolution> Apps = [..Constraints.ApplicationResolutions];
        ImmutableDictionary<int, ImmutableHashSet<AbstractObjID>> objSol = [..ReadObjSolution()
            .Select(kv => new KeyValuePair<int, ImmutableHashSet<AbstractObjID>>(kv.Key.ID, kv.Value))];
        ImmutableDictionary<int, ImmutableHashSet<Types.Type>> typeSol = [..ReadTypeSolution()
            .Select(kv => new KeyValuePair<int, ImmutableHashSet<Types.Type>>(kv.Key.ID, kv.Value))];
        
        foreach (var ap in Apps) {
            added = added || AppTL(ap);

            if (typeSol.TryGetValue(ap.TypeInternal.ID, out ImmutableHashSet<Types.Type>? value)) {
                foreach (var ty in value) {
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
                                ImmutableHashSet<AbstractObjID> objs = [];
                                if (arr.Pre[x] is ObjectInference.Var v && objSol.TryGetValue(v.ID, out ImmutableHashSet<AbstractObjID>? val))
                                    objs = val;
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

    public ConstraintSolver FindFixpoint() {
        bool ConstraintsChanged = true;
        // int size = Constraints.Constraints.Count();
        while (ConstraintsChanged) {
            ConstraintsChanged = RunRules();
            // int newSize = Constraints.Constraints.Count();
            // if (size == newSize && ConstraintsChanged)
            //     throw new Exception("HashSet Add Broken...");
            // size = newSize;
            // if (size > 8) {
            //     StringBuilder b = new StringBuilder();
            //     foreach (var c in Constraints.Constraints) {
            //         b.AppendLine(c.ToString());
            //     }
            //     throw new Exception("Large constraint set found (> 8): \n" + b.ToString());
            // }
        }
        RunRules();
        return this;
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

    //Maintain partial solution(s) throughout resolution? 
    protected ImmutableDictionary<ObjectInference.Var, ImmutableHashSet<AbstractObjID>> ReadObjSolution() {
        IEnumerable<InferenceConstraint.ObjectInclusion> objIncl = Constraints.ObjectInclusions.Where(c => c.l is ObjectInference.Literal && c.r is ObjectInference.Var);

        Dictionary<ObjectInference.Var, HashSet<AbstractObjID>> ObjSol = new Dictionary<ObjectInference.Var, HashSet<int>>();

        foreach (var c in objIncl) {
            ObjectInference.Var k = (ObjectInference.Var)c.r;
            ObjectInference.Literal v = (ObjectInference.Literal)c.l;

            if (!ObjSol.ContainsKey(k))
                ObjSol.Add(k, new HashSet<int>());

            foreach (var o in v.Objects) {
                ObjSol[k].Add(o);
            }
        }

        return [.. ObjSol.Select(kv => new KeyValuePair<ObjectInference.Var, ImmutableHashSet<AbstractObjID>>(kv.Key, [.. kv.Value]))];
    }

    protected ImmutableDictionary<TypeInference.Var, ImmutableHashSet<Types.Type>> ReadTypeSolution() {
        IEnumerable<InferenceConstraint.SubTyping> subTy = Constraints.SubTypings.Where(c => c.l is TypeInference.Literal && c.r is TypeInference.Var);

        Dictionary<TypeInference.Var, HashSet<Types.Type>> TypeSol = new Dictionary<TypeInference.Var, HashSet<Types.Type>>();

        foreach (var c in subTy) {
            TypeInference.Var k = (TypeInference.Var)c.r;
            TypeInference.Literal v = (TypeInference.Literal)c.l;

            if (!TypeSol.ContainsKey(k))
                TypeSol.Add(k, new HashSet<Types.Type>());

            foreach (var o in v.Types) {
                TypeSol[k].Add(o);
            }
        }

        return [..TypeSol.Select(kv => new KeyValuePair<TypeInference.Var, ImmutableHashSet<Types.Type>>(kv.Key, [.. kv.Value]))];
    }

    protected ImmutableDictionary<AliasInference.Var, AliasFlag> ReadAliasSolution() {
        IEnumerable<InferenceConstraint.AliasBounding> als = Constraints.AliasBoundings.Where(c => c.l is AliasInference.Literal && c.r is AliasInference.Var);

        Dictionary<AliasInference.Var, AliasFlag> AliasSol = new Dictionary<AliasInference.Var, AliasFlag>();

        foreach (var c in als) {
            AliasInference.Var k = (AliasInference.Var)c.r;
            AliasInference.Literal v = (AliasInference.Literal)c.l;

            if (!AliasSol.ContainsKey(k))
                AliasSol.Add(k, v.Flag.Flag);
            else {
                if (AliasSol[k] == AliasFlag.S && v.Flag.Flag == AliasFlag.M)
                    AliasSol[k] = v.Flag.Flag;
            }
        }

        return [.. AliasSol];
    }

    public InferenceVariableSolution ReadSolutionFromConstraints() => new InferenceVariableSolution(
            ReadObjSolution(),
            ReadTypeSolution(),
            ReadAliasSolution()
        );

    public bool IsTypeSafe() => Constraints.PartialOrders.Any(c => c.IsTrivialUnsat(Delta));

    public IEnumerable<AnalysisError> GetAnalysisErrors() {
        InferenceVariableSolution Sol = ReadSolutionFromConstraints();
        if (!IsTypeSafe()) {
            //Make & return error message
            ImmutableHashSet<InferenceConstraint.PartialOrder> UnSatIncl = [.. UnSatConstraints(Sol).Select(c => c.Reduce())];

            foreach (InferenceConstraint.PartialOrder po in UnSatIncl) {
                if (po is InferenceConstraint.ObjectInclusion c && c.l is ObjectInference.Literal ll && c.r is ObjectInference.Literal rr && rr.CodeSource.HasValue) {
                    foreach (int o in ll.Objects) {
                        SyntaxNode Source = Delta.AbstractObjectIDsToCodeLocations[o];
                        yield return new AnalysisError(Source.SyntaxTree.FilePath, Source, rr.CodeSource.Value.Item1, rr.CodeSource.Value.Item2);
                    }
                }
                else if (po is InferenceConstraint.SubTyping tc && tc.l is TypeInference.Literal tll && tc.r is TypeInference.Literal trr && tll.CodeSource.HasValue && trr.CodeSource.HasValue) {
                    foreach (Types.Type t in tll.Types) {
                        yield return new AnalysisError(tll.CodeSource.Value.Item1, tll.CodeSource.Value.Item2, trr.CodeSource.Value.Item1, trr.CodeSource.Value.Item2);
                    }
                    // } else if (po is InferenceConstraint.AliasBounding ac && ac.l is AliasInference.Literal all && ac.r is TypeInference.Literal arr && all.CodeSource.HasValue && arr.CodeSource.HasValue) {  
                }
                else {
                    throw new Exception($"Found type error but constraint form was incorrect: {po}.");
                }
            }
        }
    }
}
