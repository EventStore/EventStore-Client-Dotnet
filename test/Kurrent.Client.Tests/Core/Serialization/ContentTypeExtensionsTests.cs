using EventStore.Client;
using Kurrent.Client.Core.Serialization;

namespace Kurrent.Client.Tests.Core.Serialization;

using static Constants.Metadata.ContentTypes;

public class ContentTypeExtensionsTests {
	[Fact]
	public void FromMessageContentType_WithApplicationJson_ReturnsJsonContentType() {
		// Given
		// When
		var result = ContentTypeExtensions.FromMessageContentType(ApplicationJson);

		// Then
		Assert.Equal(ContentType.Json, result);
	}

	[Fact]
	public void FromMessageContentType_WithAnyOtherContentType_ReturnsBytesContentType() {
		// Given
		// When
		var result = ContentTypeExtensions.FromMessageContentType(ApplicationOctetStream);

		// Then
		Assert.Equal(ContentType.Bytes, result);
	}

	[Fact]
	public void FromMessageContentType_WithRandomString_ReturnsBytesContentType() {
		// Given
		const string contentType = "some-random-content-type";

		// When
		var result = ContentTypeExtensions.FromMessageContentType(contentType);

		// Then
		Assert.Equal(ContentType.Bytes, result);
	}

	[Fact]
	public void ToMessageContentType_WithJsonContentType_ReturnsApplicationJson() {
		// Given
		// When
		var result = ContentType.Json.ToMessageContentType();

		// Then
		Assert.Equal(ApplicationJson, result);
	}

	[Fact]
	public void ToMessageContentType_WithBytesContentType_ReturnsApplicationOctetStream() {
		// Given
		// When
		var result = ContentType.Bytes.ToMessageContentType();

		// Then
		Assert.Equal(ApplicationOctetStream, result);
	}

	[Fact]
	public void ToMessageContentType_WithInvalidContentType_ThrowsArgumentOutOfRangeException() {
		// Given
		var contentType = (ContentType)999; // Invalid content type

		// When/Then
		Assert.Throws<ArgumentOutOfRangeException>(() => contentType.ToMessageContentType());
	}
}
