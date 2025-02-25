using Kurrent.Client.Core.Serialization;

namespace Kurrent.Client.Tests.Core.Serialization;

public class MessageSerializationContextTests
{
	[Fact]
	public void CategoryName_ExtractsFromStreamName()
	{
		// Arrange
		var context = new MessageSerializationContext("user-123", ContentType.Json);
            
		// Act
		var categoryName = context.CategoryName;
            
		// Assert
		Assert.Equal("user", categoryName);
	}
	
	[Fact]
	public void CategoryName_ExtractsFromStreamNameWithMoreThanOneDash()
	{
		// Arrange
		var context = new MessageSerializationContext("user-some-123", ContentType.Json);
            
		// Act
		var categoryName = context.CategoryName;
            
		// Assert
		Assert.Equal("user", categoryName);
	}
	
	[Fact]
	public void CategoryName_ReturnsTheWholeStreamName()
	{
		// Arrange
		var context = new MessageSerializationContext("user123", ContentType.Json);
            
		// Act
		var categoryName = context.CategoryName;
            
		// Assert
		Assert.Equal("user123", categoryName);
	}
}
