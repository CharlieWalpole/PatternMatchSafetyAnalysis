using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Analysis;
using System.Collections.Immutable;
using Analysis.Types;

class Program {
    public static string exampleProgram =
@"
public class A { protected int z = 0; private int w = 0; }
public class B : A { public int y = 0; }
public class C : A { public int x = 0; public C() {} }

public class Program {
    public static void Main() {
        A x = new B();
        x = new C();
        Func<int, int> f = i => i+1;
        int y = x switch {
            B b => 1
        };
        foo();
    }

    public static void foo() { var x = new C(); }
}
";

    public static void Main() {
        StringRunner runner = new StringRunner(exampleProgram);
        AnalysisConclusion conc = runner.RunAnalysis();

        if (conc.Errors.IsEmpty) {
            Console.WriteLine("No type errors found.");
        }
        else {
            Console.WriteLine($"{conc.Errors.Count} Type Errors found: \n");
            foreach (AnalysisError err in conc.Errors) {
                Console.WriteLine(err);
            }
        }
    }

    // public static void Main() {
    //     SyntaxTree tree = CSharpSyntaxTree.ParseText(exampleProgram);
    //     CompilationUnitSyntax root = tree.GetCompilationUnitRoot();
    //     CSharpCompilation compilation = CSharpCompilation.Create("HelloWorld").AddSyntaxTrees(tree);
    //     SemanticModel semanticModel = compilation.GetSemanticModel(tree) ?? throw new Exception("Semantic model was null.");

    //     AbstractObjectIDAssigner assigner = new AbstractObjectIDAssigner(semanticModel);

    //     MethodCollector collector = new MethodCollector(semanticModel);
    //     root.Accept(assigner);
    //     root.Accept(collector);

    //     foreach (var item in assigner.AbstractObjectIDsToCodeLocations) {
    //         if(assigner.TypeMap.isClassObj(item.Key)) {
    //             Console.WriteLine($"Constructor call occurred at {item.Value.Span} and was assigned ID {item.Key} with type {assigner.TypeMap[item.Key]}.");
    //             Console.Write($"\tID has fields: ");
    //             foreach (var dom in assigner.HeapDomain) {
    //                 if (dom.Item1.Equals(item.Key))
    //                     Console.Write($"{dom.Item2}, ");
    //             }
    //             Console.WriteLine();
    //         } else {
    //             Console.WriteLine($"Closure definition occurred at {item.Value} and was assigned ID {item.Key}.");
    //         }
    //     }

    //     foreach (var item in collector.MethodSet) {
    //         Console.WriteLine($"Found method: {item.GetName()}.");

    //         foreach (var func in collector.CallMap[item])
    //             Console.WriteLine($"Found {(func.isMethodDecl ? "method" : "constructor")} call from {item.GetName()} to {func.GetName()}.");
    //     }

    //     Console.WriteLine("Call graph closure:");
    //     foreach (var item in collector.GetCallMapTransClosure()) {
    //         Console.WriteLine($"\tFrom {item.Key.GetName()} to:");
    //         foreach (var dest in item.Value) {
    //             Console.WriteLine($"\t\t{dest.GetName()}");
    //         }
    //     }

    //     Console.WriteLine();
    //     Console.WriteLine("Analysis order is: ");
    //     int i = 0;
    //     foreach (AnalysisUnit unit in collector.AnalysisOrdering) {
    //         Console.WriteLine($"\tUnit number {i} containing: ");
    //         foreach (var item in unit.Defns) {
    //             Console.WriteLine($"\t\t{(item.isMethodDecl ? "Method" : "Constructor")}: {item.GetName()}.");
    //         }
    //         i++;
    //     }
    // }

}