using System.Reflection;

namespace Kurrent.Client.Tests;

public static class TypeExtensions {
	public static bool InvokeEqualityOperator(this Type type, object? left, object? right) => type.InvokeOperator("Equality", left, right);

	public static bool InvokeInequalityOperator(this Type type, object? left, object? right) => type.InvokeOperator("Inequality", left, right);

	public static bool InvokeGreaterThanOperator(this Type type, object? left, object? right) => type.InvokeOperator("GreaterThan", left, right);

	public static bool InvokeLessThanOperator(this Type type, object? left, object? right) => type.InvokeOperator("LessThan", left, right);

	public static bool InvokeGreaterThanOrEqualOperator(this Type type, object? left, object? right) => type.InvokeOperator("GreaterThanOrEqual", left, right);

	public static bool InvokeLessThanOrEqualOperator(this Type type, object? left, object? right) => type.InvokeOperator("LessThanOrEqual", left, right);

	public static int InvokeGenericCompareTo(this Type type, object? left, object? right) =>
		(int)typeof(IComparable<>).MakeGenericType(type)
			.GetMethod("CompareTo", BindingFlags.Public | BindingFlags.Instance)!
			.Invoke(left, new[] { right })!;

	public static int InvokeCompareTo(this Type type, object? left, object? right) =>
		(int)typeof(IComparable)
			.GetMethod("CompareTo", BindingFlags.Public | BindingFlags.Instance)!
			.Invoke(left, new[] { right })!;

	public static bool ImplementsGenericIComparable(this Type type) => type.GetInterfaces().Any(t => t == typeof(IComparable<>).MakeGenericType(type));

	public static bool ImplementsIComparable(this Type type) => type.GetInterfaces().Length > 0 && type.GetInterfaces().Any(t => t == typeof(IComparable));

	static bool InvokeOperator(this Type type, string name, object? left, object? right) {
		var op = type.GetMethod($"op_{name}", BindingFlags.Public | BindingFlags.Static);
		if (op == null)
			throw new($"The type {type} did not implement op_{name}.");

		return (bool)op.Invoke(null, new[] { left, right })!;
	}
}
