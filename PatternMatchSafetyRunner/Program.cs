using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Analysis;

class Program
{
    public static string exampleProgram = 
@"
public class A { protected int z = 0; private int w = 0; }
public class B : A { public int y = 0; }
public class C : A { public int x = 0; }

public class Program {
    public static void Main() {
        A x = new B();
        x = new C();
        int y = x switch {
            B b => 1
        };
    }
}
";
    public static void Main()
    {
        SyntaxTree tree = CSharpSyntaxTree.ParseText(exampleProgram);
        CompilationUnitSyntax root = tree.GetCompilationUnitRoot();
        var compilation = CSharpCompilation.Create("HelloWorld").AddSyntaxTrees(tree);
        var semanticModel = compilation.GetSemanticModel(tree);
        if (semanticModel is null)
            throw new Exception("Semantic model was null.");

        var assigner = new AbstractObjectIDAssigner(semanticModel);
        assigner.Visit(root);

        foreach (var item in assigner.AbstractObjectIDsToCodeLocations) {
            Console.WriteLine($"Constructor call occurred at {item.Value} and was assigned ID {item.Key} with type {assigner.TypeMap[item.Key]}.");
            Console.WriteLine($"ID has fields: ");
            foreach (var dom in assigner.HeapDomain) {
                if(dom.Item1.Equals(item.Key))
                    Console.WriteLine($"\t{dom.Item2}");
            }
        }

    }
}