using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Analysis;

class Program
{
    public static string exampleProgram = 
@"
public class A {}
public class B : A {}
public class C : A {}

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
        var assigner = new AbstractObjectIDAssigner();
        assigner.Visit(root);

        foreach(var item in assigner.AbstractObjectIDsToCodeLocations)
        {
            Console.WriteLine($"Constructor call occurred at {item.Value} and was assigned ID {item.Key} with type {assigner.TypeMap[item.Key]}.");
        }

    }
}