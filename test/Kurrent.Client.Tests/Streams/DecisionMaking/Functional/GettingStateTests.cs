using EventStore.Client;
using Kurrent.Client.Streams.GettingState;

namespace Kurrent.Client.Tests.Streams.DecisionMaking.Functional;

[Trait("Category", "Target:Streams")]
[Trait("Category", "Operation:Decide")]
public class DecisionMakingTests(ITestOutputHelper output, KurrentPermanentFixture fixture)
	: KurrentPermanentTests<KurrentPermanentFixture>(output, fixture) {
	[RetryFact]
	public async Task gets_state_for_state_builder_with_evolve_function() {
		// Given
		var shoppingCartId  = Guid.NewGuid();
		var clientId        = Guid.NewGuid();
		var shoesId         = Guid.NewGuid();
		var tShirtId        = Guid.NewGuid();
		var twoPairsOfShoes = new PricedProductItem(shoesId, 2, 100);
		var pairOfShoes     = new PricedProductItem(shoesId, 1, 100);
		var tShirt          = new PricedProductItem(tShirtId, 1, 50);

		var events = new object[] {
			new ShoppingCartOpened(shoppingCartId, clientId),
			new ProductItemAddedToShoppingCart(shoppingCartId, twoPairsOfShoes),
			new ProductItemAddedToShoppingCart(shoppingCartId, tShirt),
			new ProductItemRemovedFromShoppingCart(shoppingCartId, pairOfShoes),
			new ShoppingCartConfirmed(shoppingCartId, DateTime.UtcNow),
			new ShoppingCartCanceled(shoppingCartId, DateTime.UtcNow)
		};

		var streamName = $"shopping_cart-{shoppingCartId}";

		await Fixture.Streams.AppendToStreamAsync(streamName, events);

		// When
		var result = await Fixture.Streams.GetStateAsync(
			streamName,
			BuildState.From<ShoppingCart, object>(ShoppingCart.Evolve, ShoppingCart.Default)
		);

		var shoppingCart = result.State;

		// Then
		Assert.Equal(shoppingCartId, shoppingCart.Id);
		Assert.Equal(2, shoppingCart.ProductItems.Length);

		Assert.Equal(shoesId, shoppingCart.ProductItems[0].ProductId);
		Assert.Equal(pairOfShoes.Quantity, shoppingCart.ProductItems[0].Quantity);
		Assert.Equal(pairOfShoes.UnitPrice, shoppingCart.ProductItems[0].UnitPrice);

		Assert.Equal(tShirtId, shoppingCart.ProductItems[1].ProductId);
		Assert.Equal(tShirt.Quantity, shoppingCart.ProductItems[1].Quantity);
		Assert.Equal(tShirt.UnitPrice, shoppingCart.ProductItems[1].UnitPrice);
	}
}

public record ShoppingCartOpened(
	Guid ShoppingCartId,
	Guid ClientId
);

public record ProductItemAddedToShoppingCart(
	Guid ShoppingCartId,
	PricedProductItem ProductItem
);

public record ProductItemRemovedFromShoppingCart(
	Guid ShoppingCartId,
	PricedProductItem ProductItem
);

public record ShoppingCartConfirmed(
	Guid ShoppingCartId,
	DateTime ConfirmedAt
);

public record ShoppingCartCanceled(
	Guid ShoppingCartId,
	DateTime CanceledAt
);

// VALUE OBJECTS
public record PricedProductItem(
	Guid ProductId,
	int Quantity,
	decimal UnitPrice
) {
	public decimal TotalPrice => Quantity * UnitPrice;
}

// ENTITY
public record ShoppingCart(
	Guid Id,
	ShoppingCartStatus Status,
	PricedProductItem[] ProductItems
) {
	public static ShoppingCart Default() =>
		new(Guid.Empty, default, []);

	public static ShoppingCart Evolve(ShoppingCart shoppingCart, object @event) =>
		@event switch {
			ShoppingCartOpened(var shoppingCartId, var clientId) =>
				shoppingCart with {
					Id = shoppingCartId,
					Status = ShoppingCartStatus.Pending
				},
			ProductItemAddedToShoppingCart(_, var pricedProductItem) =>
				shoppingCart with {
					ProductItems = shoppingCart.ProductItems
						.Concat([pricedProductItem])
						.GroupBy(pi => pi.ProductId)
						.Select(
							group => group.Count() == 1
								? group.First()
								: new PricedProductItem(
									group.Key,
									group.Sum(pi => pi.Quantity),
									group.First().UnitPrice
								)
						)
						.ToArray()
				},
			ProductItemRemovedFromShoppingCart(_, var pricedProductItem) =>
				shoppingCart with {
					ProductItems = shoppingCart.ProductItems
						.Select(
							pi => pi.ProductId == pricedProductItem.ProductId
								? pi with { Quantity = pi.Quantity - pricedProductItem.Quantity }
								: pi
						)
						.Where(pi => pi.Quantity > 0)
						.ToArray()
				},
			ShoppingCartConfirmed =>
				shoppingCart with {
					Status = ShoppingCartStatus.Confirmed
				},
			ShoppingCartCanceled =>
				shoppingCart with {
					Status = ShoppingCartStatus.Canceled
				},
			_ => shoppingCart
		};
}

public enum ShoppingCartStatus {
	Pending   = 1,
	Confirmed = 2,
	Canceled  = 4,

	Closed = Confirmed | Canceled
}
