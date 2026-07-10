using Analysis.Types;
using AnalysisTests.Util;
using Microsoft.CodeAnalysis;

namespace AnalysisTests.DataTests.InferenceVariableTests;

[TestClass]
[TestCategory("Equality")]
public sealed class EqualityTests {

    [TestMethod]
    [DynamicData(nameof(DataSources.ObjectSets), typeof(DataSources))]
    [TestCategory("ObjectInference")]
    public void ObjectInferenceLiteral_True(int[] objs) {
        ObjectInference l = ObjectInference.Create([.. objs], new Optional<(string, SyntaxNode)>());
        ObjectInference r = ObjectInference.Create([.. objs], new Optional<(string, SyntaxNode)>());

        Assert.AreEqual(l, r);
        Assert.AreEqual(l.GetHashCode(), r.GetHashCode());
    }

    [TestMethod]
    [DynamicData(nameof(DataSources.ObjectSetPairs), typeof(DataSources))]
    [TestCategory("ObjectInference")]
    public void ObjectInferenceLiteral_Mixed(int[] objsL, int[] objsR) {
        ObjectInference l = ObjectInference.Create([.. objsL], new Optional<(string, SyntaxNode)>());
        ObjectInference r = ObjectInference.Create([.. objsR], new Optional<(string, SyntaxNode)>());

        if (objsL.All(objsR.Contains) && objsR.All(objsL.Contains)) {
            Assert.AreEqual(l, r);
            Assert.AreEqual(l.GetHashCode(), r.GetHashCode());
        }
        else {
            Assert.AreNotEqual(l, r);
        }
    }


    [TestMethod]
    [DynamicData(nameof(DataSources.VariableIDs), typeof(DataSources))]
    [TestCategory("ObjectInference")]
    public void ObjectInferenceVar_True(int ID) {
        ObjectInference l = new ObjectInference.Var(ID, new Optional<(string, SyntaxNode)>());
        ObjectInference r = new ObjectInference.Var(ID, new Optional<(string, SyntaxNode)>());

        Assert.AreEqual(l, r);
        Assert.AreEqual(l.GetHashCode(), r.GetHashCode());
    }

    [TestMethod]
    [DynamicData(nameof(DataSources.VariableIDPairs), typeof(DataSources))]
    [TestCategory("ObjectInference")]
    public void ObjectInferenceVar_Mixed(int IDl, int IDr) {
        ObjectInference l = new ObjectInference.Var(IDl, new Optional<(string, SyntaxNode)>());
        ObjectInference r = new ObjectInference.Var(IDr, new Optional<(string, SyntaxNode)>());

        if (IDl == IDr) {
            Assert.AreEqual(l, r);
            Assert.AreEqual(l.GetHashCode(), r.GetHashCode());
        }
        else
            Assert.AreNotEqual(l, r);
    }

    [TestMethod]
    [DynamicData(nameof(DataSources.PairClassNames), typeof(DataSources))]
    [TestCategory("TypeInference")]
    public void TypeInference_Class_Mixed(string name1, string name2) {
        TypeInference l = new TypeInference.Literal([new Class(name1)], new Optional<(string, SyntaxNode)>());
        TypeInference r = new TypeInference.Literal([new Class(name2)], new Optional<(string, SyntaxNode)>());

        if (name1.Equals(name2)) {
            Assert.AreEqual(l, r);
            Assert.AreEqual(l.GetHashCode(), r.GetHashCode());
        }
        else
            Assert.AreNotEqual(l, r);
    }

    [TestMethod]
    [DynamicData(nameof(DataSources.VariableIDPairs), typeof(DataSources))]
    [TestCategory("ObjectInference")]
    public void TypeInferenceVar_Mixed(int IDl, int IDr) {
        TypeInference l = new TypeInference.Var(IDl, new Optional<(string, SyntaxNode)>());
        TypeInference r = new TypeInference.Var(IDr, new Optional<(string, SyntaxNode)>());

        if (IDl == IDr) {
            Assert.AreEqual(l, r);
            Assert.AreEqual(l.GetHashCode(), r.GetHashCode());
        }
        else
            Assert.AreNotEqual(l, r);
    }
}
