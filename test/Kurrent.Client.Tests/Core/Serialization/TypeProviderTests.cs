using System.Reflection;
using Kurrent.Client.Core.Serialization;

namespace Kurrent.Client.Tests.Core.Serialization {
	public record TestNestedNamespaceEvent;

	namespace Nested {
		public record TestNestedNamespaceEvent;
	}

	public class TypeProviderTests {
		[Fact]
		public void GetTypeByFullName_WorksWithLoadedAssemblyTypes() {
			// Given
			string enumFullName = typeof(TestEvent).FullName!;

			// When
			var result = TypeProvider.GetTypeByFullName(enumFullName);

			// Then
			Assert.NotNull(result);
			Assert.Equal(typeof(TestEvent), result);
		}

		[Fact]
		public void GetTypeByFullName_ReturnsNull_WhenTypeDoesNotExist() {
			// Given
			const string fullName = "NonExistent.Type.That.Should.Not.Exist";

			// When
			var result = TypeProvider.GetTypeByFullName(fullName);

			// Then
			Assert.Null(result);
		}

		[Fact]
		public void GetTypeByFullName_ForNestedNamespacesSortsAssembliesByNamespace_AndReturnsFirstMatch() {
			// Given
			string originalType = typeof(TestNestedNamespaceEvent).FullName!;
			string nestedType   = typeof(Kurrent.Client.Tests.Core.Serialization.Nested.TestNestedNamespaceEvent).FullName!;

			// When
			var originalTypeResult = TypeProvider.GetTypeByFullName(originalType);
			var nestedTypeResult   = TypeProvider.GetTypeByFullName(nestedType);

			// Then
			Assert.NotNull(originalTypeResult);
			Assert.NotNull(nestedTypeResult);
			Assert.NotEqual(originalTypeResult, nestedTypeResult);
		}

		[Fact]
		public void GetTypeByFullName_ForNestedClassesSortsAssembliesByNamespace_AndReturnsFirstMatch() {
			// Given
			string originalType = typeof(TestNestedInClassEvent).FullName!;
			string nestedType   = typeof(Nested.TestNestedInClassEvent).FullName!;

			// When
			var originalTypeResult = TypeProvider.GetTypeByFullName(originalType);
			var nestedTypeResult   = TypeProvider.GetTypeByFullName(nestedType);

			// Then
			Assert.NotNull(originalTypeResult);
			Assert.NotNull(nestedTypeResult);
			Assert.NotEqual(originalTypeResult, nestedTypeResult);
		}

		[Fact]
		public void GetTypeByFullName_HandlesGenericTypes() {
			// Given
			string fullName = typeof(GenericEvent<string>).FullName!;

			// When
			var result = TypeProvider.GetTypeByFullName(fullName);

			// Then
			Assert.NotNull(result);
			Assert.Equal(typeof(GenericEvent<string>), result);
		}

		[Fact]
		public void GetTypeByFullName_ReturnsType_WhenTypeExistsInSystemLib() {
			// Given
			string fullName = typeof(string).FullName!;

			// When
			var result = TypeProvider.GetTypeByFullName(fullName);

			// Then
			Assert.NotNull(result);
			Assert.Equal(typeof(string), result);
		}

		public record TestEvent;

		public record TestNestedInClassEvent;

		public class Nested {
			public record TestNestedInClassEvent;
		}

		/// <summary>
		/// Generic external event class to test generic type resolution
		/// </summary>
		/// <typeparam name="T">The payload type</typeparam>
		public class GenericEvent<T> {
			public string Id   { get; set; } = null!;
			public T      Data { get; set; } = default!;
		}
	}

	/// <summary>
	/// Tests for TypeProvider focusing on unloaded assemblies
	/// Uses Kurrent.Client.Tests.NeverLoadedAssembly project which should never be loaded before this test runs
	/// </summary>
	public class UnloadedAssemblyTests {
		const string NotLoadedTypeFullName = "Kurrent.Client.Tests.NeverLoadedAssembly.NotLoadedExternalEvent";

		[Fact]
		public void GetTypeByFullName_ReturnsNull_ForTypeInUnloadedAssembly() {
			// When
			var result = TypeProvider.GetTypeByFullName(NotLoadedTypeFullName);

			// Then
			Assert.Null(result);
		}
	}

	/// <summary>
	/// Tests for TypeProvider focusing on loaded assemblies
	/// Uses Kurrent.Client.Tests.ExternalAssembly.dll which will be explicitly loaded during the tests
	/// </summary>
	public class LoadedAssemblyTests {
		const    string   ExternalAssemblyName = "Kurrent.Client.Tests.ExternalAssembly";
		const    string   ExternalTypeFullName = "Kurrent.Client.Tests.ExternalAssembly.ExternalEvent";
		readonly Assembly _externalAssembly;

		public LoadedAssemblyTests() {
			var path = Path.Combine(
				AppDomain.CurrentDomain.BaseDirectory,
				$"{ExternalAssemblyName}.dll"
			);

			_externalAssembly = Assembly.LoadFrom(path);
		}

		[Fact]
		public void GetTypeByFullName_FindsType_AfterAssemblyIsExplicitlyLoaded() {
			// Given

			// When
			var externalType = TypeProvider.GetTypeByFullName(ExternalTypeFullName);

			// Then
			Assert.NotNull(externalType);
			Assert.Equal(ExternalTypeFullName, externalType.FullName);
			Assert.Equal(_externalAssembly, externalType.Assembly);
		}

		[Fact]
		public void GetTypeByFullName_PrioritizesAssembliesByNamespacePrefix() {
			// This test verifies the namespace-based prioritization by:
			// 1. Loading our test assembly which has a name matching its namespace prefix
			// 2. Verifying that we can resolve a type from it

			// Given
			// When
			var result = TypeProvider.GetTypeByFullName(ExternalTypeFullName);
			Assert.NotNull(result);
			Assert.Equal(ExternalTypeFullName, result.FullName);
			Assert.NotEqual(typeof(ExternalEvent), result);
		}

		/// <summary>
		/// External event class used for testing loaded assembly resolution, it should not be resolved,
		/// because of prioritising exact namespaces resolutions first
		/// </summary>
		public class ExternalEvent {
			public string Id   { get; set; } = null!;
			public string Name { get; set; } = null!;
		}
	}
}
