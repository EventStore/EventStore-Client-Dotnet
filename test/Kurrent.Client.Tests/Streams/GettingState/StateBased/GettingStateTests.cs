using EventStore.Client;
using Kurrent.Client.Streams.GettingState;

namespace Kurrent.Client.Tests.Streams.GettingState.StateBased;


[Trait("Category", "Target:Streams")]
[Trait("Category", "Operation:GetState")]
public class GettingStateTests(ITestOutputHelper output, KurrentPermanentFixture fixture)
	: KurrentPermanentTests<KurrentPermanentFixture>(output, fixture) {
	[RetryFact]
	public async Task gets_state_for_istate_with_default_constructor() {
		// Given
		var shoppingCartId  = Guid.NewGuid();
		var clientId        = Guid.NewGuid();
		var shoesId         = Guid.NewGuid();
		var tShirtId        = Guid.NewGuid();
		var twoPairsOfShoes = new PricedProductItem {
			ProductId = shoesId,
			Quantity  = 2,
			UnitPrice = 100
		};
		var pairOfShoes = new PricedProductItem {
			ProductId = shoesId,
			Quantity = 1,
			UnitPrice = 100
		};
		var tShirt = new PricedProductItem {
			ProductId = tShirtId,
			Quantity  = 1,
			UnitPrice = 50
		};

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
		var result = await Fixture.Streams.GetStateAsync<ShoppingCart, object>(streamName);

		var shoppingCart = result.State;

		// Then
		Assert.Equal(shoppingCartId, shoppingCart.Id);
		Assert.Equal(2, shoppingCart.ProductItems.Count);

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

public class PricedProductItem
{
	public Guid    ProductId  { get; set; }
	public decimal UnitPrice  { get; set; }
	public int     Quantity   { get; set; }
	public decimal TotalPrice => Quantity * UnitPrice;
}

public class ShoppingCart : IState<object> {
	public Guid                     Id           { get; private set; }
	public ShoppingCartStatus       Status       { get; private set; }
	public IList<PricedProductItem> ProductItems { get; } = new List<PricedProductItem>();

	public bool IsClosed => ShoppingCartStatus.Closed.HasFlag(Status);

	public void Apply(object @event) {
		switch (@event) {
			case ShoppingCartOpened opened:
				Apply(opened);
				return;

			case ProductItemAddedToShoppingCart productItemAdded:
				Apply(productItemAdded);
				return;

			case ProductItemRemovedFromShoppingCart productItemRemoved:
				Apply(productItemRemoved);
				return;

			case ShoppingCartConfirmed confirmed:
				Apply(confirmed);
				return;

			case ShoppingCartCanceled canceled:
				Apply(canceled);
				return;
		}
	}

	public static ShoppingCart Initial() => new();

	//just for default creation of empty object
	public ShoppingCart() { }

	void Apply(ShoppingCartOpened opened) {
		Id       = opened.ShoppingCartId;
		Status   = ShoppingCartStatus.Pending;
	}

	void Apply(ProductItemAddedToShoppingCart productItemAdded) {
		var (_, pricedProductItem) = productItemAdded;
		var productId     = pricedProductItem.ProductId;
		var quantityToAdd = pricedProductItem.Quantity;

		var current = ProductItems.SingleOrDefault(pi => pi.ProductId == productId);

		if (current == null)
			ProductItems.Add(pricedProductItem);
		else
			current.Quantity += quantityToAdd;
	}

	void Apply(ProductItemRemovedFromShoppingCart productItemRemoved) {
		var (_, pricedProductItem) = productItemRemoved;
		var productId        = pricedProductItem.ProductId;
		var quantityToRemove = pricedProductItem.Quantity;

		var current = ProductItems.Single(pi => pi.ProductId == productId);

		if (current.Quantity == quantityToRemove)
			ProductItems.Remove(current);
		else
			current.Quantity -= quantityToRemove;
	}

	void Apply(ShoppingCartConfirmed confirmed) {
		Status      = ShoppingCartStatus.Confirmed;
	}

	void Apply(ShoppingCartCanceled canceled) {
		Status     = ShoppingCartStatus.Canceled;
	}
}

public enum ShoppingCartStatus {
	Pending   = 1,
	Confirmed = 2,
	Canceled  = 4,

	Closed = Confirmed | Canceled
}
