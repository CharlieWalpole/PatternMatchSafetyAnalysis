using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Analysis.Types;

namespace Analysis;

public class AbstractObjectIDAssigner : CSharpSyntaxWalker
{
    public Dictionary<AbstractObjID, TextSpan> AbstractObjectIDsToCodeLocations = [];
    public TypeEnv TypeMap = new();
    protected AbstractObjID nextID = 0;

    public override void VisitObjectCreationExpression(ObjectCreationExpressionSyntax node)
    {
        AbstractObjID currentID = nextID;
        nextID++;
        AbstractObjectIDsToCodeLocations.Add(currentID, node.Span);

        if(node.Type.Kind() == SyntaxKind.IdentifierName)
        {
            string val = node.Type.ChildTokens().First().Text;
            TypeMap[currentID] = new Class(val);
        } else
        {
            throw new Exception($"IDK: {node}");
        }
    }
}