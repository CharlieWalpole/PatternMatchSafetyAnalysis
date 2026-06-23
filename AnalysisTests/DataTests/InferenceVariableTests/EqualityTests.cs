using Analysis.Types;

namespace AnalysisTests.DataTests.InferenceVariableTests;

[TestClass]
public sealed class EqualityTests {

    public static IEnumerable<int[]> ObjectSets = [
        [],
        [0],
        [0,1],
        [0, 1, 2]
    ];

    public static IEnumerable<int> VariableIDs = [0, 1, 2, 3];

    [TestMethod]
    [DynamicData(nameof(ObjectSets))]
    public void ObjectInferenceLiteral_True(int[] objs) {
        ObjectInference l = ObjectInference.Create([.. objs]);
        ObjectInference r = ObjectInference.Create([.. objs]);

        Assert.AreEqual(l, r);
    }

    [TestMethod]
    [DynamicData(nameof(VariableIDs))]
    public void ObjectInferenceVar_True(int ID) {
        ObjectInference l = new ObjectInference.Var(ID);
        ObjectInference r = new ObjectInference.Var(ID);

        Assert.AreEqual(l, r);
    }
}