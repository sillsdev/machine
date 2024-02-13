using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace SIL.Machine.Optimization
{
    public class Vector : IEnumerable<double>
    {
        private readonly double[] _components;

        public Vector(int dimensions)
        {
            _components = new double[dimensions];
        }

        public Vector(IEnumerable<double> components)
        {
            _components = components.ToArray();
        }

        public double this[int index]
        {
            get { return _components[index]; }
            set { _components[index] = value; }
        }

        /// <summary>
        /// Add another vector to this one
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public Vector Add(Vector v)
        {
            if (v.Count != Count)
                throw new ArgumentException("Can only add vectors of the same dimensionality");

            var vector = new Vector(v.Count);
            for (int i = 0; i < v.Count; i++)
            {
                vector[i] = this[i] + v[i];
            }
            return vector;
        }

        /// <summary>
        /// Subtract another vector from this one
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public Vector Subtract(Vector v)
        {
            if (v.Count != Count)
                throw new ArgumentException("Can only subtract vectors of the same dimensionality");

            var vector = new Vector(v.Count);
            for (int i = 0; i < v.Count; i++)
            {
                vector[i] = this[i] - v[i];
            }
            return vector;
        }

        /// <summary>
        /// Multiply this vector by a scalar value
        /// </summary>
        /// <param name="scalar"></param>
        /// <returns></returns>
        public Vector Multiply(double scalar)
        {
            var scaledVector = new Vector(Count);
            for (int i = 0; i < Count; i++)
            {
                scaledVector[i] = this[i] * scalar;
            }
            return scaledVector;
        }

        /// <summary>
        /// Compute the dot product of this vector and the given vector
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public double DotProduct(Vector v)
        {
            if (v.Count != Count)
                throw new ArgumentException("Can only compute dot product for vectors of the same dimensionality");

            double sum = 0;
            for (int i = 0; i < v.Count; i++)
            {
                sum += this[i] * v[i];
            }
            return sum;
        }

        public override string ToString()
        {
            string[] components = new string[_components.Length];
            for (int i = 0; i < components.Length; i++)
                components[i] = _components[i].ToString(CultureInfo.InvariantCulture);
            return "[ " + string.Join(", ", components) + " ]";
        }

        public IEnumerator<double> GetEnumerator()
        {
            return ((IList<double>)_components).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public int Count
        {
            get { return _components.Length; }
        }
    }
}
