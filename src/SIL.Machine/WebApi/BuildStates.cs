namespace SIL.Machine.WebApi
{
	public static class BuildStates
	{
		public const string Pending = "PENDING";
		public const string Active = "ACTIVE";
		public const string Completed = "COMPLETED";
		public const string Faulted = "FAULTED";
		public const string Canceled = "CANCELED";

		public static bool IsFinished(string state)
		{
			return state == Completed || state == Faulted || state == Canceled;
		}
	}
}
