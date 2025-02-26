using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using EventStore.Client;
using Kurrent.Client.Core.Serialization;

namespace Kurrent.Client.Tests.Core.Serialization;

public class SchemaRegistryTests {
	// Test classes
	record TestEvent1;

	record TestEvent2;

	record TestEvent3;

	record TestMetadata;

	[Fact]
	public void Constructor_InitializesProperties() {
		// Given
		var serializers = new Dictionary<ContentType, ISerializer> {
			{ ContentType.Json, new SystemTextJsonSerializer() },
			{ ContentType.Bytes, new SystemTextJsonSerializer() }
		};

		var namingStrategy = new DefaultMessageTypeNamingStrategy(typeof(TestMetadata));

		// When
		var registry = new SchemaRegistry(serializers, namingStrategy);

		// Then
		Assert.Same(namingStrategy, registry.MessageTypeNamingStrategy);
	}

	[Fact]
	public void GetSerializer_ReturnsCorrectSerializer() {
		// Given
		var jsonSerializer  = new SystemTextJsonSerializer();
		var bytesSerializer = new SystemTextJsonSerializer();

		var serializers = new Dictionary<ContentType, ISerializer> {
			{ ContentType.Json, jsonSerializer },
			{ ContentType.Bytes, bytesSerializer }
		};

		var registry = new SchemaRegistry(
			serializers,
			new DefaultMessageTypeNamingStrategy(typeof(TestMetadata))
		);

		// When
		var resultJsonSerializer  = registry.GetSerializer(ContentType.Json);
		var resultBytesSerializer = registry.GetSerializer(ContentType.Bytes);

		// Then
		Assert.NotSame(resultJsonSerializer, resultBytesSerializer);
		Assert.Same(jsonSerializer, resultJsonSerializer);
		Assert.Same(bytesSerializer, resultBytesSerializer);
	}

	[Fact]
	public void From_WithDefaultSettings_CreatesRegistryWithDefaults() {
		// Given
		var settings = new KurrentClientSerializationSettings();

		// When
		var registry = SchemaRegistry.From(settings);

		// Then
		Assert.NotNull(registry);
		Assert.NotNull(registry.MessageTypeNamingStrategy);
		Assert.NotNull(registry.GetSerializer(ContentType.Json));
		Assert.NotNull(registry.GetSerializer(ContentType.Bytes));

		Assert.IsType<MessageTypeNamingStrategyWrapper>(registry.MessageTypeNamingStrategy);
		Assert.IsType<SystemTextJsonSerializer>(registry.GetSerializer(ContentType.Json));
		Assert.IsType<SystemTextJsonSerializer>(registry.GetSerializer(ContentType.Bytes));
	}

	[Fact]
	public void From_WithCustomJsonSerializer_UsesProvidedSerializer() {
		// Given
		var customJsonSerializer = new SystemTextJsonSerializer(
			new SystemTextJsonSerializationSettings {
				Options = new JsonSerializerOptions { WriteIndented = true }
			}
		);

		var settings = new KurrentClientSerializationSettings()
			.UseJsonSerializer(customJsonSerializer);

		// When
		var registry = SchemaRegistry.From(settings);

		// Then
		Assert.Same(customJsonSerializer, registry.GetSerializer(ContentType.Json));
		Assert.NotSame(customJsonSerializer, registry.GetSerializer(ContentType.Bytes));
	}

	[Fact]
	public void From_WithCustomBytesSerializer_UsesProvidedSerializer() {
		// Given
		var customBytesSerializer = new SystemTextJsonSerializer(
			new SystemTextJsonSerializationSettings {
				Options = new JsonSerializerOptions { WriteIndented = true }
			}
		);

		var settings = new KurrentClientSerializationSettings()
			.UseBytesSerializer(customBytesSerializer);

		// When
		var registry = SchemaRegistry.From(settings);

		// Then
		Assert.Same(customBytesSerializer, registry.GetSerializer(ContentType.Bytes));
		Assert.NotSame(customBytesSerializer, registry.GetSerializer(ContentType.Json));
	}

	[Fact]
	public void From_WithMessageTypeMap_RegistersTypes() {
		// Given
		var settings = new KurrentClientSerializationSettings();
		settings.RegisterMessageType<TestEvent1>("test-event-1");
		settings.RegisterMessageType<TestEvent2>("test-event-2");

		// When
		var registry       = SchemaRegistry.From(settings);
		var namingStrategy = registry.MessageTypeNamingStrategy;

		// Then
		// Verify types can be resolved
		Assert.True(namingStrategy.TryResolveClrType("test-event-1", out var type1));
		Assert.Equal(typeof(TestEvent1), type1);

		Assert.True(namingStrategy.TryResolveClrType("test-event-2", out var type2));
		Assert.Equal(typeof(TestEvent2), type2);
	}

	[Fact]
	public void From_WithCategoryMessageTypesMap_RegistersTypesWithCategories() {
		// Given
		var settings = new KurrentClientSerializationSettings();
		settings.RegisterMessageTypeForCategory<TestEvent1>("category1");
		settings.RegisterMessageTypeForCategory<TestEvent2>("category1");
		settings.RegisterMessageTypeForCategory<TestEvent3>("category2");

		// When
		var registry       = SchemaRegistry.From(settings);
		var namingStrategy = registry.MessageTypeNamingStrategy;

		// Then
		// For categories, the naming strategy should have resolved the type names
		// using the ResolveTypeName method, which by default uses the type's name
		string typeName1 = namingStrategy.ResolveTypeName(
			typeof(TestEvent1),
			new MessageTypeNamingResolutionContext("category1")
		);

		string typeName2 = namingStrategy.ResolveTypeName(
			typeof(TestEvent2),
			new MessageTypeNamingResolutionContext("category1")
		);

		string typeName3 = namingStrategy.ResolveTypeName(
			typeof(TestEvent3),
			new MessageTypeNamingResolutionContext("category2")
		);

		// Verify types can be resolved by the type names
		Assert.True(namingStrategy.TryResolveClrType(typeName1, out var resolvedType1));
		Assert.Equal(typeof(TestEvent1), resolvedType1);

		Assert.True(namingStrategy.TryResolveClrType(typeName2, out var resolvedType2));
		Assert.Equal(typeof(TestEvent2), resolvedType2);

		Assert.True(namingStrategy.TryResolveClrType(typeName3, out var resolvedType3));
		Assert.Equal(typeof(TestEvent3), resolvedType3);
	}

	[Fact]
	public void From_WithCustomNamingStrategy_UsesProvidedStrategy() {
		// Given
		var customNamingStrategy = new TestNamingStrategy();
		var settings = new KurrentClientSerializationSettings()
			.UseMessageTypeNamingStrategy(customNamingStrategy);

		// When
		var registry = SchemaRegistry.From(settings);

		// Then
		// The registry wraps the naming strategy, but should still use it
		var wrappedStrategy = registry.MessageTypeNamingStrategy;
		Assert.IsType<MessageTypeNamingStrategyWrapper>(wrappedStrategy);

		// Test to make sure it behaves like our custom strategy
		string typeName = wrappedStrategy.ResolveTypeName(
			typeof(TestEvent1),
			new MessageTypeNamingResolutionContext("test")
		);

		// Our test strategy adds "Custom-" prefix
		Assert.StartsWith("Custom-", typeName);
	}

	[Fact]
	public void From_WithNoMessageTypeNamingStrategy_UsesDefaultStrategy() {
		// Given
		var settings = new KurrentClientSerializationSettings {
			MessageTypeNamingStrategy = null,
			DefaultMetadataType       = typeof(TestMetadata)
		};

		// When
		var registry = SchemaRegistry.From(settings);

		// Then
		Assert.NotNull(registry.MessageTypeNamingStrategy);

		// The wrapped default strategy should use our metadata type
		Assert.True(
			registry.MessageTypeNamingStrategy.TryResolveClrMetadataType("some-type", out var defaultMetadataType)
		);

		Assert.Equal(typeof(TestMetadata), defaultMetadataType);
	}

	// Custom naming strategy for testing
	class TestNamingStrategy : IMessageTypeNamingStrategy {
		public string ResolveTypeName(Type type, MessageTypeNamingResolutionContext context) {
			return $"Custom-{type.Name}-{context.CategoryName}";
		}
#if NET48
	public bool TryResolveClrType(string messageTypeName, out Type? clrType)
#else
		public bool TryResolveClrType(string messageTypeName, [NotNullWhen(true)] out Type? clrType)
#endif
		{
			// Simple implementation for testing
			clrType = messageTypeName.StartsWith("Custom-TestEvent1")
				? typeof(TestEvent1)
				: null;

			return clrType != null;
		}

#if NET48
	public bool TryResolveClrMetadataType(string messageTypeName, out Type? clrType)
#else
		public bool TryResolveClrMetadataType(string messageTypeName, [NotNullWhen(true)] out Type? clrType)
#endif
		{
			clrType = typeof(TestMetadata);
			return true;
		}
	}
}
