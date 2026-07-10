

using System.Collections.Immutable;
using Analysis.Types;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Analysis;


public class FileRunner : IAnalysisRunner {

    public string FilePath { get; }
    protected string FileContents { get; }

    protected SyntaxTree Tree { get; }
    protected SemanticModel SemanticModel { get; }
    protected AbstractObjectIDAssigner Delta { get; }
    protected MethodCollector Collector { get; }

    protected AnalysisVisitor ConstraintGenerator { get; }

    public FileRunner(string filePath) {
        FilePath = filePath;
        FileContents = File.ReadAllText(filePath);

        Tree = CSharpSyntaxTree.ParseText(FileContents);
        CompilationUnitSyntax root = Tree.GetCompilationUnitRoot();
        CSharpCompilation compilation = CSharpCompilation.Create(Path.GetFileNameWithoutExtension(filePath)).AddSyntaxTrees(Tree);
        SemanticModel = compilation.GetSemanticModel(Tree);
        if (SemanticModel is null)
            throw new Exception("Semantic model was null.");

        Delta = new AbstractObjectIDAssigner(SemanticModel);
        Collector = new MethodCollector(SemanticModel);

        root.Accept(Delta);
        root.Accept(Collector);

        ConstraintGenerator = new AnalysisVisitor(SemanticModel, Delta);
    }


    public AnalysisConclusion RunAnalysis() {
        foreach (Types.AnalysisUnit unit in Collector.AnalysisOrdering) {
            ConstraintGenerator.HandleAnalysisUnit(unit);
        }

        return new AnalysisConclusion([.. ConstraintGenerator.MethodSummary.Values.SelectMany(summary => new ConstraintSolver(Delta, summary.Constraints).GetAnalysisErrors())]);
    }
}
