namespace EventStore.Client;

/// <summary>
/// Provides the definitions of the available extra statistics.
/// </summary>
public static class PersistentSubscriptionExtraStatistic {
#pragma warning disable CS1591
	public const string Highest                    = "Highest";
	public const string Mean                       = "Mean";
	public const string Median                     = "Median";
	public const string Fastest                    = "Fastest";
	public const string Quintile1                  = "Quintile 1";
	public const string Quintile2                  = "Quintile 2";
	public const string Quintile3                  = "Quintile 3";
	public const string Quintile4                  = "Quintile 4";
	public const string Quintile5                  = "Quintile 5";
	public const string NinetyPercent              = "90%";
	public const string NinetyFivePercent          = "95%";
	public const string NinetyNinePercent          = "99%";
	public const string NinetyNinePointFivePercent = "99.5%";
	public const string NinetyNinePointNinePercent = "99.9%";
#pragma warning restore CS1591
}