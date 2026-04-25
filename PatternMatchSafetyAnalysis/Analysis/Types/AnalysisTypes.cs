global using ClassName = string;

namespace Analysis.Types;


public interface Type {}
public record struct Class(ClassName Name) : Type;
public record class Arrow(VarName[] Args, Environment Pre, Environment Post, ObjectSet Return);


public record struct AliasData(AliasFlag Flag);
public enum AliasFlag { S, M }
