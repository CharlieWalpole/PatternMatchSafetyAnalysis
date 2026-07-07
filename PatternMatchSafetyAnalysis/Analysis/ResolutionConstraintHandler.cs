using System.Collections.Immutable;
using System.Data;
using System.Dynamic;
using System.Text;
using Analysis.Types;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Analysis;


public class ResolutionConstraintHandler {
    public HashSet<InferenceConstraint.ObjectInclusion> ObjectInclusions { get; } = new HashSet<InferenceConstraint.ObjectInclusion>();
    public HashSet<InferenceConstraint.AliasBounding> AliasBoundings { get; } = new HashSet<InferenceConstraint.AliasBounding>();
    public HashSet<InferenceConstraint.SubTyping> SubTypings { get; } = new HashSet<InferenceConstraint.SubTyping>();
    public HashSet<InferenceConstraint.HeapLookup> HeapLookups { get; } = new HashSet<InferenceConstraint.HeapLookup>();
    public HashSet<InferenceConstraint.HeapUpdate> HeapUpdates { get; } = new HashSet<InferenceConstraint.HeapUpdate>();
    public HashSet<InferenceConstraint.TypeLookup> TypeLookups { get; } = new HashSet<InferenceConstraint.TypeLookup>();
    public HashSet<InferenceConstraint.Restriction> Restrictions { get; } = new HashSet<InferenceConstraint.Restriction>();
    public HashSet<InferenceConstraint.ApplicationResolution> ApplicationResolutions { get; } = new HashSet<InferenceConstraint.ApplicationResolution>();
    public HashSet<InferenceConstraint.Conditional> Conditionals { get; } = new HashSet<InferenceConstraint.Conditional>();


    public IEnumerable<InferenceConstraint> PartialOrders => ObjectInclusions.Select(c => (InferenceConstraint)c)
        .Append(AliasBoundings).Append(SubTypings);

    public IEnumerable<InferenceConstraint> Constraints => ObjectInclusions.Select(c => (InferenceConstraint)c)
        .Append(AliasBoundings).Append(SubTypings).Append(SubTypings).Append(HeapLookups).Append(HeapUpdates).Append(TypeLookups)
        .Append(Restrictions).Append(ApplicationResolutions).Append(Conditionals);

    public ResolutionConstraintHandler(IEnumerable<InferenceConstraint> cons) {
        foreach (var c in cons) {
            Add(c);
        }
    }

    public bool Add(InferenceConstraint cons) => cons switch {
        InferenceConstraint.ObjectInclusion obj => ObjectInclusions.Add(obj),
        InferenceConstraint.AliasBounding als => AliasBoundings.Add(als),
        InferenceConstraint.SubTyping sub => SubTypings.Add(sub),
        InferenceConstraint.HeapLookup hl => HeapLookups.Add(hl),
        InferenceConstraint.HeapUpdate hu => HeapUpdates.Add(hu),
        InferenceConstraint.TypeLookup tl => TypeLookups.Add(tl),
        InferenceConstraint.Restriction r => Restrictions.Add(r),
        InferenceConstraint.ApplicationResolution app => ApplicationResolutions.Add(app),
        InferenceConstraint.Conditional cond => Conditionals.Add(cond),
        _ => throw new ArgumentException($"Unknown inference constraint type: val = {cons}; type = {cons.GetType()}.")
    };


    public string PrintConstraints() {
        StringBuilder sb = new StringBuilder();
        sb.Append("\n { \n");
        sb.AppendJoin(", \n", Constraints);
        sb.Append("\n } \n");
        return sb.ToString();
    }

}