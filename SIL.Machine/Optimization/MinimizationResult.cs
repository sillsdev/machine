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
		private readonly MinimizationExitCondition _reasonForExit;
		private readonly int _functionEvaluationCount;
		private readonly Vector _minimizingPoint;
		private readonly double _errorValue;

		public MinimizationResult(MinimizationExitCondition reasonForExit, Vector minimizingPoint, double errorValue, int functionEvaluationCount)
		{
			_reasonForExit = reasonForExit;
			_minimizingPoint = minimizingPoint;
			_errorValue = errorValue;
			_functionEvaluationCount = functionEvaluationCount;
		}

		public MinimizationExitCondition ReasonForExit
		{
			get { return _reasonForExit; }
		}

		public Vector MinimizingPoint
		{
			get { return _minimizingPoint; }
		}

		public double ErrorValue
		{
			get { return _errorValue; }
		}

		public int FunctionEvaluationCount
		{
			get { return _functionEvaluationCount; }
		}
	}
}
