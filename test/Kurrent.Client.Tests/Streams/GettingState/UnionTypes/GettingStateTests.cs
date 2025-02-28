using System.Collections.Immutable;
using EventStore.Client;
using Kurrent.Client.Streams.GettingState;

namespace Kurrent.Client.Tests.Streams.GettingState.UnionTypes;
using static ShoppingCart;
using static ShoppingCart.Event;

[Trait("Category", "Target:Streams")]
[Trait("Category", "Operation:GetState")]
public class GettingStateTests(ITestOutputHelper output, KurrentPermanentFixture fixture)
	: KurrentPermanentTests<KurrentPermanentFixture>(output, fixture) {
	[RetryFact]
	public async Task gets_state_for_state_builder_with_evolve_function_and_typed_events() {
		// Given
		var shoppingCartId  = Guid.NewGuid();
		var clientId        = Guid.NewGuid();
		var shoesId         = Guid.NewGuid();
		var tShirtId        = Guid.NewGuid();
		var twoPairsOfShoes = new PricedProductItem(shoesId, 2, 100);
		var pairOfShoes     = new PricedProductItem(shoesId, 1, 100);
		var tShirt          = new PricedProductItem(tShirtId, 1, 50);

		var events = new Event[] {
			new Opened(shoppingCartId, clientId, DateTime.UtcNow),
			new ProductItemAdded(shoppingCartId, twoPairsOfShoes, DateTime.UtcNow),
			new ProductItemAdded(shoppingCartId, tShirt, DateTime.UtcNow),
			new ProductItemRemoved(shoppingCartId, pairOfShoes, DateTime.UtcNow),
			new Confirmed(shoppingCartId, DateTime.UtcNow),
			new Canceled(shoppingCartId, DateTime.UtcNow)
		};

		var streamName = $"shopping_cart-{shoppingCartId}";

		await Fixture.Streams.AppendToStreamAsync(streamName, events);

		// When
		var result = await Fixture.Streams.GetStateAsync(
			streamName,
			StateBuilder.For<ShoppingCart, Event>(Evolve, () => new Initial())
		);

		var shoppingCart = result.State;

		// Then
		Assert.IsType<Closed>(shoppingCart);
		// TODO: Add some time travelling
		// Assert.Equal(2, shoppingCart.);
		//
		// Assert.Equal(shoesId, shoppingCart.ProductItems[0].ProductId);
		// Assert.Equal(pairOfShoes.Quantity, shoppingCart.ProductItems[0].Quantity);
		// Assert.Equal(pairOfShoes.UnitPrice, shoppingCart.ProductItems[0].UnitPrice);
		//
		// Assert.Equal(tShirtId, shoppingCart.ProductItems[1].ProductId);
		// Assert.Equal(tShirt.Quantity, shoppingCart.ProductItems[1].Quantity);
		// Assert.Equal(tShirt.UnitPrice, shoppingCart.ProductItems[1].UnitPrice);
	}
}

public record PricedProductItem(
	Guid ProductId,
	int Quantity,
	decimal UnitPrice
) {
	public decimal TotalPrice => Quantity * UnitPrice;
}

public abstract record ShoppingCart {
	public abstract record Event {
		public record Opened(
			Guid ShoppingCartId,
			Guid ClientId,
			DateTimeOffset OpenedAt
		) : Event;

		public record ProductItemAdded(
			Guid ShoppingCartId,
			PricedProductItem ProductItem,
			DateTimeOffset AddedAt
		) : Event;

		public record ProductItemRemoved(
			Guid ShoppingCartId,
			PricedProductItem ProductItem,
			DateTimeOffset RemovedAt
		) : Event;

		public record Confirmed(
			Guid ShoppingCartId,
			DateTimeOffset ConfirmedAt
		) : Event;

		public record Canceled(
			Guid ShoppingCartId,
			DateTimeOffset CanceledAt
		) : Event;

		// This won't allow external inheritance
		private Event() { }
	}

	public record Initial : ShoppingCart;

	public record Pending(ProductItems ProductItems) : ShoppingCart;

	public record Closed : ShoppingCart;

	public static ShoppingCart Evolve(ShoppingCart state, Event @event) =>
		(state, @event) switch {
			(Initial, Opened) =>
				new Pending(ProductItems.Empty),

			(Pending(var productItems), ProductItemAdded(_, var productItem, _)) =>
				new Pending(productItems.Add(productItem)),

			(Pending(var productItems), ProductItemRemoved(_, var productItem, _)) =>
				new Pending(productItems.Remove(productItem)),

			(Pending, Confirmed) =>
				new Closed(),

			(Pending, Canceled) =>
				new Closed(),

			_ => state
		};
}

public record ProductItems(ImmutableDictionary<string, int> Items) {
	public static ProductItems Empty => new(ImmutableDictionary<string, int>.Empty);

	public ProductItems Add(PricedProductItem productItem) =>
		IncrementQuantity(Key(productItem), productItem.Quantity);

	public ProductItems Remove(PricedProductItem productItem) =>
		IncrementQuantity(Key(productItem), -productItem.Quantity);

	public bool HasEnough(PricedProductItem productItem) =>
		Items.TryGetValue(Key(productItem), out var currentQuantity) && currentQuantity >= productItem.Quantity;

	static string Key(PricedProductItem pricedProductItem) =>
		$"{pricedProductItem.ProductId}_{pricedProductItem.UnitPrice}";

	ProductItems IncrementQuantity(string key, int quantity) =>
		new(Items.SetItem(key, Items.TryGetValue(key, out var current) ? current + quantity : quantity));
}

public static class DictionaryExtensions {
	public static ImmutableDictionary<TKey, TValue> Set<TKey, TValue>(
		this ImmutableDictionary<TKey, TValue> dictionary,
		TKey key,
		Func<TValue?, TValue> set
	) where TKey : notnull =>
		dictionary.SetItem(key, set(dictionary.TryGetValue(key, out var current) ? current : default));

	public static void Set<TKey, TValue>(
		this Dictionary<TKey, TValue> dictionary,
		TKey key,
		Func<TValue?, TValue> set
	) where TKey : notnull =>
		dictionary[key] = set(dictionary.TryGetValue(key, out var current) ? current : default);
}
