using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Analysis.Types;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using System.Reflection;

namespace Analysis;

public class AbstractObjectIDAssigner(SemanticModel semanticModel) : CSharpSyntaxWalker {
    protected Dictionary<AbstractObjID, TextSpan> _AbstractObjectIDsToCodeLocations = [];
    protected Optional<ImmutableDictionary<AbstractObjID, TextSpan>> __AbstractObjectIDsToCodeLocations = new();
    public ImmutableDictionary<AbstractObjID, TextSpan> AbstractObjectIDsToCodeLocations {
        get {
            if (!__AbstractObjectIDsToCodeLocations.HasValue)
                __AbstractObjectIDsToCodeLocations = new(_AbstractObjectIDsToCodeLocations.ToImmutableDictionary());
            return __AbstractObjectIDsToCodeLocations.Value;
        }
    }


    public TypeEnv TypeMap = new();


    protected HashSet<(AbstractObjID, FieldName, IFieldSymbol)> _HeapDomain = [];
    protected Optional<ImmutableHashSet<(AbstractObjID, FieldName, IFieldSymbol)>> __HeapDomain = new();
    public ImmutableHashSet<(AbstractObjID, FieldName, IFieldSymbol)> HeapDomain {
        get {
            if (!__HeapDomain.HasValue)
                __HeapDomain = new([.. _HeapDomain]);
            return __HeapDomain.Value;
        }
    }

    protected SemanticModel semanticModel = semanticModel;
    protected AbstractObjID nextID = 0;


    public override void VisitObjectCreationExpression(ObjectCreationExpressionSyntax node) {
        AbstractObjID currentID = nextID;
        nextID++;
        _AbstractObjectIDsToCodeLocations.Add(currentID, node.Span);

        if (node.Type.Kind() == SyntaxKind.IdentifierName) {
            string val = node.Type.ChildTokens().First().Text;
            TypeMap[currentID] = new Class(val);
        }
        else {
            throw new Exception($"IDK: {node}");
        }

        var info = semanticModel.GetSymbolInfo(node);
        if (info.Symbol is not null) {
            //Console.WriteLine($"Single constructor symbol found for: {TypeMap[currentID]} with ID {currentID}.");
            bool addPrivate = true;
            Optional<INamedTypeSymbol> TypeSymbol = new(info.Symbol.ContainingType);
            //Console.WriteLine($"Containing Type symbol name is: {TypeSymbol.Value.Name}");

            while (TypeSymbol.HasValue) {
                foreach (var mem in TypeSymbol.Value.GetMembers().OfType<IFieldSymbol>()) {
                    if (addPrivate || !mem.DeclaredAccessibility.Equals(Accessibility.Private)) {
                        //Console.WriteLine($"Type symbol's field: {mem.Name}");
                        _HeapDomain.Add((currentID, mem.Name, mem));
                    }
                }
                addPrivate = false;
                if (TypeSymbol.Value.BaseType is INamedTypeSymbol t)
                    TypeSymbol = new(t);
                else
                    TypeSymbol = new();
            }

        }
        else {
            //Console.WriteLine($"Multiple constructor symbol found for: {TypeMap[currentID]} with ID {currentID}.");
            foreach (var item in info.CandidateSymbols) {
                bool addPrivate = true;
                Optional<INamedTypeSymbol> TypeSymbol = new(item.ContainingType);
                //Console.WriteLine($"Containing Type symbol name is: {TypeSymbol.Value.Name}");
                while (TypeSymbol.HasValue) {
                    foreach (var mem in TypeSymbol.Value.GetMembers().OfType<IFieldSymbol>()) {
                        if (addPrivate || !mem.DeclaredAccessibility.Equals(Accessibility.Private)) {
                            //Console.WriteLine($"Type symbol's field: {mem.Name}");
                            _HeapDomain.Add((currentID, mem.Name, mem));
                        }
                    }
                    addPrivate = false;
                    if (TypeSymbol.Value.BaseType is INamedTypeSymbol t)
                        TypeSymbol = new(t);
                    else
                        TypeSymbol = new();
                }
            }
        }
    }
}