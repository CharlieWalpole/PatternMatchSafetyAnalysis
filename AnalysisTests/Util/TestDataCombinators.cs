namespace AnalysisTests.Util;

public static class TestDataCombinators {

    public const int ListLimitDefault = 4;
    public const int FiniteListCountLimit = 4;
    public const int ExponentialProductDomainLimit = 1;
    public const int ExponentialProductRangeLimit = 1;


    public static IEnumerable<(A, B)> CartesianProd<A, B>(IEnumerable<A> As, IEnumerable<B> Bs) {
        foreach (A a in As) {
            foreach (B b in Bs) {
                yield return (a, b);
            }
        }
    }

    public static IEnumerable<(A, B, C)> CartesianProd<A, B, C>(IEnumerable<A> As, IEnumerable<B> Bs, IEnumerable<C> Cs) {
        foreach (A a in As) {
            foreach (B b in Bs) {
                foreach (C c in Cs) {
                    yield return (a, b, c);
                }
            }
        }
    }

    public static IEnumerable<(A, B, C, D)> CartesianProd<A, B, C, D>(IEnumerable<A> As, IEnumerable<B> Bs, IEnumerable<C> Cs, IEnumerable<D> Ds) {
        foreach (A a in As) {
            foreach (B b in Bs) {
                foreach (C c in Cs) {
                    foreach (D d in Ds) {
                        yield return (a, b, c, d);
                    }
                }
            }
        }
    }

    public static IEnumerable<IEnumerable<(K, V)>> ExponentialProduct<K, V>(K[] dom, V[] rng) {
        if (dom.Length > ExponentialProductDomainLimit || rng.Length > ExponentialProductRangeLimit)
            return ExponentialProduct(dom[0..Math.Min(dom.Length,ExponentialProductDomainLimit)], rng[0..Math.Min(rng.Length,ExponentialProductRangeLimit)]);
        if (dom.Length == 0 || rng.Length == 0)
            return [];
        else if (dom.Length == 1)
            return rng.Select<V, IEnumerable<(K, V)>>(v => [(dom[0], v)]);
        else {
            IEnumerable<IEnumerable<(K, V)>> m1 = ExponentialProduct(dom[0..(dom.Length / 2 - 1)], rng);
            IEnumerable<IEnumerable<(K, V)>> m2 = ExponentialProduct(dom[(dom.Length / 2)..dom.Length], rng);
            return CartesianProd(m1, m2).Select(p => p.Item1.Append(p.Item2));
        }
    }

    public static IEnumerable<T[]> FiniteLists<T>(this IEnumerable<T> vals, int listLimit = ListLimitDefault) {
        int retCount = 0;
        IEnumerable<T[]> rets = vals.Select(v => new T[] { v });
        for (int i = 0; i < listLimit && retCount < FiniteListCountLimit; i++) {
            foreach (T[] ret in rets) {
                yield return ret;
                retCount++;
                if (retCount > FiniteListCountLimit)
                    break;
            }
            rets = CartesianProd(rets, vals).Select<(T[], T), T[]>(p => [.. p.Item1, p.Item2]);
        }
    }

    public static IEnumerable<T> Append<T>(this IEnumerable<T> l, IEnumerable<T> r) {
        foreach (var item in l) {
            yield return item;
        }
        foreach (var item in r) {
            yield return item;
        }
    }
}