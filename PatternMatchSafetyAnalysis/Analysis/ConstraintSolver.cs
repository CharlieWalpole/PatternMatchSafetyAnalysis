using Analysis.Types;

namespace Analysis;


public class ConstraintSolver {
    protected AbstractObjectIDAssigner Delta;
    protected HashSet<InferenceConstraint> Constraints;

    public ConstraintSolver(AbstractObjectIDAssigner Delta, HashSet<InferenceConstraint> Constraints) {
        this.Delta = Delta;
        this.Constraints = Constraints;
    }

    protected void Transitivity<T, L>() where T : InferenceConstraint.PartialOrder<T, L> where L : InferenceVariable {
        IEnumerable<T> objIncl = Constraints.Where(con => con is T).Select(con => (T)con);
        foreach (var l in objIncl) {
            foreach (var r in objIncl) {
                if (T.isTransitive(l, r))
                    Constraints.Add(T.Transitivity(l, r));
            }
        }
    }

    protected void TransitivityObj() => Transitivity<InferenceConstraint.ObjectInclusion, ObjectInference>();
    protected void TransitivityType() => Transitivity<InferenceConstraint.SubTyping, TypeInference>();
    protected void TransitivityAlias() => Transitivity<InferenceConstraint.AliasBounding, AliasInference>();

    protected void Satisfaction() {
        
    }

}
