using Analysis.Types;
using Environment = Analysis.Types.Environment;
using AnalysisTests.Util;
using Analysis;
using System.Collections.Immutable;

namespace AnalysisTests.DataTests.Constraints;


[TestClass]
[TestCategory("Resolution")]
public sealed class Resolution {

    [TestMethod]
    [TestCategory("Constraint")]
    [TestCategory("ObjectInclusion")]
    [TestCategory("Transitivity")]
    public void ObjectInclusion_Var_Transitivity() {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        AbstractObjectIDAssigner Delta = new AbstractObjectIDAssigner(null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        InferenceConstraint.ObjectInclusion c1 = new InferenceConstraint.ObjectInclusion(new ObjectInference.Var(0), new ObjectInference.Var(1));
        InferenceConstraint.ObjectInclusion c2 = new InferenceConstraint.ObjectInclusion(new ObjectInference.Var(1), new ObjectInference.Var(2));
        InferenceConstraint.ObjectInclusion c3 = new InferenceConstraint.ObjectInclusion(new ObjectInference.Var(0), new ObjectInference.Var(2));

        ConstraintSolver solver = new ConstraintSolver(Delta, [c1, c2]);

        Assert.HasCount(2, solver.InferenceConstraints);
        Assert.Contains(c1, solver.InferenceConstraints);
        Assert.Contains(c2, solver.InferenceConstraints);

        solver.FindFixpoint();

        Assert.HasCount(3, solver.InferenceConstraints);
        Assert.Contains(c1, solver.InferenceConstraints);
        Assert.Contains(c2, solver.InferenceConstraints);
        Assert.Contains(c3, solver.InferenceConstraints);
    }

    [TestMethod]
    [TestCategory("Constraint")]
    [TestCategory("ObjectInclusion")]
    [TestCategory("Transitivity")]
    public void ObjectInclusion_Literal_Transitivity() {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        AbstractObjectIDAssigner Delta = new AbstractObjectIDAssigner(null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        InferenceConstraint.ObjectInclusion c1 = new InferenceConstraint.ObjectInclusion(new ObjectInference.Var(0), new ObjectInference.Var(1));
        InferenceConstraint.ObjectInclusion c2 = new InferenceConstraint.ObjectInclusion(new ObjectInference.Var(1), new ObjectInference.Literal([0]));
        InferenceConstraint.ObjectInclusion c3 = new InferenceConstraint.ObjectInclusion(new ObjectInference.Var(0), new ObjectInference.Literal([0]));

        ConstraintSolver solver = new ConstraintSolver(Delta, [c1, c2]);

        Assert.HasCount(2, solver.InferenceConstraints);
        Assert.Contains(c1, solver.InferenceConstraints);
        Assert.Contains(c2, solver.InferenceConstraints);

        solver.FindFixpoint();

        Assert.HasCount(3, solver.InferenceConstraints);
        Assert.Contains(c1, solver.InferenceConstraints);
        Assert.Contains(c2, solver.InferenceConstraints);
        Assert.Contains(c3, solver.InferenceConstraints);
    }

    [TestMethod]
    [TestCategory("Constraint")]
    [TestCategory("ObjectInclusion")]
    [TestCategory("Transitivity")]
    [DynamicData(nameof(DataSources.TripleObjectInf), typeof(DataSources))]
    public void ObjectInclusion_Mixed_Transitivity(ObjectInference i1, ObjectInference i2, ObjectInference i3) {
        if (i1.Equals(i2) || i1.Equals(i3) || i2.Equals(i3))
            return;

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        AbstractObjectIDAssigner Delta = new AbstractObjectIDAssigner(null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        InferenceConstraint.ObjectInclusion c1 = new InferenceConstraint.ObjectInclusion(i1, i2);
        InferenceConstraint.ObjectInclusion c2 = new InferenceConstraint.ObjectInclusion(i2, i3);
        InferenceConstraint.ObjectInclusion c3 = new InferenceConstraint.ObjectInclusion(i1, i3);

        ConstraintSolver solver = new ConstraintSolver(Delta, [c1, c2]);

        Assert.HasCount(2, solver.InferenceConstraints);
        Assert.Contains(c1, solver.InferenceConstraints);
        Assert.Contains(c2, solver.InferenceConstraints);

        solver.FindFixpoint();

        Assert.HasCount(3, solver.InferenceConstraints);
        Assert.Contains(c1, solver.InferenceConstraints);
        Assert.Contains(c2, solver.InferenceConstraints);
        Assert.Contains(c3, solver.InferenceConstraints);
    }

    [TestMethod]
    [TestCategory("Constraint")]
    [TestCategory("SubTyping")]
    [TestCategory("Transitivity")]
    [DynamicData(nameof(DataSources.TripleTypeInf), typeof(DataSources))]
    public void SubTyping_Mixed_Transitivity(TypeInference i1, TypeInference i2, TypeInference i3) {
        if (i1.Equals(i2) || i1.Equals(i3) || i2.Equals(i3))
            return;

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        AbstractObjectIDAssigner Delta = new AbstractObjectIDAssigner(null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        InferenceConstraint.SubTyping c1 = new InferenceConstraint.SubTyping(i1, i2);
        InferenceConstraint.SubTyping c2 = new InferenceConstraint.SubTyping(i2, i3);
        InferenceConstraint.SubTyping c3 = new InferenceConstraint.SubTyping(i1, i3);

        ConstraintSolver solver = new ConstraintSolver(Delta, [c1, c2]);

        Assert.HasCount(2, solver.InferenceConstraints);
        Assert.Contains(c1, solver.InferenceConstraints);
        Assert.Contains(c2, solver.InferenceConstraints);

        solver.FindFixpoint();

        Assert.HasCount(3, solver.InferenceConstraints);
        Assert.Contains(c1, solver.InferenceConstraints);
        Assert.Contains(c2, solver.InferenceConstraints);
        Assert.Contains(c3, solver.InferenceConstraints);
    }

    [TestMethod]
    [TestCategory("Constraint")]
    [TestCategory("AliasBounding")]
    [TestCategory("Transitivity")]
    [DynamicData(nameof(DataSources.TripleAliasInf), typeof(DataSources))]
    public void AliasBounding_Mixed_Transitivity(AliasInference i1, AliasInference i2, AliasInference i3) {
        if (i1.Equals(i2) || i1.Equals(i3) || i2.Equals(i3))
            return;

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        AbstractObjectIDAssigner Delta = new AbstractObjectIDAssigner(null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        InferenceConstraint.AliasBounding c1 = new InferenceConstraint.AliasBounding(i1, i2);
        InferenceConstraint.AliasBounding c2 = new InferenceConstraint.AliasBounding(i2, i3);
        InferenceConstraint.AliasBounding c3 = new InferenceConstraint.AliasBounding(i1, i3);

        ConstraintSolver solver = new ConstraintSolver(Delta, [c1, c2]);

        Assert.HasCount(2, solver.InferenceConstraints);
        Assert.Contains(c1, solver.InferenceConstraints);
        Assert.Contains(c2, solver.InferenceConstraints);

        solver.FindFixpoint();

        Assert.HasCount(3, solver.InferenceConstraints);
        Assert.Contains(c1, solver.InferenceConstraints);
        Assert.Contains(c2, solver.InferenceConstraints);
        Assert.Contains(c3, solver.InferenceConstraints);
    }


    [TestMethod]
    [TestCategory("Constraint")]
    [TestCategory("SubTyping")]
    [TestCategory("Conditional")]
    [TestCategory("Satisfaction")]
    public void Satisfaction_Type() {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        AbstractObjectIDAssigner Delta = new AbstractObjectIDAssigner(null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

        InferenceConstraint.SubTyping t = new InferenceConstraint.SubTyping(new TypeInference.Var(0), new TypeInference.Var(1));
        InferenceConstraint ts = new InferenceConstraint.ObjectInclusion(new ObjectInference.Var(0), new ObjectInference.Var(1));

        InferenceConstraint.Conditional c = new InferenceConstraint.Conditional([t], [], [ts]);

        ConstraintSolver solver = new ConstraintSolver(Delta, [t, c]);

        Assert.HasCount(2, solver.InferenceConstraints);
        Assert.Contains(t, solver.InferenceConstraints);
        Assert.Contains(c, solver.InferenceConstraints);

        solver.FindFixpoint();

        Assert.HasCount(4, solver.InferenceConstraints);
        Assert.HasCount(1, solver.Constraints.ObjectInclusions);
        Assert.HasCount(1, solver.Constraints.SubTypings);
        Assert.HasCount(2, solver.Constraints.Conditionals);
        Assert.Contains(t, solver.InferenceConstraints);
        Assert.Contains(c, solver.InferenceConstraints);
        Assert.Contains(ts, solver.InferenceConstraints);
    }


    [TestMethod]
    [TestCategory("Constraint")]
    [TestCategory("SubTyping")]
    [TestCategory("Conditional")]
    [TestCategory("Satisfaction")]
    [DynamicData(nameof(DataSources.TypeConditional), typeof(DataSources))]
    public void Satisfaction_Type(InferenceConstraint.SubTyping t, InferenceConstraint ts) {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        AbstractObjectIDAssigner Delta = new AbstractObjectIDAssigner(null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

        InferenceConstraint.Conditional c = new InferenceConstraint.Conditional([t], [], [ts]);

        ConstraintSolver solver = new ConstraintSolver(Delta, [t, c]);

        if (t.Equals(ts) || t.Equals(c) || ts.Equals(c))
            return;

        Assert.HasCount(2, solver.InferenceConstraints);
        Assert.Contains(t, solver.InferenceConstraints);
        Assert.Contains(c, solver.InferenceConstraints);

        solver.FindFixpoint();

        Assert.HasCount(t.l.Equals(t.r) ? 3 : 4, solver.InferenceConstraints, solver.PrintConstraints());
        Assert.Contains(t, solver.InferenceConstraints);
        Assert.Contains(c, solver.InferenceConstraints);
        Assert.Contains(ts, solver.InferenceConstraints);
    }


    [TestMethod]
    [TestCategory("Constraint")]
    [TestCategory("AliasBounding")]
    [TestCategory("Conditional")]
    [TestCategory("Satisfaction")]
    public void Satisfaction_Weakening_Alias() {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        AbstractObjectIDAssigner Delta = new AbstractObjectIDAssigner(null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

        InferenceConstraint.AliasBounding t = new InferenceConstraint.AliasBounding(new AliasInference.Var(0), new AliasInference.Var(1));
        InferenceConstraint ts = new InferenceConstraint.ObjectInclusion(new ObjectInference.Var(0), new ObjectInference.Var(1));

        InferenceConstraint.Conditional c = new InferenceConstraint.Conditional([], [t], [ts]);

        ConstraintSolver solver = new ConstraintSolver(Delta, [t, c]);

        Assert.HasCount(2, solver.InferenceConstraints);
        Assert.Contains(t, solver.InferenceConstraints);
        Assert.Contains(c, solver.InferenceConstraints);

        solver.FindFixpoint();

        Assert.HasCount(4, solver.InferenceConstraints);
        Assert.HasCount(1, solver.Constraints.ObjectInclusions);
        Assert.HasCount(1, solver.Constraints.AliasBoundings);
        Assert.HasCount(2, solver.Constraints.Conditionals);
        Assert.Contains(t, solver.InferenceConstraints);
        Assert.Contains(c, solver.InferenceConstraints);
        Assert.Contains(ts, solver.InferenceConstraints);
    }


    [TestMethod]
    [TestCategory("Constraint")]
    [TestCategory("AliasBounding")]
    [TestCategory("Conditional")]
    [TestCategory("Satisfaction")]
    [DynamicData(nameof(DataSources.AliasConditional), typeof(DataSources))]
    public void Satisfaction_Weakening_Alias(InferenceConstraint.AliasBounding t, InferenceConstraint ts) {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        AbstractObjectIDAssigner Delta = new AbstractObjectIDAssigner(null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

        InferenceConstraint.Conditional c = new InferenceConstraint.Conditional([], [t], [ts]);

        ConstraintSolver solver = new ConstraintSolver(Delta, [t, c]);

        if (t.Equals(ts) || t.Equals(c) || ts.Equals(c))
            return;

        Assert.HasCount(2, solver.InferenceConstraints);
        Assert.Contains(t, solver.InferenceConstraints);
        Assert.Contains(c, solver.InferenceConstraints);

        solver.FindFixpoint();

        Assert.HasCount(t.l.Equals(t.r) ? 3 : 4, solver.InferenceConstraints, solver.PrintConstraints());
        Assert.Contains(t, solver.InferenceConstraints);
        Assert.Contains(c, solver.InferenceConstraints);
        Assert.Contains(ts, solver.InferenceConstraints);
    }

    [TestMethod]
    [TestCategory("Constraint")]
    [TestCategory("AliasBounding")]
    [TestCategory("Conditional")]
    [TestCategory("Satisfaction")]
    [DynamicData(nameof(DataSources.TypeAliasConditional), typeof(DataSources))]
    public void Satisfaction_Weakening_Mixed(InferenceConstraint.SubTyping tt, InferenceConstraint.AliasBounding ta, InferenceConstraint ts) {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        AbstractObjectIDAssigner Delta = new AbstractObjectIDAssigner(null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

        InferenceConstraint.Conditional c = new InferenceConstraint.Conditional([tt], [ta], [ts]);

        InferenceConstraint.SubTyping tw = new InferenceConstraint.SubTyping(tt.l, tt.l);
        InferenceConstraint.AliasBounding aw = new InferenceConstraint.AliasBounding(ta.l, ta.l);

        ConstraintSolver solver = new ConstraintSolver(Delta, [tt, ta, c]);

        if (tt.Equals(ts) || tt.Equals(c) || ts.Equals(c) || ta.Equals(ts) || ta.Equals(c) || tt.l.Equals(tt.r) || ta.l.Equals(ta.r))
            return;

        Assert.HasCount(3, solver.InferenceConstraints);
        Assert.Contains(tt, solver.InferenceConstraints);
        Assert.Contains(ta, solver.InferenceConstraints);
        Assert.Contains(c, solver.InferenceConstraints);

        solver.FindFixpoint();

        Assert.Contains(tt, solver.InferenceConstraints);
        Assert.Contains(ta, solver.InferenceConstraints);
        Assert.Contains(ts, solver.InferenceConstraints);

        //When satisfaction, do all satisfaction; When weaken, don't necisarily weaken all.
        Assert.Contains(c, solver.InferenceConstraints);
        Assert.Contains(new InferenceConstraint.Conditional([tt], [aw], [ts]), solver.InferenceConstraints);
        // Assert.Contains(new InferenceConstraint.Conditional([tt], [], [ts]), solver.InferenceConstraints);
        Assert.Contains(new InferenceConstraint.Conditional([tw], [ta], [ts]), solver.InferenceConstraints);
        Assert.Contains(new InferenceConstraint.Conditional([tw], [aw], [ts]), solver.InferenceConstraints);
        Assert.Contains(new InferenceConstraint.Conditional([tw], [], [ts]), solver.InferenceConstraints);
        // Assert.Contains(new InferenceConstraint.Conditional([], [ta], [ts]), solver.InferenceConstraints);
        Assert.Contains(new InferenceConstraint.Conditional([], [aw], [ts]), solver.InferenceConstraints);

        Assert.HasCount(9, solver.InferenceConstraints, solver.PrintConstraints());

    }


    [TestMethod]
    [TestCategory("Constraint")]
    [TestCategory("HeapUpdate")]
    public void HeapUpdate_SU() {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        AbstractObjectIDAssigner Delta = new AbstractObjectIDAssigner(null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

        int obj1 = 0;

        AliasInference obj1AliasLit = new AliasInference.Literal(new AliasData(AliasFlag.S));
        AliasInference obj1AliasVar = new AliasInference.Var(0);
        AliasInference obj1AliasVarOut = new AliasInference.Var(1);
        Class obj1ClassC = new Class("C");

        ObjectInference.Var o1 = new ObjectInference.Var(1); //ObjIn
        ObjectInference.Var o2 = new ObjectInference.Var(2); //Old mapping
        ObjectInference.Var o3 = new ObjectInference.Var(3); //New mapping
        ObjectInference.Var o4 = new ObjectInference.Var(4); //New mapping var (Post: o3 <= o4)

        // [];[(0, f) -> O2];[0 -> C];[0 -> A1]
        Environment In = new Environment(
            new StackEnv([]),
            new HeapEnv(ImmutableDictionary<(int, string), ObjectInference>.Empty.Add((obj1, "f"), o2)),
            new TypeEnv(ImmutableDictionary<int, Class>.Empty.Add(obj1, obj1ClassC), []),
            new AliasEnv(ImmutableDictionary<int, AliasInference>.Empty.Add(obj1, obj1AliasVar))
        );
        // [];[(0, f) -> O4];[0 -> C];[0 -> A1]
        Environment Out = new Environment(
            new StackEnv([]),
            new HeapEnv(ImmutableDictionary<(int, string), ObjectInference>.Empty.Add((obj1, "f"), o4)),
            new TypeEnv(ImmutableDictionary<int, Class>.Empty.Add(obj1, obj1ClassC), []),
            new AliasEnv(ImmutableDictionary<int, AliasInference>.Empty.Add(obj1, obj1AliasVarOut))
        );

        InferenceConstraint.HeapUpdate hu = new InferenceConstraint.HeapUpdate(Out, In, o1, "f", o3);
        InferenceConstraint objIncl = (ObjectInference)new ObjectInference.Literal([0]) <= o1;
        InferenceConstraint AliasBound1 = obj1AliasLit <= obj1AliasVar;
        InferenceConstraint AliasBound2 = obj1AliasVar <= obj1AliasLit;

        IEnumerable<InferenceConstraint> cons = [
                hu,
                objIncl,
                AliasBound1, AliasBound2
            ];

        ConstraintSolver solver = new ConstraintSolver(Delta, cons);

        Assert.HasCount(4, solver.Constraints.Constraints);
        Assert.Contains(hu, solver.InferenceConstraints);
        Assert.Contains(objIncl, solver.InferenceConstraints);
        Assert.Contains(AliasBound1, solver.InferenceConstraints);
        Assert.Contains(AliasBound2, solver.InferenceConstraints);

        solver.FindFixpoint();

        Assert.HasCount(7, solver.Constraints.Constraints, solver.PrintConstraints());
    }


    [TestMethod]
    [TestCategory("Constraint")]
    [TestCategory("HeapUpdate")]
    public void HeapUpdate_WU_MBound() {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        AbstractObjectIDAssigner Delta = new AbstractObjectIDAssigner(null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

        int obj1 = 0;

        AliasInference obj1AliasLit = new AliasInference.Literal(new AliasData(AliasFlag.M));
        AliasInference obj1AliasVar = new AliasInference.Var(0);
        AliasInference obj1AliasVarOut = new AliasInference.Var(1);
        Class obj1ClassC = new Class("C");

        ObjectInference.Var o1 = new ObjectInference.Var(1); //ObjIn
        ObjectInference.Var o2 = new ObjectInference.Var(2); //Old mapping
        ObjectInference.Var o3 = new ObjectInference.Var(3); //New mapping
        ObjectInference.Var o4 = new ObjectInference.Var(4); //New mapping var (Post: o3 <= o4)

        // [];[(0, f) -> O2];[0 -> C];[0 -> A1]
        Environment In = new Environment(
            new StackEnv([]),
            new HeapEnv(ImmutableDictionary<(int, string), ObjectInference>.Empty.Add((obj1, "f"), o2)),
            new TypeEnv(ImmutableDictionary<int, Class>.Empty.Add(obj1, obj1ClassC), []),
            new AliasEnv(ImmutableDictionary<int, AliasInference>.Empty.Add(obj1, obj1AliasVar))
        );
        // [];[(0, f) -> O4];[0 -> C];[0 -> A1]
        Environment Out = new Environment(
            new StackEnv([]),
            new HeapEnv(ImmutableDictionary<(int, string), ObjectInference>.Empty.Add((obj1, "f"), o4)),
            new TypeEnv(ImmutableDictionary<int, Class>.Empty.Add(obj1, obj1ClassC), []),
            new AliasEnv(ImmutableDictionary<int, AliasInference>.Empty.Add(obj1, obj1AliasVarOut))
        );

        InferenceConstraint.HeapUpdate hu = new InferenceConstraint.HeapUpdate(Out, In, o1, "f", o3);
        InferenceConstraint objIncl = (ObjectInference)new ObjectInference.Literal([0]) <= o1;
        InferenceConstraint AliasBound1 = obj1AliasLit <= obj1AliasVar;
        InferenceConstraint AliasBound2 = obj1AliasVar <= obj1AliasLit;

        IEnumerable<InferenceConstraint> cons = [
                hu,
                objIncl,
                AliasBound1, AliasBound2
            ];

        ConstraintSolver solver = new ConstraintSolver(Delta, cons);

        Assert.HasCount(4, solver.Constraints.Constraints);
        Assert.Contains(hu, solver.InferenceConstraints);
        Assert.Contains(objIncl, solver.InferenceConstraints);
        Assert.Contains(AliasBound1, solver.InferenceConstraints);
        Assert.Contains(AliasBound2, solver.InferenceConstraints);

        solver.FindFixpoint();

        Assert.HasCount(8, solver.Constraints.Constraints, solver.PrintConstraints());
    }


    [TestMethod]
    [TestCategory("Constraint")]
    [TestCategory("HeapUpdate")]
    public void HeapUpdate_WU_PassThrough() {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        AbstractObjectIDAssigner Delta = new AbstractObjectIDAssigner(null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

        int obj1 = 0;
        int obj2 = 1;

        //AliasInference obj1AliasLit = new AliasInference.Literal(new AliasData(AliasFlag.M));
        AliasInference obj1AliasVar = new AliasInference.Var(0);
        AliasInference obj1AliasVarOut = new AliasInference.Var(1);
        Class obj1ClassC = new Class("C");

        ObjectInference.Var o1 = new ObjectInference.Var(1); //ObjIn
        ObjectInference.Var o2 = new ObjectInference.Var(2); //Old (0, f) mapping
        ObjectInference.Var o3 = new ObjectInference.Var(3); //New (0, f) mapping
        ObjectInference.Var o4 = new ObjectInference.Var(4); //New (0, f) mapping var (Post: o3 <= o4)
        ObjectInference.Var o5 = new ObjectInference.Var(5); //Old (1, f) mapping
        ObjectInference.Var o6 = new ObjectInference.Var(6); //New (1, f) mapping
        ObjectInference.Var o7 = new ObjectInference.Var(7); //Old (1, g) mapping
        ObjectInference.Var o8 = new ObjectInference.Var(8); //New (1, g) mapping

        // [];[(0, f) -> O2, (1, f) -> O5, (1, g) -> O7];[0 -> C];[0 -> A1]
        Environment In = new Environment(
            new StackEnv([]),
            new HeapEnv(ImmutableDictionary<(int, string), ObjectInference>.Empty.Add((obj1, "f"), o2).Add((obj2, "f"), o5).Add((obj2, "g"), o7)),
            new TypeEnv(ImmutableDictionary<int, Class>.Empty.Add(obj1, obj1ClassC), []),
            new AliasEnv(ImmutableDictionary<int, AliasInference>.Empty.Add(obj1, obj1AliasVar))
        );
        // [];[(0, f) -> O4, (1, f) -> O6, (1, g) -> O7];[0 -> C];[0 -> A2]
        Environment Out = new Environment(
            new StackEnv([]),
            new HeapEnv(ImmutableDictionary<(int, string), ObjectInference>.Empty.Add((obj1, "f"), o4).Add((obj2, "f"), o6).Add((obj2, "g"), o8)),
            new TypeEnv(ImmutableDictionary<int, Class>.Empty.Add(obj1, obj1ClassC), []),
            new AliasEnv(ImmutableDictionary<int, AliasInference>.Empty.Add(obj1, obj1AliasVarOut))
        );

        InferenceConstraint.HeapUpdate hu = new InferenceConstraint.HeapUpdate(Out, In, o1, "f", o3);
        InferenceConstraint objIncl = (ObjectInference)new ObjectInference.Literal([0]) <= o1;
        //InferenceConstraint AliasBound1 = obj1AliasLit <= obj1AliasVar;
        //InferenceConstraint AliasBound2 = obj1AliasVar <= obj1AliasLit;

        IEnumerable<InferenceConstraint> cons = [
                hu,
                objIncl,
                //AliasBound1, AliasBound2
            ];

        ConstraintSolver solver = new ConstraintSolver(Delta, cons);

        Assert.HasCount(2, solver.Constraints.Constraints);
        Assert.Contains(hu, solver.InferenceConstraints);
        Assert.Contains(objIncl, solver.InferenceConstraints);
        //Assert.Contains(AliasBound1, solver.InferenceConstraints);
        //Assert.Contains(AliasBound2, solver.InferenceConstraints);

        solver.FindFixpoint();

        Assert.HasCount(5, solver.Constraints.Constraints, solver.PrintConstraints());
    }


    [TestMethod]
    [TestCategory("Constraint")]
    [TestCategory("HeapLookup")]
    public void HeapLookup() {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        AbstractObjectIDAssigner Delta = new AbstractObjectIDAssigner(null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

        int obj1 = 0;

        AliasInference obj1AliasVar = new AliasInference.Var(0);
        AliasInference obj1AliasVarOut = new AliasInference.Var(1);
        Class obj1ClassC = new Class("C");

        ObjectInference.Var o1 = new ObjectInference.Var(1); //ObjIn
        ObjectInference.Var o2 = new ObjectInference.Var(2); //ObjOut
        ObjectInference.Var o3 = new ObjectInference.Var(3); //New (0, f) mapping

        // [];[(0, f) -> O3];[0 -> C];[0 -> A1]
        Environment In = new Environment(
            new StackEnv([]),
            new HeapEnv(ImmutableDictionary<(int, string), ObjectInference>.Empty.Add((obj1, "f"), o3)),
            new TypeEnv(ImmutableDictionary<int, Class>.Empty.Add(obj1, obj1ClassC), []),
            new AliasEnv(ImmutableDictionary<int, AliasInference>.Empty.Add(obj1, obj1AliasVar))
        );

        InferenceConstraint.HeapLookup hl = new InferenceConstraint.HeapLookup(o2, In, o1, "f");
        InferenceConstraint objIncl = (ObjectInference)new ObjectInference.Literal([0]) <= o1;

        IEnumerable<InferenceConstraint> cons = [
                hl,
                objIncl
            ];

        ConstraintSolver solver = new ConstraintSolver(Delta, cons);

        Assert.HasCount(2, solver.Constraints.Constraints);
        Assert.Contains(hl, solver.InferenceConstraints);
        Assert.Contains(objIncl, solver.InferenceConstraints);

        solver.FindFixpoint();

        Assert.HasCount(3, solver.Constraints.Constraints, solver.PrintConstraints());
        Assert.Contains(hl, solver.InferenceConstraints);
        Assert.Contains(objIncl, solver.InferenceConstraints);
        Assert.Contains((ObjectInference)o3 <= o2, solver.InferenceConstraints);
    }


    [TestMethod]
    [TestCategory("Constraint")]
    [TestCategory("TypeLookup")]
    public void TypeLookup() {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        AbstractObjectIDAssigner Delta = new AbstractObjectIDAssigner(null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

        int obj1 = 0;

        AliasInference obj1AliasVar = new AliasInference.Var(0);
        AliasInference obj1AliasVarOut = new AliasInference.Var(1);
        Class obj1ClassC = new Class("C");

        ObjectInference.Var o1 = new ObjectInference.Var(1); //ObjIn
        ObjectInference.Var o2 = new ObjectInference.Var(2); //ObjOut
        ObjectInference.Var o3 = new ObjectInference.Var(3); //New (0, f) mapping
        TypeInference.Var tOut = new TypeInference.Var(0);

        // [];[(0, f) -> O3];[0 -> C];[0 -> A1]
        Environment In = new Environment(
            new StackEnv([]),
            new HeapEnv(ImmutableDictionary<(int, string), ObjectInference>.Empty.Add((obj1, "f"), o3)),
            new TypeEnv(ImmutableDictionary<int, Class>.Empty.Add(obj1, obj1ClassC), []),
            new AliasEnv(ImmutableDictionary<int, AliasInference>.Empty.Add(obj1, obj1AliasVar))
        );

        InferenceConstraint.TypeLookup tl = new InferenceConstraint.TypeLookup(tOut, In, o1);
        InferenceConstraint objIncl = (ObjectInference)new ObjectInference.Literal([0]) <= o1;

        IEnumerable<InferenceConstraint> cons = [
                tl,
                objIncl
            ];

        ConstraintSolver solver = new ConstraintSolver(Delta, cons);

        Assert.HasCount(2, solver.Constraints.Constraints);
        Assert.Contains(tl, solver.InferenceConstraints);
        Assert.Contains(objIncl, solver.InferenceConstraints);

        solver.FindFixpoint();

        InferenceConstraint.SubTyping tOutIncl = new InferenceConstraint.SubTyping(new TypeInference.Literal([obj1ClassC]), tOut);

        // Assert.HasCount(3, solver.Constraints.Constraints, solver.PrintConstraints()); // Contains 2 copies of 'tOutIncl'; Both are counted here despite being equal?
        Assert.Contains(tl, solver.InferenceConstraints);
        Assert.Contains(objIncl, solver.InferenceConstraints);
        Assert.Contains(tOutIncl, solver.InferenceConstraints);

        solver.Constraints.SubTypings.Remove(tOutIncl);

        Assert.DoesNotContain(tOutIncl, solver.InferenceConstraints);
        Assert.HasCount(2, solver.Constraints.Constraints, solver.PrintConstraints());
    }

}
