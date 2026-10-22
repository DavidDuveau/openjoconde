using System.Reflection;

namespace OpenJoconde.Infrastructure.Tests;

/// <summary>
/// Double d'une interface : chaque méthode lève <see cref="InvalidOperationException"/> sauf celles dont un comportement est donné.
/// Sert à provoquer les pannes qu'une vraie base ne produit pas à la demande.
/// </summary>
public class Stub : DispatchProxy
{
    private Dictionary<string, Func<object?[], object?>> _behaviors = new();

    /// <summary>Crée le double ; <paramref name="behaviors"/> associe un nom de méthode à sa réponse</summary>
    public static T Create<T>(params (string Method, Func<object?[], object?> Behavior)[] behaviors) where T : class
    {
        var proxy = Create<T, Stub>();
        ((Stub)(object)proxy)._behaviors = behaviors.ToDictionary(b => b.Method, b => b.Behavior);
        return proxy;
    }

    /// <summary>Comportement constant, quels que soient les arguments</summary>
    public static (string, Func<object?[], object?>) Returns(string method, object? value) => (method, _ => value);

    /// <summary>Panne explicite de la méthode</summary>
    public static (string, Func<object?[], object?>) Throws(string method) =>
        (method, _ => throw new InvalidOperationException($"{method} en panne (simulé)"));

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        var name = targetMethod!.Name;
        if (_behaviors.TryGetValue(name, out var behavior))
            return behavior(args ?? []);
        throw new InvalidOperationException($"{name} en panne (simulé)");
    }
}
