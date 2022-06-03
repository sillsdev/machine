namespace SIL.Machine.Optimization
{
    public enum MinimizationExitCondition
    {
        None,
        Converged,
        MaxFunctionEvaluations
    }

    public class MinimizationResult
    {
        public MinimizationResult(
            MinimizationExitCondition reasonForExit,
            Vector minimizingPoint,
            double errorValue,
            int functionEvaluationCount
        )
        {
            ReasonForExit = reasonForExit;
            MinimizingPoint = minimizingPoint;
            ErrorValue = errorValue;
            FunctionEvaluationCount = functionEvaluationCount;
        }

        public MinimizationExitCondition ReasonForExit { get; }

        public Vector MinimizingPoint { get; }

        public double ErrorValue { get; }

        public int FunctionEvaluationCount { get; }
    }
}
