using Analysis.Types;
using Environment = Analysis.Types.Environment;
using AnalysisTests.Util;

namespace AnalysisTests.DataTests.Environments;


[TestClass]
[TestCategory("Equality")]
public sealed class EqualityTests {
    [TestMethod]
    [TestCategory("Environment")]
    [DynamicData(nameof(DataSources.EnvCol), typeof(DataSources))]
    public void Environment_True(StackEnv s, HeapEnv h, TypeEnv t, AliasEnv a) {
        Environment l = new Environment(s, h, t, a);
        Environment r = new Environment(s, h, t, a);

        Assert.AreEqual(l, r);
    }

    
}
