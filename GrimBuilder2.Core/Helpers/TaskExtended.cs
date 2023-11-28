namespace GrimBuilder2.Core.Helpers;
public static class TaskExtended
{
    public static async Task WhenAll(params Func<Task>[] generators) =>
        await Task.WhenAll(generators.Select(g => g()));

    public static async Task<T[]> WhenAll<T>(params Func<Task<T>>[] generators) =>
        await Task.WhenAll(generators.Select(g => g()));

    public static async Task<(T1, T2)> WhenAll<T1, T2>(Func<Task<T1>> g1, Func<Task<T2>> g2)
    {
        var results = await WhenAll([
            async () => (object?)await g1(),
            async () => (object?)await g2()]);
        return ((T1)results[0], (T2)results[1]);
    }

    public static async Task<(T1, T2)> WhenAll<T1, T2>(Task<T1> t1, Task<T2> t2)
    {
        var results = await WhenAll([
            async () => (object?)await t1,
            async () => (object?)await t2]);
        return ((T1)results[0], (T2)results[1]);
    }

    public static async Task<(T1, T2, T3)> WhenAll<T1, T2, T3>(Task<T1> t1, Task<T2> t2, Task<T3> t3)
    {
        var results = await WhenAll([
            async () => (object?)await t1,
            async () => (object?)await t2,
            async () => (object?)await t3]);
        return ((T1)results[0], (T2)results[1], (T3)results[2]);
    }
}
