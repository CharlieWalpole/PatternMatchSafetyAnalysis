using System.Collections.Immutable;
using Analysis.Types;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Analysis;

public class MethodCollector(SemanticModel semanticModel) : CSharpSyntaxWalker {
    protected HashSet<DeclarationSyntax> _MethodSet = [];
    protected Optional<ImmutableHashSet<DeclarationSyntax>> __MethodSet = new();
    public ImmutableHashSet<DeclarationSyntax> MethodSet {
        get {
            if (!__MethodSet.HasValue)
                __MethodSet = new([.. _MethodSet]);
            return __MethodSet.Value;
        }
    }

    protected Dictionary<DeclarationSyntax, ImmutableList<DeclarationSyntax>> _CallMap = [];
    protected Optional<ImmutableDictionary<DeclarationSyntax, ImmutableList<DeclarationSyntax>>> __CallMap = new();
    public ImmutableDictionary<DeclarationSyntax, ImmutableList<DeclarationSyntax>> CallMap {
        get {
            if (!__CallMap.HasValue)
                __CallMap = new([.. _CallMap]);
            return __CallMap.Value;
        }
    }

    protected Optional<ImmutableList<AnalysisUnit>> _analysisOrdering = new();
    public ImmutableList<AnalysisUnit> AnalysisOrdering {
        get {
            if (!_analysisOrdering.HasValue) {
                ImmutableDictionary<AnalysisUnit, ImmutableHashSet<AnalysisUnit>> ReducedCalls = ReduceCallMapClosure(GetCallMapTransClosure());
                var sorted = TopologicalSort(
                        [.. ReducedCalls.Keys],
                        [.. ReducedCalls.SelectMany(kv => kv.Value.Select(dest => new Tuple<AnalysisUnit, AnalysisUnit>(kv.Key, dest)))]
                    );
                sorted.Reverse();
                _analysisOrdering = new(
                    [..sorted]
                );
            }
            return _analysisOrdering.Value;
        }
    }

    private void AddDefns(DeclarationSyntax decl) {
        _MethodSet.Add(decl);

        List<DeclarationSyntax> defns = [.. decl.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .SelectMany(n => semanticModel.GetSymbolInfo(n).Symbol.Cons(semanticModel.GetSymbolInfo(n).CandidateSymbols)
                .Where(s => s is not null).Select(s => s!)
                .Where(s => s.DeclaringSyntaxReferences.Length > 0)
                .Select(s => (s.DeclaringSyntaxReferences.First().GetSyntax() as MethodDeclarationSyntax)!))
            .Select(n => new MethodDecl(n) as DeclarationSyntax)];
        defns.AddRange(
            decl.DescendantNodes()
                .OfType<ObjectCreationExpressionSyntax>()
                .SelectMany(n => semanticModel.GetSymbolInfo(n).Symbol.Cons(semanticModel.GetSymbolInfo(n).CandidateSymbols)
                    .Where(s => s is not null).Select(s => s!)
                    .Where(s => s.DeclaringSyntaxReferences.Length > 0)
                    .Select(s => (s.DeclaringSyntaxReferences.First().GetSyntax() as ConstructorDeclarationSyntax)!)
                .Select(n => new ConstructorDecl(n) as DeclarationSyntax)
        ));

        _CallMap.Add(decl, [.. defns]);
    }

    public override void VisitMethodDeclaration(MethodDeclarationSyntax node) => AddDefns(new MethodDecl(node));


    public override void VisitConstructorDeclaration(ConstructorDeclarationSyntax node) => AddDefns(new ConstructorDecl(node));

    public ImmutableDictionary<DeclarationSyntax, ImmutableHashSet<DeclarationSyntax>> GetCallMapTransClosure() {
        Dictionary<DeclarationSyntax, HashSet<DeclarationSyntax>> Calls = [];

        // foreach (var item in CallMap) {
        //     Calls.Add(item.Key, [.. item.Value]);
        // }

        foreach (var src in CallMap.Keys) {
            HashSet<DeclarationSyntax> Dests = [];
            HashSet<DeclarationSyntax> seen = [];
            Queue<DeclarationSyntax> toCheck = [];
            toCheck.Enqueue(src);

            while (toCheck.Count != 0) {
                DeclarationSyntax node = toCheck.Dequeue();
                if (seen.Contains(node)) continue;
                seen.Add(node);

                foreach (DeclarationSyntax dest in CallMap[node]) {
                    if (!seen.Contains(dest)) {
                        Dests.Add(dest);
                        toCheck.Enqueue(dest);
                    }
                }
            }

            Calls.Add(src, [.. Dests]);
        }

        // bool changed = true;
        // while (changed) {
        //     changed = false;
        //     foreach (var src in Calls.Keys) {
        //         foreach (var dest in Calls[src]) {
        //             if (Calls[dest].Difference(Calls[src], out ImmutableHashSet<DeclarationSyntax> diff)) {
        //                 changed = true;
        //                 foreach (var item in diff) {
        //                     Calls[src].Add(item);
        //                 }
        //             }
        //         }
        //     }
        // }

        foreach (var item in this.MethodSet) {
            if (!Calls.ContainsKey(item))
                Calls.Add(item, []);
        }

        return [.. Calls.Select(k => new KeyValuePair<DeclarationSyntax, ImmutableHashSet<DeclarationSyntax>>(k.Key, [.. k.Value]))];
    }

    public ImmutableDictionary<AnalysisUnit, ImmutableHashSet<AnalysisUnit>> ReduceCallMapClosure(ImmutableDictionary<DeclarationSyntax, ImmutableHashSet<DeclarationSyntax>> Calls) {
        //Initial translation from DeclSyntax -> AnalysisUnit is required because, ... ImmutableHashSet equality?
        Dictionary<DeclarationSyntax, AnalysisUnit> initialLookup = Calls.Keys.Select(d => new KeyValuePair<DeclarationSyntax, AnalysisUnit>(d, new AnalysisUnit([d]))).ToDictionary();

        //Setup initial call graph closure.
        Dictionary<AnalysisUnit, ImmutableHashSet<AnalysisUnit>> ret = Calls
            .Select(kv => new KeyValuePair<AnalysisUnit, ImmutableHashSet<AnalysisUnit>>(
                initialLookup[kv.Key],
                [.. kv.Value.Select(n => initialLookup[n])]
            )
        ).ToDictionary();

        bool changed = true;
        while (changed) {
            changed = false;

            foreach (var src in ret.Keys) {
                List<DeclarationSyntax> newSrc = [.. src.Defns];   //New unit method definitions
                HashSet<AnalysisUnit> oldUnits = [src];              //Old units (old nodes)
                List<AnalysisUnit> oldEdges = [.. ret[src]];            //Old edge's destitions; should be preserved.
                foreach (var dest in ret[src]) {
                    if (ret[dest].Contains(src)) {
                        changed = true;                                                    //The graph has changed
                        newSrc.AddRange(dest.Defns);                                       //Update new unit method definition list.
                        oldUnits.Add(dest);                                                //Update old node list.
                        oldEdges.AddRange(ret[dest]);                                      //Update old edge destination list.
                    }
                }

                oldEdges = [.. oldEdges.Distinct()];                      //Remove duplicate edge destinations.
                oldEdges.RemoveAll(oldUnits.Contains);                                     //Remove all 'internal' edges for the new unit

                if (oldUnits.Count > 1) {
                    AnalysisUnit newUnit = new AnalysisUnit([.. newSrc]); //Make new node.
                    foreach (AnalysisUnit old in oldUnits) {
                        ret.Remove(old);                        //Remove all old units from the graph.
                    }
                    foreach (var kv in ret) {
                        //Update all edges that pointed to old node to point to the new node
                        ret[kv.Key] = [.. kv.Value.Select(n => !oldUnits.Contains(n) ? n : newUnit)];
                    }
                    ret.Add(newUnit, [.. oldEdges]); //Add new unit and preserved (external) edges to the graph
                }
            }
        }

        return [.. ret];
    }


    /// <summary>
    /// Topological Sorting (Kahn's algorithm) 
    /// Adapted from https://gist.github.com/Sup3rc4l1fr4g1l1571c3xp14l1d0c10u5/3341dba6a53d7171fe3397d13d00ee3f
    /// </summary>
    /// <remarks>https://en.wikipedia.org/wiki/Topological_sorting</remarks>
    /// <typeparam name="T"></typeparam>
    /// <param name="nodes">All nodes of directed acyclic graph.</param>
    /// <param name="edges">All edges of directed acyclic graph.</param>
    /// <returns>Sorted node in topological order.</returns>
    protected static List<T> TopologicalSort<T>(HashSet<T> nodes, HashSet<Tuple<T, T>> edges) where T : IEquatable<T> {
        // Empty list that will contain the sorted elements
        List<T> L = [];

        // Set of all nodes with no incoming edges
        HashSet<T> S = [.. nodes.Where(n => edges.All(e => e.Item2.Equals(n) == false))];

        // while S is non-empty do
        while (S.Count != 0) {

            //  remove a node n from S
            T n = S.First();
            S.Remove(n);

            // add n to tail of L
            L.Add(n);

            // for each node m with an edge e from n to m do
            foreach (var e in edges.Where(e => e.Item1.Equals(n)).ToList()) {
                T m = e.Item2;

                // remove edge e from the graph
                edges.Remove(e);

                // if m has no other incoming edges then
                if (edges.All(me => me.Item2.Equals(m) == false)) {
                    // insert m into S
                    S.Add(m);
                }
            }
        }

        // if graph has edges then
        if (edges.Any()) {
            // return error (graph has at least one cycle)
            throw new ArgumentException("Graph being sorted contains a cycle.");
        }
        else {
            // return L (a topologically sorted order)
            return L;
        }
    }

}
