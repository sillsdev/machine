﻿// Converted from code released with a MIT license available at https://code.google.com/p/nelder-mead-simplex/
using System;
using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine.Optimization
{
    /// <summary>
    /// Class implementing the Nelder-Mead simplex algorithm, used to find a minima when no gradient is available.
    /// Called fminsearch() in Matlab. A description of the algorithm can be found at
    /// http://se.mathworks.com/help/matlab/math/optimizing-nonlinear-functions.html#bsgpq6p-11
    /// or
    /// https://en.wikipedia.org/wiki/Nelder%E2%80%93Mead_method
    /// </summary>
    public sealed class NelderMeadSimplex
    {
        public double ConvergenceTolerance { get; set; }
        public int MaxFunctionEvaluations { get; set; }
        public double Scale { get; set; }

        public NelderMeadSimplex(double convergenceTolerance, int maxFunctionEvaluations, double scale)
        {
            ConvergenceTolerance = convergenceTolerance;
            MaxFunctionEvaluations = maxFunctionEvaluations;
            Scale = scale;
        }

        /// <summary>
        /// Finds the minimum of the objective function with an initial perturbation
        /// </summary>
        public MinimizationResult FindMinimum(
            Func<Vector, int, double> objectiveFunction,
            IEnumerable<double> initialGuess
        )
        {
            // confirm that we are in a position to commence
            if (objectiveFunction == null)
                throw new ArgumentNullException("objectiveFunction");

            if (initialGuess == null)
                throw new ArgumentNullException("initialGuess");

            // create the initial simplex
            var initialGuessVector = new Vector(initialGuess);
            Vector[] vertices = InitializeVertices(initialGuessVector);

            int evaluationCount = 0;
            MinimizationExitCondition exitCondition;
            ErrorProfile errorProfile;

            double[] errorValues = InitializeErrorValues(vertices, objectiveFunction);

            // iterate until we converge, or complete our permitted number of iterations
            while (true)
            {
                errorProfile = EvaluateSimplex(errorValues);

                // see if the range in point heights is small enough to exit
                if (HasConverged(errorValues))
                {
                    exitCondition = MinimizationExitCondition.Converged;
                    break;
                }

                // attempt a reflection of the simplex
                double reflectionPointValue = TryToScaleSimplex(
                    -1.0,
                    errorProfile,
                    vertices,
                    errorValues,
                    objectiveFunction,
                    evaluationCount
                );
                ++evaluationCount;
                if (reflectionPointValue <= errorValues[errorProfile.LowestIndex])
                {
                    // it's better than the best point, so attempt an expansion of the simplex
                    TryToScaleSimplex(2.0, errorProfile, vertices, errorValues, objectiveFunction, evaluationCount);
                    ++evaluationCount;
                }
                else if (reflectionPointValue >= errorValues[errorProfile.NextHighestIndex])
                {
                    // it would be worse than the second best point, so attempt a contraction to look
                    // for an intermediate point
                    double currentWorst = errorValues[errorProfile.HighestIndex];
                    double contractionPointValue = TryToScaleSimplex(
                        0.5,
                        errorProfile,
                        vertices,
                        errorValues,
                        objectiveFunction,
                        evaluationCount
                    );
                    ++evaluationCount;
                    if (contractionPointValue >= currentWorst)
                    {
                        // that would be even worse, so let's try to contract uniformly towards the low point;
                        // don't bother to update the error profile, we'll do it at the start of the
                        // next iteration
                        ShrinkSimplex(errorProfile, vertices, errorValues, objectiveFunction, evaluationCount);
                        evaluationCount += vertices.Length - 1; // that required one function evaluation for each vertex; keep track
                    }
                }
                // check to see if we have exceeded our alloted number of evaluations
                if (evaluationCount >= MaxFunctionEvaluations)
                {
                    exitCondition = MinimizationExitCondition.MaxFunctionEvaluations;
                    break;
                }
            }
            return new MinimizationResult(
                exitCondition,
                vertices[errorProfile.LowestIndex],
                errorValues[errorProfile.LowestIndex],
                evaluationCount
            );
        }

        /// <summary>
        /// Evaluate the objective function at each vertex to create a corresponding
        /// list of error values for each vertex
        /// </summary>
        /// <param name="vertices"></param>
        /// <param name="objectiveFunction"></param>
        /// <returns></returns>
        private static double[] InitializeErrorValues(Vector[] vertices, Func<Vector, int, double> objectiveFunction)
        {
            double[] errorValues = new double[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
                errorValues[i] = objectiveFunction(vertices[i], -1);
            return errorValues;
        }

        /// <summary>
        /// Check whether the points in the error profile have so little range that we
        /// consider ourselves to have converged
        /// </summary>
        private bool HasConverged(double[] errorValues)
        {
            double avg = errorValues.Average();
            double range = 0;
            foreach (double ev in errorValues)
                range += Math.Pow(ev - avg, 2) / (errorValues.Length - 1);
            range = Math.Sqrt(range);

            return range < ConvergenceTolerance;
        }

        /// <summary>
        /// Examine all error values to determine the ErrorProfile
        /// </summary>
        private static ErrorProfile EvaluateSimplex(double[] errorValues)
        {
            var errorProfile = new ErrorProfile();
            if (errorValues[0] > errorValues[1])
            {
                errorProfile.HighestIndex = 0;
                errorProfile.NextHighestIndex = 1;
            }
            else
            {
                errorProfile.HighestIndex = 1;
                errorProfile.NextHighestIndex = 0;
            }

            for (int index = 0; index < errorValues.Length; index++)
            {
                double errorValue = errorValues[index];
                if (errorValue <= errorValues[errorProfile.LowestIndex])
                    errorProfile.LowestIndex = index;
                if (errorValue > errorValues[errorProfile.HighestIndex])
                {
                    // downgrade the current highest to next highest
                    errorProfile.NextHighestIndex = errorProfile.HighestIndex;
                    errorProfile.HighestIndex = index;
                }
                else if (errorValue > errorValues[errorProfile.NextHighestIndex] && index != errorProfile.HighestIndex)
                    errorProfile.NextHighestIndex = index;
            }

            return errorProfile;
        }

        /// <summary>
        /// Construct an initial simplex, given starting guesses for the constants, and
        /// initial step sizes for each dimension
        /// </summary>
        private Vector[] InitializeVertices(Vector initialGuess)
        {
            int numDimensions = initialGuess.Count;
            var vertices = new Vector[numDimensions + 1];

            double pn = Scale * (Math.Sqrt(numDimensions + 1) - 1 + numDimensions) / (numDimensions * Math.Sqrt(2));
            double qn = Scale * (Math.Sqrt(numDimensions + 1) - 1) / (numDimensions * Math.Sqrt(2));

            // define one point of the simplex as the given initial guesses
            var p0 = new Vector(numDimensions);
            for (int i = 0; i < numDimensions; i++)
                p0[i] = initialGuess[i];

            vertices[0] = p0;
            for (int i = 0; i < numDimensions; i++)
            {
                var v = new Vector(numDimensions);
                for (int j = 0; j < numDimensions; j++)
                    v[j] = i == j ? pn : qn;
                vertices[i + 1] = p0.Add(v);
            }
            return vertices;
        }

        /// <summary>
        /// Test a scaling operation of the high point, and replace it if it is an improvement
        /// </summary>
        private static double TryToScaleSimplex(
            double scaleFactor,
            ErrorProfile errorProfile,
            Vector[] vertices,
            double[] errorValues,
            Func<Vector, int, double> objectiveFunction,
            int evaluationCount
        )
        {
            // find the centroid through which we will reflect
            Vector centroid = ComputeCentroid(vertices, errorProfile);

            // define the vector from the centroid to the high point
            Vector centroidToHighPoint = vertices[errorProfile.HighestIndex].Subtract(centroid);

            // scale and position the vector to determine the new trial point
            Vector newPoint = centroidToHighPoint.Multiply(scaleFactor).Add(centroid);

            // evaluate the new point
            double newErrorValue = objectiveFunction(newPoint, evaluationCount);

            // if it's better, replace the old high point
            if (newErrorValue < errorValues[errorProfile.HighestIndex])
            {
                vertices[errorProfile.HighestIndex] = newPoint;
                errorValues[errorProfile.HighestIndex] = newErrorValue;
            }

            return newErrorValue;
        }

        /// <summary>
        /// Contract the simplex uniformly around the lowest point
        /// </summary>
        private static void ShrinkSimplex(
            ErrorProfile errorProfile,
            Vector[] vertices,
            double[] errorValues,
            Func<Vector, int, double> objectiveFunction,
            int evaluationCount
        )
        {
            Vector lowestVertex = vertices[errorProfile.LowestIndex];
            for (int i = 0; i < vertices.Length; i++)
            {
                if (i != errorProfile.LowestIndex)
                {
                    vertices[i] = vertices[i].Add(lowestVertex).Multiply(0.5);
                    errorValues[i] = objectiveFunction(vertices[i], evaluationCount);
                }
            }
        }

        /// <summary>
        /// Compute the centroid of all points except the worst
        /// </summary>
        private static Vector ComputeCentroid(Vector[] vertices, ErrorProfile errorProfile)
        {
            int numVertices = vertices.Length;
            // find the centroid of all points except the worst one
            var centroid = new Vector(numVertices - 1);
            for (int i = 0; i < numVertices; i++)
            {
                if (i != errorProfile.HighestIndex)
                    centroid = centroid.Add(vertices[i]);
            }
            return centroid.Multiply(1.0d / (numVertices - 1));
        }

        private sealed class ErrorProfile
        {
            public int HighestIndex { get; set; }
            public int NextHighestIndex { get; set; }
            public int LowestIndex { get; set; }
        }
    }
}
