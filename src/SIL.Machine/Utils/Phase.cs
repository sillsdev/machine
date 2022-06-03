namespace SIL.Machine.Utils
{
    public class Phase
    {
        public Phase(string message = null, double percentage = 0, bool reportSteps = true)
        {
            Message = message;
            Percentage = percentage;
            ReportSteps = reportSteps;
        }

        public string Message { get; }
        public double Percentage { get; }
        public bool ReportSteps { get; }
    }
}
