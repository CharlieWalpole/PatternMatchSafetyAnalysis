using Analysis.Types;
using Environment = Analysis.Types.Environment;
using AnalysisTests.Util;
using Analysis;
using Microsoft.CodeAnalysis;

namespace AnalysisTests.DataTests.Constraints;


[TestClass]
[TestCategory("UnSat")]
public sealed class UnSat {

    [TestMethod]
    [TestCategory("Constraint")]
    [TestCategory("ObjectInclusion")]
    public void ObjectInclusion_True() {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        AbstractObjectIDAssigner Delta = new AbstractObjectIDAssigner(null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        InferenceConstraint.ObjectInclusion c = new InferenceConstraint.ObjectInclusion(new ObjectInference.Literal([0], new Optional<(string, SyntaxNode)>()), new ObjectInference.Literal([], new Optional<(string, SyntaxNode)>()));

        Assert.IsTrue(c.IsTrivialUnsat(Delta));
    }

    [TestMethod]
    [TestCategory("Constraint")]
    [TestCategory("ObjectInclusion")]
    public void ObjectInclusion_False() {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        AbstractObjectIDAssigner Delta = new AbstractObjectIDAssigner(null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        InferenceConstraint.ObjectInclusion c = new InferenceConstraint.ObjectInclusion(new ObjectInference.Literal([0, 1], new Optional<(string, SyntaxNode)>()), new ObjectInference.Literal([0, 1, 2], new Optional<(string, SyntaxNode)>()));

        Assert.IsFalse(c.IsTrivialUnsat(Delta));
    }


    [TestMethod]
    [TestCategory("Constraint")]
    [TestCategory("AliasBounding")]
    public void AliasBounding_True() {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        AbstractObjectIDAssigner Delta = new AbstractObjectIDAssigner(null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        InferenceConstraint.AliasBounding c = new InferenceConstraint.AliasBounding(new AliasInference.Literal(new AliasData(AliasFlag.M)), new AliasInference.Literal(new AliasData(AliasFlag.S)));

        Assert.IsTrue(c.IsTrivialUnsat(Delta));
    }

    [TestMethod]
    [TestCategory("Constraint")]
    [TestCategory("AliasBounding")]
    public void AliasBounding_False() {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        AbstractObjectIDAssigner Delta = new AbstractObjectIDAssigner(null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        InferenceConstraint.AliasBounding c = new InferenceConstraint.AliasBounding(new AliasInference.Literal(new AliasData(AliasFlag.M)), new AliasInference.Literal(new AliasData(AliasFlag.M)));

        Assert.IsFalse(c.IsTrivialUnsat(Delta));
    }


    [TestMethod]
    [TestCategory("Constraint")]
    [TestCategory("SubTyping")]
    public void SubTyping_True() {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        AbstractObjectIDAssigner Delta = new AbstractObjectIDAssigner(null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        InferenceConstraint.SubTyping c = new InferenceConstraint.SubTyping(new TypeInference.Literal([new Class("A")], new Optional<(string, SyntaxNode)>()), new TypeInference.Literal([], new Optional<(string, SyntaxNode)>()));

        Assert.IsTrue(c.IsTrivialUnsat(Delta));
    }

    [TestMethod]
    [TestCategory("Constraint")]
    [TestCategory("AliasBounding")]
    public void SubTyping_False() {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        AbstractObjectIDAssigner Delta = new AbstractObjectIDAssigner(null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        InferenceConstraint.SubTyping c = new InferenceConstraint.SubTyping(new TypeInference.Literal([new Class("A")], new Optional<(string, SyntaxNode)>()), new TypeInference.Literal([new Class("A")], new Optional<(string, SyntaxNode)>()));

        Assert.IsFalse(c.IsTrivialUnsat(Delta));
    }

}
