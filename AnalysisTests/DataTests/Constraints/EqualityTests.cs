using Analysis.Types;
using Environment = Analysis.Types.Environment;
using AnalysisTests.Util;
using System.Collections.Immutable;

namespace AnalysisTests.DataTests.Constraints;


[TestClass]
[TestCategory("Equality")]
public sealed class EqualityTests { //TODO: Finish constraint types
    [TestMethod]
    [TestCategory("Constraint")]
    [TestCategory("ObjectInclusion")]
    [DynamicData(nameof(DataSources.PairObjectInf), typeof(DataSources))]
    public void ObjectInclusion_True(ObjectInference l, ObjectInference r) {
        InferenceConstraint.ObjectInclusion lc = new InferenceConstraint.ObjectInclusion(l, r);
        InferenceConstraint.ObjectInclusion rc = new InferenceConstraint.ObjectInclusion(l, r);

        Assert.AreEqual(lc, rc);
        Assert.AreEqual(lc.GetHashCode(), rc.GetHashCode());
    }

    [TestMethod]
    [TestCategory("Constraint")]
    [TestCategory("ObjectInclusion")]
    [DynamicData(nameof(DataSources.PairObjectInclusions), typeof(DataSources))]
    public void ObjectInclusion_Mixed(InferenceConstraint.ObjectInclusion l, InferenceConstraint.ObjectInclusion r) {
        if (l.l.Equals(r.l) && l.r.Equals(r.r)) {
            Assert.AreEqual(l, r);
            Assert.AreEqual(l.GetHashCode(), r.GetHashCode());
        }
        else
            Assert.AreNotEqual(l, r);
    }


    [TestMethod]
    [TestCategory("Constraint")]
    [TestCategory("AliasBounding")]
    [DynamicData(nameof(DataSources.PairAliasInf), typeof(DataSources))]
    public void AliasBounding_True(AliasInference l, AliasInference r) {
        InferenceConstraint.AliasBounding lc = new InferenceConstraint.AliasBounding(l, r);
        InferenceConstraint.AliasBounding rc = new InferenceConstraint.AliasBounding(l, r);

        Assert.AreEqual(lc, rc);
        Assert.AreEqual(lc.GetHashCode(), rc.GetHashCode());
    }

    [TestMethod]
    [TestCategory("Constraint")]
    [TestCategory("AliasBounding")]
    [DynamicData(nameof(DataSources.PairAliasBounds), typeof(DataSources))]
    public void AliasBounding_Mixed(InferenceConstraint.AliasBounding l, InferenceConstraint.AliasBounding r) {
        if (l.l.Equals(r.l) && l.r.Equals(r.r)) {
            Assert.AreEqual(l, r);
            Assert.AreEqual(l.GetHashCode(), r.GetHashCode());
        }
        else
            Assert.AreNotEqual(l, r);
    }


    [TestMethod]
    [TestCategory("Constraint")]
    [TestCategory("SubTyping")]
    [DynamicData(nameof(DataSources.PairTypeInf), typeof(DataSources))]
    public void SubTyping_True(TypeInference l, TypeInference r) {
        InferenceConstraint.SubTyping lc = new InferenceConstraint.SubTyping(l, r);
        InferenceConstraint.SubTyping rc = new InferenceConstraint.SubTyping(l, r);

        Assert.AreEqual(lc, rc);
        Assert.AreEqual(lc.GetHashCode(), rc.GetHashCode());
    }

    [TestMethod]
    [TestCategory("Constraint")]
    [TestCategory("SubTyping")]
    public void SubTyping_Var01_Double() {
        InferenceConstraint.SubTyping lc = new InferenceConstraint.SubTyping(new TypeInference.Var(0), new TypeInference.Var(1));
        InferenceConstraint.SubTyping rc = new InferenceConstraint.SubTyping(new TypeInference.Var(0), new TypeInference.Var(1));

        Assert.AreEqual(lc, rc);
        Assert.AreEqual(lc.GetHashCode(), rc.GetHashCode());
    }

    [TestMethod]
    [TestCategory("Constraint")]
    [TestCategory("SubTyping")]
    [DynamicData(nameof(DataSources.PairSubTypings), typeof(DataSources))]
    public void SubTyping_Mixed(InferenceConstraint.SubTyping l, InferenceConstraint.SubTyping r) {
        if (l.l.Equals(r.l) && l.r.Equals(r.r)) {
            Assert.AreEqual(l, r);
            Assert.AreEqual(l.GetHashCode(), r.GetHashCode());
        }
        else
            Assert.AreNotEqual(l, r);
    }

    [TestMethod]
    [TestCategory("Constraint")]
    [TestCategory("SubTyping")]
    [DynamicData(nameof(DataSources.PairSubTypings), typeof(DataSources))]
    public void SubTyping_HashSet_Mixed(InferenceConstraint.SubTyping l, InferenceConstraint.SubTyping r) {
        ImmutableHashSet<InferenceConstraint> set = [l];

        Assert.Contains(l, set);
        if (l.Equals(r))
            Assert.Contains(r, set);
        else
            Assert.DoesNotContain(r, set);

        ImmutableHashSet<InferenceConstraint> set2 = [l, l];

        Assert.HasCount(1, set2);
        Assert.Contains(l, set);
        if (l.Equals(r))
            Assert.Contains(r, set);
        else
            Assert.DoesNotContain(r, set);
    }


}
