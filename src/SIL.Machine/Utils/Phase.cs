namespace SIL.Machine.Utils
{
	public class Phase
	{
		public Phase(string message = null, double percentage = 0)
		{
			Message = message;
			Percentage = percentage;
		}

		public string Message { get; }
		public double Percentage { get; }
	}
}
