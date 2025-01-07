using System.Text;

namespace Kurrent.Client.Tests;

static class PasswordGenerator {
	static PasswordGenerator() {
		Random        = new();
		AsciiChars    = GenerateAsciiCharacters();
		NonAsciiChars = GenerateNonAsciiCharacters();
	}

	static Random Random        { get; }
	static string AsciiChars    { get; }
	static string NonAsciiChars { get; }

	static string GenerateAsciiCharacters() {
		var builder = new StringBuilder();
		for (var i = 32; i < 127; i++)
			builder.Append((char)i);

		return builder.ToString();
	}

	static string GenerateNonAsciiCharacters() {
		var builder = new StringBuilder();
		for (var i = 127; i < 65535; i++)
			builder.Append((char)i);

		return builder.ToString();
	}

	public static string GeneratePassword(int length = 8, int minNonAsciiChars = 1) {
		if (length < minNonAsciiChars || length <= 0 || minNonAsciiChars < 0)
			throw new ArgumentException("Invalid input parameters.");

		var password = new char[length];

		// Generate the required number of non-ASCII characters
		for (var i = 0; i < minNonAsciiChars; i++)
			password[i] = NonAsciiChars[Random.Next(NonAsciiChars.Length)];

		// Generate the remaining characters
		for (var i = minNonAsciiChars; i < length; i++)
			password[i] = AsciiChars[Random.Next(AsciiChars.Length)];

		// Shuffle the characters to randomize the password
		for (var i = length - 1; i > 0; i--) {
			var j = Random.Next(i + 1);
			(password[i], password[j]) = (password[j], password[i]);
		}

		return new(password);
	}

	public static string GenerateSimplePassword(int length = 8) {
		if (length <= 0)
			throw new ArgumentException("Invalid input parameters.");

		var password = new char[length];

		for (var i = 0; i < length; i++)
			password[i] = AsciiChars[Random.Next(AsciiChars.Length)];

		return new(password);
	}
}
