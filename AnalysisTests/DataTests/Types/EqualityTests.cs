using Analysis.Types;
using Environment = Analysis.Types.Environment;
using AnalysisTests.Util;

namespace AnalysisTests.DataTests.Types;

[TestClass]
[TestCategory("Equality")]
public sealed class EqualityTests {
    [TestMethod]
    [TestCategory("Class")]
    [DynamicData(nameof(DataSources.ClassNames), typeof(DataSources))]
    public void ClassEqual_True(string name) {
        Class l = new Class(name);
        Class r = new Class(name);

        Assert.AreEqual(l, r);
    }

    // [TestMethod]
    // [TestCategory("Arrow")]
    // [DynamicData(nameof(DataSources.ArrowArgs), typeof(DataSources))]
    public void ArrowEquality_True(string[] vars, Environment pre, Environment post, ObjectInference ret) { //Arrow type generators take a long time
        Arrow l = new Arrow(vars, pre, post, ret);
        Arrow r = new Arrow(vars, pre, post, ret);

        Assert.AreEqual(l, r);
    }
}