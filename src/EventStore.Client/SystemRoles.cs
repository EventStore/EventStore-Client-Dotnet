#nullable enable
namespace EventStore.Client {
	/// <summary>
	/// Roles used by the system.
	/// </summary>
	public static class SystemRoles {
		/// <summary>
		/// The $admin role.
		/// </summary>
		public const string Admins = "$admins";

		/// <summary>
		/// The $ops role.
		/// </summary>
		public const string Operations = "$ops";

		/// <summary>
		/// The $all role.
		/// </summary>
		public const string All = "$all";
	}
}
