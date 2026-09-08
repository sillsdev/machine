using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SIL.Extensions;
using SIL.Machine.DataStructures;
using SIL.Machine.FeatureModel.Fluent;
using SIL.ObjectModel;

namespace SIL.Machine.FeatureModel
{
    public class FeatureStruct : FeatureValue, ICloneable<FeatureStruct>, IFreezable, IValueEquatable<FeatureStruct>
    {
        public static IFeatureStructSyntax New()
        {
            return new FeatureStructBuilder();
        }

        public static IFeatureStructSyntax New(FeatureSystem featSys)
        {
            return new FeatureStructBuilder(featSys);
        }

        public static IFeatureStructSyntax New(FeatureStruct fs)
        {
            return new FeatureStructBuilder(fs.Clone());
        }

        public static IFeatureStructSyntax New(FeatureSystem featSys, FeatureStruct fs)
        {
            return new FeatureStructBuilder(featSys, fs.Clone());
        }

        public static IFeatureStructSyntax NewMutable()
        {
            return new FeatureStructBuilder(true);
        }

        public static IFeatureStructSyntax NewMutable(FeatureSystem featSys)
        {
            return new FeatureStructBuilder(featSys, true);
        }

        public static IFeatureStructSyntax NewMutable(FeatureStruct fs)
        {
            return new FeatureStructBuilder(fs.Clone(), true);
        }

        public static IFeatureStructSyntax NewMutable(FeatureSystem featSys, FeatureStruct fs)
        {
            return new FeatureStructBuilder(featSys, fs.Clone(), true);
        }

        private readonly IDBearerDictionary<Feature, FeatureValue> _definite;
        private int? _hashCode;

        // Computed once, at freeze time, by FreezeImpl: true iff this structure contains no nested FeatureStruct
        // and no FeatureValue instance reachable from it is visited more than once (see FreezeImpl). Always
        // false until Freeze()/GetFrozenHashCode() has run; a new (unfrozen) instance -- including one produced
        // by Clone() -- always starts with the default false, so there is no stale-flag carryover to worry
        // about. Read only in combination with IsFrozen (see ValueEquals) since it has no meaning otherwise.
        private bool _isTree;

        // Thread-local pool of the scratch collections that guard FeatureStruct's recursive top-level walks
        // (ValueEquals/Freeze/GetFrozenHashCode) against cyclic/reentrant graphs. A CPU profile of a heavy parse
        // found these HashSet/Dictionary allocations -- and the resizes they triggered -- among the largest
        // GC-triggering allocators, even after 325f8419 made them lazily-allocated (they still get allocated on
        // the first non-identical comparison). Each thread now reuses one instance across calls instead of
        // allocating a fresh HashSet/HashSet/Dictionary triple (ValueEquals) or HashSet (Freeze) every time.
        //
        // A single InUse flag guards the whole pool: if a top-level call is already using it -- e.g. re-entered
        // from inside another comparison, such as a custom IEqualityComparer or an overridden ValueEquals
        // invoked mid-walk -- the nested call falls back to fresh, unpooled allocations, exactly the behavior
        // this code had before pooling existed. Correctness never depends on the pool being available.
        private sealed class VisitedSetsPool
        {
            public readonly HashSet<FeatureValue> Self = new HashSet<FeatureValue>();
            public readonly HashSet<FeatureValue> Other = new HashSet<FeatureValue>();
            public readonly Dictionary<FeatureValue, FeatureValue> Pairs = new Dictionary<FeatureValue, FeatureValue>(
                64
            );
            public readonly HashSet<FeatureValue> FreezeVisited = new HashSet<FeatureValue>();
            public bool InUse;
        }

        [ThreadStatic]
        private static VisitedSetsPool Pool;

        private static VisitedSetsPool GetPool()
        {
            VisitedSetsPool pool = Pool;
            if (pool == null)
            {
                pool = new VisitedSetsPool();
                Pool = pool;
            }
            return pool;
        }

        // Thread-local pool of the "copies" dictionary used by the top-level Clone() entry point to preserve
        // sharing (reentrant/shared sub-structures and leaves) during a deep copy. Same InUse-guarded, fall-back-
        // to-fresh-allocation design as VisitedSetsPool above, for the same reason: a profile of a heavy parse
        // found this dictionary's resizes among the largest GC-triggering allocators.
        private sealed class CopiesPool
        {
            public readonly Dictionary<FeatureValue, FeatureValue> Copies = new Dictionary<FeatureValue, FeatureValue>(
                64
            );
            public bool InUse;
        }

        [ThreadStatic]
        private static CopiesPool CopiesPoolInstance;

        private static CopiesPool GetCopiesPool()
        {
            CopiesPool pool = CopiesPoolInstance;
            if (pool == null)
            {
                pool = new CopiesPool();
                CopiesPoolInstance = pool;
            }
            return pool;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FeatureStruct"/> class.
        /// </summary>
        public FeatureStruct()
        {
            _definite = new IDBearerDictionary<Feature, FeatureValue>();
        }

        /// <summary>
        /// Top-level copy constructor, used only by <see cref="Clone"/>. Unlike the private copy constructor
        /// below (used for recursive cloning within an already-in-progress, externally-supplied copies map),
        /// this is always a self-contained clone operation with no outside sharing context, so it is free to
        /// skip the copies map entirely when <paramref name="other"/> is frozen and <c>_isTree</c> -- proven, at
        /// <paramref name="other"/>'s own freeze time, to contain no nested FeatureStruct and no repeated
        /// FeatureValue instance anywhere reachable from it (see FreezeImpl), a proof that still holds since
        /// frozen structures are immutable. With nothing to preserve, cloning each value independently (no map)
        /// produces an identical result to using one, since the map could never have found a match anyway.
        /// Otherwise, falls back to a pooled (or, if already in use, freshly allocated) copies dictionary.
        /// </summary>
        protected FeatureStruct(FeatureStruct other)
            : this()
        {
            if (other.IsFrozen && other._isTree)
            {
                foreach (KeyValuePair<Feature, FeatureValue> featVal in other._definite)
                    _definite[featVal.Key] = Dereference(featVal.Value).CloneImpl(null);
                return;
            }

            CopiesPool pool = GetCopiesPool();
            if (pool.InUse)
            {
                var fallbackCopies = new Dictionary<FeatureValue, FeatureValue>();
                PopulateClone(other, fallbackCopies);
                return;
            }

            pool.InUse = true;
            try
            {
                PopulateClone(other, pool.Copies);
            }
            finally
            {
                pool.Copies.Clear();
                pool.InUse = false;
            }
        }

        private void PopulateClone(FeatureStruct other, IDictionary<FeatureValue, FeatureValue> copies)
        {
            copies[other] = this;
            foreach (KeyValuePair<Feature, FeatureValue> featVal in other._definite)
                _definite[featVal.Key] = Dereference(featVal.Value).CloneImpl(copies);
        }

        /// <summary>
        /// Copy constructor used for recursive cloning within an already-in-progress deep copy that supplies its
        /// own <paramref name="copies"/> map (e.g. a parent structure's Clone(), or Unify/PriorityUnion). Always
        /// uses the caller's map as-is -- it may itself already be shared/reentrant sub-structure state that must
        /// be preserved, regardless of whether this particular <paramref name="other"/> is individually a tree.
        /// </summary>
        /// <param name="other">The fs.</param>
        /// <param name="copies"></param>
        private FeatureStruct(FeatureStruct other, IDictionary<FeatureValue, FeatureValue> copies)
            : this()
        {
            PopulateClone(other, copies);
        }

        /// <summary>
        /// Gets the features.
        /// </summary>
        /// <value>The features.</value>
        public IReadOnlyCollection<Feature> Features
        {
            get { return _definite.Keys.ToReadOnlyCollection(); }
        }

        public bool HasVariables
        {
            get { return DetermineHasVariables(new HashSet<FeatureStruct>()); }
        }

        public bool IsEmpty
        {
            get { return _definite.Count == 0; }
        }

        private bool DetermineHasVariables(ISet<FeatureStruct> visited)
        {
            if (visited.Contains(this))
                return false;

            visited.Add(this);

            foreach (FeatureValue value in _definite.Values)
            {
                if (value is FeatureStruct childFS)
                {
                    if (childFS.DetermineHasVariables(visited))
                        return true;
                }
                else if (((SimpleFeatureValue)value).IsVariable)
                {
                    return true;
                }
            }

            return false;
        }

        public void AddValue(SymbolicFeature feature, IEnumerable<FeatureSymbol> values)
        {
            if (values == null)
                throw new ArgumentNullException("values");

            FeatureSymbol[] vals = values.ToArray();
            AddValue(feature, vals.Length == 0 ? new SymbolicFeatureValue(feature) : new SymbolicFeatureValue(vals));
        }

        public void AddValue(SymbolicFeature feature, params FeatureSymbol[] values)
        {
            AddValue(
                feature,
                values.Length == 0 ? new SymbolicFeatureValue(feature) : new SymbolicFeatureValue(values)
            );
        }

        public void AddValue(StringFeature feature, IEnumerable<string> values)
        {
            AddValue(feature, false, values);
        }

        public void AddValue(StringFeature feature, bool not, IEnumerable<string> values)
        {
            if (values == null)
                throw new ArgumentNullException("values");

            AddValue(feature, new StringFeatureValue(values, not));
        }

        public void AddValue(StringFeature feature, params string[] values)
        {
            AddValue(feature, false, values);
        }

        public void AddValue(StringFeature feature, bool not, params string[] values)
        {
            AddValue(feature, new StringFeatureValue(values, not));
        }

        /// <summary>
        /// Adds the specified feature-value pair.
        /// </summary>
        /// <param name="feature">The feature.</param>
        /// <param name="value">The value.</param>
        public void AddValue(Feature feature, FeatureValue value)
        {
            if (feature == null)
                throw new ArgumentNullException("feature");
            if (value == null)
                throw new ArgumentNullException("value");

            CheckFrozen();
            _definite[feature] = value;
        }

        public void AddValue(IEnumerable<Feature> path, FeatureValue value)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            if (value == null)
                throw new ArgumentNullException("value");

            CheckFrozen();
            Feature lastFeature;
            FeatureStruct lastFS;
            if (FollowPath(path, out lastFeature, out lastFS))
                lastFS._definite[lastFeature] = value;

            throw new ArgumentException("The feature path is invalid.", "path");
        }

        public void RemoveValue(Feature feature)
        {
            if (feature == null)
                throw new ArgumentNullException("feature");

            CheckFrozen();
            _definite.Remove(feature);
        }

        public void RemoveValue(IEnumerable<Feature> path)
        {
            if (path == null)
                throw new ArgumentNullException("path");

            CheckFrozen();
            Feature lastFeature;
            FeatureStruct lastFS;
            if (FollowPath(path, out lastFeature, out lastFS))
                lastFS._definite.Remove(lastFeature);

            throw new ArgumentException("The feature path is invalid.", "path");
        }

        public void ReplaceVariables(VariableBindings varBindings)
        {
            CheckFrozen();
            ReplaceVariables(varBindings, new HashSet<FeatureStruct>());
        }

        private void ReplaceVariables(VariableBindings varBindings, ISet<FeatureStruct> visited)
        {
            if (visited.Contains(this))
                return;

            visited.Add(this);

            var replacements = new Dictionary<Feature, FeatureValue>();
            foreach (KeyValuePair<Feature, FeatureValue> featVal in _definite)
            {
                FeatureValue value = Dereference(featVal.Value);
                if (value is SimpleFeatureValue sfv)
                {
                    if (sfv.IsVariable)
                    {
                        SimpleFeatureValue binding;
                        if (varBindings.TryGetValue(sfv.VariableName, out binding))
                            replacements[featVal.Key] = binding.GetVariableValue(sfv.Agree);
                    }
                }
                else
                {
                    var fs = (FeatureStruct)value;
                    fs.ReplaceVariables(varBindings, visited);
                }
            }

            foreach (KeyValuePair<Feature, FeatureValue> replacement in replacements)
                _definite[replacement.Key] = replacement.Value;
        }

        public void RemoveVariables()
        {
            CheckFrozen();
            RemoveVariables(new HashSet<FeatureStruct>());
        }

        private void RemoveVariables(ISet<FeatureStruct> visited)
        {
            if (visited.Contains(this))
                return;

            visited.Add(this);

            foreach (KeyValuePair<Feature, FeatureValue> featVal in _definite.ToArray())
            {
                FeatureValue value = Dereference(featVal.Value);
                if (value is SimpleFeatureValue sfv)
                {
                    if (sfv.IsVariable)
                        _definite.Remove(featVal.Key);
                }
                else
                {
                    var fs = (FeatureStruct)value;
                    fs.RemoveVariables(visited);
                    if (fs.IsEmpty)
                        _definite.Remove(featVal.Key);
                }
            }
        }

        public void PriorityUnion(FeatureStruct other)
        {
            PriorityUnion(other, null);
        }

        public void PriorityUnion(FeatureStruct other, VariableBindings varBindings)
        {
            if (other == null)
                throw new ArgumentNullException("other");

            CheckFrozen();
            PriorityUnion(other, varBindings, new Dictionary<FeatureValue, FeatureValue>());
        }

        private void PriorityUnion(
            FeatureStruct other,
            VariableBindings varBindings,
            IDictionary<FeatureValue, FeatureValue> copies
        )
        {
            other = Dereference(other);

            copies[other] = this;

            foreach (KeyValuePair<Feature, FeatureValue> featVal in _definite)
            {
                FeatureValue thisValue = Dereference(featVal.Value);
                FeatureValue otherValue;
                if (other._definite.TryGetValue(featVal.Key, out otherValue))
                {
                    otherValue = Dereference(otherValue);
                    if (otherValue is FeatureStruct otherFS && !copies.ContainsKey(otherFS))
                    {
                        var thisFS = thisValue as FeatureStruct;
                        thisFS?.PriorityUnion(otherFS, varBindings, copies);
                    }
                }
            }

            foreach (KeyValuePair<Feature, FeatureValue> featVal in other._definite)
            {
                FeatureValue otherValue = Dereference(featVal.Value);
                FeatureValue thisValue;
                if (_definite.TryGetValue(featVal.Key, out thisValue))
                {
                    otherValue = Dereference(otherValue);
                    if (otherValue is FeatureStruct otherFS)
                    {
                        if (thisValue is FeatureStruct)
                        {
                            FeatureValue reentrant;
                            if (copies.TryGetValue(otherFS, out reentrant))
                                _definite[featVal.Key] = reentrant;
                        }
                        else
                        {
                            _definite[featVal.Key] = otherFS.CloneImpl(copies);
                        }
                    }
                    else
                    {
                        var otherSfv = (SimpleFeatureValue)otherValue;
                        SimpleFeatureValue binding;
                        if (
                            otherSfv.IsVariable
                            && varBindings != null
                            && varBindings.TryGetValue(otherSfv.VariableName, out binding)
                        )
                        {
                            _definite[featVal.Key] = binding.GetVariableValue(otherSfv.Agree);
                        }
                        else
                        {
                            _definite[featVal.Key] = otherSfv.CloneImpl(copies);
                        }
                    }
                }
                else
                {
                    _definite[featVal.Key] = otherValue.CloneImpl(copies);
                }
            }
        }

        public void Union(FeatureStruct other)
        {
            Union(other, null);
        }

        public void Union(FeatureStruct other, VariableBindings varBindings)
        {
            if (other == null)
                throw new ArgumentNullException("other");

            CheckFrozen();
            UnionImpl(other, varBindings, new Dictionary<FeatureStruct, ISet<FeatureStruct>>());
        }

        internal override bool UnionImpl(
            FeatureValue other,
            VariableBindings varBindings,
            IDictionary<FeatureStruct, ISet<FeatureStruct>> visited
        )
        {
            FeatureStruct otherFS;
            if (Dereference(other, out otherFS))
            {
                ISet<FeatureStruct> visitedOthers = visited.GetOrCreate(this, () => new HashSet<FeatureStruct>());
                if (!visitedOthers.Contains(otherFS))
                {
                    visitedOthers.Add(otherFS);

                    foreach (KeyValuePair<Feature, FeatureValue> featVal in otherFS._definite)
                    {
                        FeatureValue otherValue = Dereference(featVal.Value);
                        FeatureValue thisValue;
                        if (_definite.TryGetValue(featVal.Key, out thisValue))
                        {
                            thisValue = Dereference(thisValue);
                            if (!thisValue.UnionImpl(otherValue, varBindings, visited))
                                _definite.Remove(featVal.Key);
                        }
                    }

                    _definite.RemoveAll(kvp => !otherFS._definite.ContainsKey(kvp.Key));
                }
            }
            return _definite.Count > 0;
        }

        public void Add(FeatureStruct other)
        {
            Add(other, null);
        }

        public void Add(FeatureStruct other, VariableBindings varBindings)
        {
            if (other == null)
                throw new ArgumentNullException("other");

            CheckFrozen();
            AddImpl(other, varBindings, new Dictionary<FeatureStruct, ISet<FeatureStruct>>());
        }

        internal override bool AddImpl(
            FeatureValue other,
            VariableBindings varBindings,
            IDictionary<FeatureStruct, ISet<FeatureStruct>> visited
        )
        {
            FeatureStruct otherFS;
            if (Dereference(other, out otherFS))
            {
                ISet<FeatureStruct> visitedOthers = visited.GetOrCreate(this, () => new HashSet<FeatureStruct>());
                if (!visitedOthers.Contains(otherFS))
                {
                    visitedOthers.Add(otherFS);

                    foreach (KeyValuePair<Feature, FeatureValue> featVal in otherFS._definite)
                    {
                        FeatureValue otherValue = Dereference(featVal.Value);
                        FeatureValue thisValue;
                        if (_definite.TryGetValue(featVal.Key, out thisValue))
                        {
                            thisValue = Dereference(thisValue);
                        }
                        else
                        {
                            if (otherValue is FeatureStruct)
                                thisValue = new FeatureStruct();
                            else if (otherValue is StringFeatureValue)
                                thisValue = new StringFeatureValue();
                            else
                                thisValue = new SymbolicFeatureValue((SymbolicFeature)featVal.Key);
                            _definite[featVal.Key] = thisValue;
                        }
                        if (!thisValue.AddImpl(otherValue, varBindings, visited))
                            _definite.Remove(featVal.Key);
                    }
                }
            }
            return _definite.Count > 0;
        }

        public void Subtract(FeatureStruct other)
        {
            Subtract(other, null);
        }

        public void Subtract(FeatureStruct other, VariableBindings varBindings)
        {
            if (other == null)
                throw new ArgumentNullException("other");

            CheckFrozen();
            SubtractImpl(other, varBindings, new Dictionary<FeatureStruct, ISet<FeatureStruct>>());
        }

        internal override bool SubtractImpl(
            FeatureValue other,
            VariableBindings varBindings,
            IDictionary<FeatureStruct, ISet<FeatureStruct>> visited
        )
        {
            FeatureStruct otherFS;
            if (Dereference(other, out otherFS))
            {
                ISet<FeatureStruct> visitedOthers = visited.GetOrCreate(this, () => new HashSet<FeatureStruct>());
                if (!visitedOthers.Contains(otherFS))
                {
                    visitedOthers.Add(otherFS);

                    foreach (KeyValuePair<Feature, FeatureValue> featVal in otherFS._definite)
                    {
                        FeatureValue otherValue = Dereference(featVal.Value);
                        FeatureValue thisValue;
                        if (_definite.TryGetValue(featVal.Key, out thisValue))
                        {
                            thisValue = Dereference(thisValue);
                            if (!thisValue.SubtractImpl(otherValue, varBindings, visited))
                                _definite.Remove(featVal.Key);
                        }
                    }
                }
            }
            return _definite.Count > 0;
        }

        public void Clear()
        {
            CheckFrozen();
            _definite.Clear();
        }

        /// <summary>
        /// Gets the values for the specified feature.
        /// </summary>
        /// <param name="feature">The feature.</param>
        /// <returns>All values.</returns>
        public FeatureValue GetValue(Feature feature)
        {
            FeatureValue value;
            if (TryGetValue(feature, out value))
                return value;

            throw new ArgumentException("The specified value could not be found.", "feature");
        }

        public StringFeatureValue GetValue(StringFeature feature)
        {
            StringFeatureValue value;
            if (TryGetValue(feature, out value))
                return value;

            throw new ArgumentException("The specified value could not be found.", "feature");
        }

        public SymbolicFeatureValue GetValue(SymbolicFeature feature)
        {
            SymbolicFeatureValue value;
            if (TryGetValue(feature, out value))
                return value;

            throw new ArgumentException("The specified value could not be found.", "feature");
        }

        public FeatureStruct GetValue(ComplexFeature feature)
        {
            FeatureStruct value;
            if (TryGetValue(feature, out value))
                return value;

            throw new ArgumentException("The specified value could not be found.", "feature");
        }

        public T GetValue<T>(Feature feature)
            where T : FeatureValue
        {
            T value;
            if (TryGetValue(feature, out value))
                return value;

            throw new ArgumentException("The specified value could not be found.", "feature");
        }

        public FeatureValue GetValue(string featureID)
        {
            FeatureValue value;
            if (TryGetValue(featureID, out value))
                return value;

            throw new ArgumentException("The specified value could not be found.", "featureID");
        }

        public T GetValue<T>(string featureID)
            where T : FeatureValue
        {
            T value;
            if (TryGetValue(featureID, out value))
                return value;

            throw new ArgumentException("The specified value could not be found.", "featureID");
        }

        public FeatureValue GetValue(IEnumerable<Feature> path)
        {
            FeatureValue value;
            if (TryGetValue(path, out value))
                return value;

            throw new ArgumentException("The specified path is not valid.", "path");
        }

        public FeatureValue GetValue(params Feature[] path)
        {
            return GetValue((IEnumerable<Feature>)path);
        }

        public T GetValue<T>(IEnumerable<Feature> path)
            where T : FeatureValue
        {
            T value;
            if (TryGetValue(path, out value))
                return value;

            throw new ArgumentException("The specified path is not valid.", "path");
        }

        public T GetValue<T>(params Feature[] path)
            where T : FeatureValue
        {
            return GetValue<T>((IEnumerable<Feature>)path);
        }

        public FeatureValue GetValue(IEnumerable<string> path)
        {
            FeatureValue value;
            if (TryGetValue(path, out value))
                return value;

            throw new ArgumentException("The specified path is not valid.", "path");
        }

        public FeatureValue GetValue(params string[] path)
        {
            return GetValue((IEnumerable<string>)path);
        }

        public T GetValue<T>(IEnumerable<string> path)
            where T : FeatureValue
        {
            T value;
            if (TryGetValue(path, out value))
                return value;

            throw new ArgumentException("The specified path is not valid.", "path");
        }

        public T GetValue<T>(params string[] path)
            where T : FeatureValue
        {
            return GetValue<T>((IEnumerable<string>)path);
        }

        public bool TryGetValue<T>(Feature feature, out T value)
            where T : FeatureValue
        {
            if (feature == null)
                throw new ArgumentNullException("feature");

            FeatureValue val;
            if (_definite.TryGetValue(feature, out val))
                return Dereference(val, out value);
            value = null;
            return false;
        }

        public bool TryGetValue<T>(string featureID, out T value)
            where T : FeatureValue
        {
            if (featureID == null)
                throw new ArgumentNullException("featureID");

            FeatureValue val;
            if (_definite.TryGetValue(featureID, out val))
                return Dereference(val, out value);
            value = null;
            return false;
        }

        public bool TryGetValue<T>(IEnumerable<Feature> path, out T value)
            where T : FeatureValue
        {
            if (path == null)
                throw new ArgumentNullException("path");

            Feature lastFeature;
            FeatureStruct lastFS;
            if (FollowPath(path, out lastFeature, out lastFS))
            {
                FeatureValue val;
                if (lastFS._definite.TryGetValue(lastFeature, out val))
                    return Dereference(val, out value);
            }
            value = null;
            return false;
        }

        public bool TryGetValue<T>(IEnumerable<string> path, out T value)
            where T : FeatureValue
        {
            if (path == null)
                throw new ArgumentNullException("path");

            string lastID;
            FeatureStruct lastFS;
            if (FollowPath(path, out lastID, out lastFS))
            {
                FeatureValue val;
                if (lastFS._definite.TryGetValue(lastID, out val))
                    return Dereference(val, out value);
            }
            value = null;
            return false;
        }

        public bool ContainsFeature(Feature feature)
        {
            if (feature == null)
                throw new ArgumentNullException("feature");

            return _definite.ContainsKey(feature);
        }

        public bool ContainsFeature(string featureID)
        {
            if (featureID == null)
                throw new ArgumentNullException("featureID");

            return _definite.ContainsKey(featureID);
        }

        public bool ContainsFeature(IEnumerable<Feature> path)
        {
            if (path == null)
                throw new ArgumentNullException("path");

            Feature lastFeature;
            FeatureStruct lastFS;
            if (FollowPath(path, out lastFeature, out lastFS))
                return lastFS._definite.ContainsKey(lastFeature);
            return false;
        }

        public bool ContainsFeature(IEnumerable<string> path)
        {
            if (path == null)
                throw new ArgumentNullException("path");

            string lastID;
            FeatureStruct lastFS;
            if (FollowPath(path, out lastID, out lastFS))
                return lastFS._definite.ContainsKey(lastID);
            return false;
        }

        private bool FollowPath(IEnumerable<string> path, out string lastID, out FeatureStruct lastFS)
        {
            lastFS = this;
            lastID = null;
            foreach (string id in path)
            {
                if (lastID != null)
                {
                    FeatureValue curValue;
                    if (!lastFS._definite.TryGetValue(lastID, out curValue) || !Dereference(curValue, out lastFS))
                    {
                        lastID = null;
                        lastFS = null;
                        return false;
                    }
                }
                lastID = id;
            }

            return true;
        }

        private bool FollowPath(IEnumerable<Feature> path, out Feature lastFeature, out FeatureStruct lastFS)
        {
            lastFS = this;
            lastFeature = null;
            foreach (Feature feature in path)
            {
                if (lastFeature != null)
                {
                    FeatureValue curValue;
                    if (!lastFS._definite.TryGetValue(lastFeature, out curValue) || !Dereference(curValue, out lastFS))
                    {
                        lastFeature = null;
                        lastFS = null;
                        return false;
                    }
                }
                lastFeature = feature;
            }

            return true;
        }

        public bool IsUnifiable(FeatureStruct other)
        {
            return IsUnifiable(other, false);
        }

        public bool IsUnifiable(FeatureStruct other, bool useDefaults)
        {
            return IsUnifiable(other, useDefaults, null);
        }

        public bool IsUnifiable(FeatureStruct other, VariableBindings varBindings)
        {
            return IsUnifiable(other, false, varBindings);
        }

        /// <summary>
        /// Determines whether the specified set of feature values is compatible with this
        /// set of feature values. It is much like <c>Matches</c> except that if a the
        /// specified set does not contain a feature in this set, it is still a match.
        /// It basically checks to make sure that there is no contradictory features.
        /// </summary>
        /// <param name="other">The feature value.</param>
        /// <param name="useDefaults"></param>
        /// <param name="varBindings"></param>
        /// <returns>
        /// 	<c>true</c> the sets are compatible, otherwise <c>false</c>.
        /// </returns>
        public bool IsUnifiable(FeatureStruct other, bool useDefaults, VariableBindings varBindings)
        {
            if (other == null)
                throw new ArgumentNullException("other");

            other = Dereference(other);

            VariableBindings definiteVarBindings = varBindings?.Clone();
            if (IsUnifiableImpl(other, useDefaults, definiteVarBindings))
            {
                varBindings?.Replace(definiteVarBindings);
                return true;
            }
            return false;
        }

        internal override bool IsUnifiableImpl(FeatureValue other, bool useDefaults, VariableBindings varBindings)
        {
            FeatureStruct otherFS;
            if (!Dereference(other, out otherFS))
                return false;

            foreach (KeyValuePair<Feature, FeatureValue> featVal in otherFS._definite)
            {
                FeatureValue otherValue = Dereference(featVal.Value);
                FeatureValue thisValue;
                if (_definite.TryGetValue(featVal.Key, out thisValue))
                {
                    thisValue = Dereference(thisValue);
                    if (!thisValue.IsUnifiableImpl(otherValue, useDefaults, varBindings))
                        return false;
                }
                else if (useDefaults && featVal.Key.DefaultValue != null)
                {
                    if (!featVal.Key.DefaultValue.IsUnifiableImpl(otherValue, true, varBindings))
                        return false;
                }
            }
            return true;
        }

        public bool Unify(FeatureStruct other, out FeatureStruct output)
        {
            return Unify(other, false, out output);
        }

        public bool Unify(FeatureStruct other, bool useDefaults, out FeatureStruct output)
        {
            return Unify(other, useDefaults, null, out output);
        }

        public bool Unify(FeatureStruct other, VariableBindings varBindings, out FeatureStruct output)
        {
            return Unify(other, false, varBindings, out output);
        }

        public bool Unify(FeatureStruct other, bool useDefaults, VariableBindings varBindings, out FeatureStruct output)
        {
            if (other == null)
                throw new ArgumentNullException("other");

            other = Dereference(other);

            VariableBindings tempVarBindings = varBindings?.Clone();
            FeatureValue newFV;
            if (!UnifyImpl(other, useDefaults, tempVarBindings, out newFV))
            {
                output = null;
                return false;
            }

            varBindings?.Replace(tempVarBindings);
            output = (FeatureStruct)newFV;
            return true;
        }

        public bool Subsumes(FeatureStruct other)
        {
            return Subsumes(other, false);
        }

        public bool Subsumes(FeatureStruct other, bool useDefaults)
        {
            return Subsumes(other, useDefaults, null);
        }

        public bool Subsumes(FeatureStruct other, VariableBindings varBindings)
        {
            return Subsumes(other, false, varBindings);
        }

        public bool Subsumes(FeatureStruct other, bool useDefaults, VariableBindings varBindings)
        {
            if (other == null)
                throw new ArgumentNullException("other");

            other = Dereference(other);

            VariableBindings tempVarBindings = varBindings?.Clone();
            if (SubsumesImpl(other, useDefaults, tempVarBindings))
            {
                varBindings?.Replace(tempVarBindings);
                return true;
            }
            return false;
        }

        internal override bool SubsumesImpl(FeatureValue other, bool useDefaults, VariableBindings varBindings)
        {
            FeatureStruct otherFS;
            if (!Dereference(other, out otherFS))
                return false;

            foreach (KeyValuePair<Feature, FeatureValue> featVal in _definite)
            {
                FeatureValue thisValue = Dereference(featVal.Value);
                FeatureValue otherValue;
                if (otherFS._definite.TryGetValue(featVal.Key, out otherValue))
                {
                    otherValue = Dereference(otherValue);
                    if (!thisValue.SubsumesImpl(otherValue, useDefaults, varBindings))
                        return false;
                }
                else if (useDefaults && featVal.Key.DefaultValue != null)
                {
                    if (!thisValue.SubsumesImpl(featVal.Key.DefaultValue, true, varBindings))
                        return false;
                }
                else
                {
                    return false;
                }
            }
            return true;
        }

        internal override bool DestructiveUnify(
            FeatureValue other,
            bool useDefaults,
            bool preserveInput,
            IDictionary<FeatureValue, FeatureValue> copies,
            VariableBindings varBindings
        )
        {
            FeatureStruct otherFS;
            if (!Dereference(other, out otherFS))
                return false;

            if (this == otherFS)
                return true;

            if (preserveInput)
            {
                if (copies != null)
                    copies[otherFS] = this;
            }
            else
            {
                otherFS.Forward = this;
            }

            foreach (KeyValuePair<Feature, FeatureValue> featVal in otherFS._definite)
            {
                FeatureValue otherValue = Dereference(featVal.Value);
                FeatureValue thisValue;
                if (_definite.TryGetValue(featVal.Key, out thisValue))
                {
                    thisValue = Dereference(thisValue);
                    if (!thisValue.DestructiveUnify(otherValue, useDefaults, preserveInput, copies, varBindings))
                        return false;
                }
                else if (useDefaults && featVal.Key.DefaultValue != null)
                {
                    thisValue = featVal.Key.DefaultValue.CloneImpl(null);
                    _definite[featVal.Key] = thisValue;
                    if (!thisValue.DestructiveUnify(otherValue, true, preserveInput, copies, varBindings))
                        return false;
                }
                else
                {
                    _definite[featVal.Key] = preserveInput ? otherValue.CloneImpl(copies) : otherValue;
                }
            }

            return true;
        }

        protected override bool NondestructiveUnify(
            FeatureValue other,
            bool useDefaults,
            IDictionary<FeatureValue, FeatureValue> copies,
            VariableBindings varBindings,
            out FeatureValue output
        )
        {
            FeatureStruct otherFS;
            if (!Dereference(other, out otherFS))
            {
                output = null;
                return false;
            }

            var copy = new FeatureStruct();
            copies[this] = copy;
            copies[other] = copy;
            foreach (KeyValuePair<Feature, FeatureValue> featVal in otherFS._definite)
            {
                FeatureValue otherValue = Dereference(featVal.Value);
                FeatureValue thisValue;
                if (_definite.TryGetValue(featVal.Key, out thisValue))
                {
                    thisValue = Dereference(thisValue);
                    FeatureValue newValue;
                    if (!thisValue.UnifyImpl(otherValue, useDefaults, copies, varBindings, out newValue))
                    {
                        output = null;
                        return false;
                    }
                    copy.AddValue(featVal.Key, newValue);
                }
                else if (useDefaults && featVal.Key.DefaultValue != null)
                {
                    thisValue = featVal.Key.DefaultValue.CloneImpl(null);
                    FeatureValue newValue;
                    if (!thisValue.UnifyImpl(otherValue, true, copies, varBindings, out newValue))
                    {
                        output = null;
                        return false;
                    }
                    copy._definite[featVal.Key] = newValue;
                }
                else
                {
                    copy._definite[featVal.Key] = otherValue.CloneImpl(copies);
                }
            }

            foreach (KeyValuePair<Feature, FeatureValue> featVal in _definite)
            {
                if (!otherFS._definite.ContainsKey(featVal.Key))
                    copy._definite[featVal.Key] = Dereference(featVal.Value).CloneImpl(copies);
            }

            output = copy;
            return true;
        }

        internal override FeatureValue CloneImpl(IDictionary<FeatureValue, FeatureValue> copies)
        {
            if (copies != null)
            {
                FeatureValue clone;
                if (copies.TryGetValue(this, out clone))
                    return clone;
                return new FeatureStruct(this, copies);
            }

            return Clone();
        }

        internal override void FindReentrances(IDictionary<FeatureValue, bool> reentrances)
        {
            if (reentrances.ContainsKey(this))
            {
                reentrances[this] = true;
            }
            else
            {
                reentrances[this] = false;
                foreach (FeatureValue value in _definite.Values)
                {
                    FeatureValue v = Dereference(value);
                    v.FindReentrances(reentrances);
                }
            }
        }

        public new FeatureStruct Clone()
        {
            return new FeatureStruct(this);
        }

        internal override bool ValueEqualsImpl(
            FeatureValue other,
            ref ISet<FeatureValue> visitedSelf,
            ref ISet<FeatureValue> visitedOther,
            ref IDictionary<FeatureValue, FeatureValue> visitedPairs
        )
        {
            if (other == null)
                return false;

            FeatureStruct otherFS;
            if (!Dereference(other, out otherFS))
                return false;

            if (this == otherFS)
                return true;

            if (visitedSelf != null && (visitedSelf.Contains(this) || visitedOther.Contains(otherFS)))
            {
                FeatureValue fv;
                if (visitedPairs.TryGetValue(this, out fv))
                    return fv == otherFS;
                return false;
            }

            if (visitedSelf == null)
            {
                visitedSelf = new HashSet<FeatureValue>();
                visitedOther = new HashSet<FeatureValue>();
                visitedPairs = new Dictionary<FeatureValue, FeatureValue>();
            }
            visitedSelf.Add(this);
            visitedOther.Add(otherFS);
            visitedPairs[this] = otherFS;

            if (_definite.Count != otherFS._definite.Count)
                return false;

            foreach (KeyValuePair<Feature, FeatureValue> kvp in _definite)
            {
                FeatureValue thisValue = Dereference(kvp.Value);
                FeatureValue otherValue;
                if (!otherFS._definite.TryGetValue(kvp.Key, out otherValue))
                    return false;
                otherValue = Dereference(otherValue);
                if (!thisValue.ValueEqualsImpl(otherValue, ref visitedSelf, ref visitedOther, ref visitedPairs))
                    return false;
            }

            return true;
        }

        public bool ValueEquals(FeatureStruct other)
        {
            if (this == other)
                return true;

            if (other == null)
                return false;

            if (_hashCode.HasValue && other._hashCode.HasValue && _hashCode != other._hashCode)
                return false;

            // Both sides frozen and known, at freeze time, to be a flat tree (no nested FeatureStruct, no
            // FeatureValue instance repeated) means neither side's recursive walk could ever hit the
            // visited-set guard below -- see TreeValueEquals for the full argument -- so we can compare leaves
            // directly with zero allocation instead of paying for the pool.
            if (IsFrozen && other.IsFrozen && _isTree && other._isTree)
                return TreeValueEquals(other);

            return SlowValueEquals(other);
        }

        /// <summary>
        /// Compares two frozen, tree-shaped (<c>_isTree</c>) feature structures without allocating the
        /// visited-set collections the general recursive walk needs.
        ///
        /// Why this is exactly equivalent to the slow path for tree structures: the slow path
        /// (<see cref="ValueEqualsImpl"/>) only ever consults the visited sets to answer a *repeat* visit --
        /// either <c>this</c>/<c>otherFS</c> being reached a second time (reentrancy/cycles) or, for a leaf, the
        /// same <see cref="SimpleFeatureValue"/> instance being reached a second time (shared leaves). A struct
        /// flagged <c>_isTree</c> at freeze time was proven, at that moment, to contain no nested
        /// <see cref="FeatureStruct"/> and no repeated <see cref="FeatureValue"/> instance anywhere reachable
        /// from it (see <see cref="FreezeImpl"/>). Frozen structures are immutable, so that proof still holds
        /// here. Requiring <c>_isTree</c> on *both* sides means neither side's walk could ever reach a repeat,
        /// on either the self or the other side of the pairing, so the slow path's visited-set checks would
        /// never fire for this comparison -- it would simply walk every feature once, in the same order, doing
        /// exactly what this method does: compare feature counts, then each leaf value via
        /// <see cref="SimpleFeatureValue.ValueEquals(SimpleFeatureValue)"/> (the same method the slow path
        /// bottoms out at, via <see cref="SimpleFeatureValue.ValueEqualsImpl"/>).
        /// </summary>
        private bool TreeValueEquals(FeatureStruct other)
        {
            if (_definite.Count != other._definite.Count)
                return false;

            foreach (KeyValuePair<Feature, FeatureValue> kvp in _definite)
            {
                FeatureValue otherValue;
                if (!other._definite.TryGetValue(kvp.Key, out otherValue))
                    return false;

                // _isTree on both sides guarantees every value is a SimpleFeatureValue; the fallback is a
                // defensive measure only, never expected to trigger.
                if (
                    !(Dereference(kvp.Value) is SimpleFeatureValue thisSfv)
                    || !(Dereference(otherValue) is SimpleFeatureValue otherSfv)
                )
                {
                    return SlowValueEquals(other);
                }

                if (!thisSfv.ValueEquals(otherSfv))
                    return false;
            }

            return true;
        }

        private bool SlowValueEquals(FeatureStruct other)
        {
            VisitedSetsPool pool = GetPool();
            if (pool.InUse)
            {
                ISet<FeatureValue> fallbackSelf = null;
                ISet<FeatureValue> fallbackOther = null;
                IDictionary<FeatureValue, FeatureValue> fallbackPairs = null;
                return ValueEqualsImpl(other, ref fallbackSelf, ref fallbackOther, ref fallbackPairs);
            }

            pool.InUse = true;
            try
            {
                ISet<FeatureValue> visitedSelf = pool.Self;
                ISet<FeatureValue> visitedOther = pool.Other;
                IDictionary<FeatureValue, FeatureValue> visitedPairs = pool.Pairs;
                return ValueEqualsImpl(other, ref visitedSelf, ref visitedOther, ref visitedPairs);
            }
            finally
            {
                pool.Self.Clear();
                pool.Other.Clear();
                pool.Pairs.Clear();
                pool.InUse = false;
            }
        }

        /// <summary>
        /// Test-only: forces the general visited-set-guarded comparison, bypassing the frozen-tree fast path,
        /// so tests can cross-check the fast path's answer against the ground-truth slow path on the same
        /// inputs. Not used by production logic.
        /// </summary>
        internal bool SlowValueEqualsForTesting(FeatureStruct other)
        {
            if (this == other)
                return true;

            if (other == null)
                return false;

            if (_hashCode.HasValue && other._hashCode.HasValue && _hashCode != other._hashCode)
                return false;

            return SlowValueEquals(other);
        }

        /// <summary>
        /// Test-only: exposes whether this frozen structure was determined, at freeze time, to be a flat tree
        /// (see <see cref="FreezeImpl"/>). Not used by production logic.
        /// </summary>
        internal bool IsTreeForTesting => _isTree;

        public int GetFrozenHashCode()
        {
            if (!IsFrozen)
            {
                throw new InvalidOperationException(
                    "The feature structure does not have a valid hash code, because it is mutable."
                );
            }

            if (!_hashCode.HasValue)
                _hashCode = ComputeFrozenHashCode();
            return _hashCode.Value;
        }

        public override bool ValueEquals(FeatureValue other)
        {
            return other is FeatureStruct otherFS && ValueEquals(otherFS);
        }

        public bool IsFrozen { get; private set; }

        private void CheckFrozen()
        {
            if (IsFrozen)
                throw new InvalidOperationException("The feature structure is immutable.");
        }

        public void Freeze()
        {
            if (IsFrozen)
                return;

            _hashCode = ComputeFrozenHashCode();
        }

        private int ComputeFrozenHashCode()
        {
            VisitedSetsPool pool = GetPool();
            if (pool.InUse)
            {
                ISet<FeatureValue> fallbackVisited = null;
                return FreezeImpl(ref fallbackVisited);
            }

            pool.InUse = true;
            try
            {
                ISet<FeatureValue> visited = pool.FreezeVisited;
                return FreezeImpl(ref visited);
            }
            finally
            {
                pool.FreezeVisited.Clear();
                pool.InUse = false;
            }
        }

        internal override int FreezeImpl(ref ISet<FeatureValue> visited)
        {
            if (visited != null && visited.Contains(this))
                return 1;

            if (visited == null)
                visited = new HashSet<FeatureValue>();
            visited.Add(this);
            IsFrozen = true;

            // Conservatively determine, while we're already walking every reachable value anyway, whether this
            // structure is a flat tree: no nested FeatureStruct anywhere below it, and no FeatureValue instance
            // reachable from it is visited a second time (shared leaves, reentrancy, or cycles). A nested
            // FeatureStruct makes this struct non-tree even if that child is itself a tree -- we don't need to
            // know, since ValueEquals's fast path never needs to recurse into a nested struct at all. See
            // TreeValueEquals for how this flag is used.
            bool isTree = true;

            int code = 23;
            foreach (KeyValuePair<Feature, FeatureValue> kvp in _definite.OrderBy(kvp => kvp.Key.ID))
            {
                code = code * 31 + kvp.Key.GetHashCode();
                FeatureValue value = Dereference(kvp.Value);

                if (value is FeatureStruct childFS)
                {
                    isTree = false;
                    if (visited.Contains(childFS))
                    {
                        code = code * 31 + 1;
                        continue;
                    }
                }
                else if (visited.Contains(value))
                {
                    isTree = false;
                }

                code = code * 31 + value.FreezeImpl(ref visited);
            }

            _isTree = isTree;
            return code;
        }

        public override string ToString()
        {
            if (IsEmpty)
                return "ANY";

            var reentrances = new Dictionary<FeatureValue, bool>();
            FindReentrances(reentrances);
            var reentranceIds = new Dictionary<FeatureValue, int>();
            int id = 1;
            foreach (FeatureValue value in reentrances.Where(kvp => kvp.Value).Select(kvp => kvp.Key))
                reentranceIds[value] = id++;
            return ToStringImpl(new HashSet<FeatureValue>(), reentranceIds);
        }

        internal override string ToStringImpl(ISet<FeatureValue> visited, IDictionary<FeatureValue, int> reentranceIds)
        {
            if (visited.Contains(this))
                return string.Format("<{0}>", reentranceIds[this]);

            visited.Add(this);

            var sb = new StringBuilder();
            int id;
            if (reentranceIds.TryGetValue(this, out id))
            {
                sb.Append(id);
                sb.Append("=");
            }

            if (_definite.Count > 0)
            {
                bool firstFeature = true;
                if (_definite.Count > 0)
                    sb.Append("[");
                foreach (KeyValuePair<Feature, FeatureValue> kvp in _definite.OrderBy(kvp => kvp.Key.Description))
                {
                    FeatureValue value = Dereference(kvp.Value);
                    if (!firstFeature)
                        sb.Append(", ");
                    sb.Append(kvp.Key.Description);
                    sb.Append(":");
                    sb.Append(value.ToStringImpl(visited, reentranceIds));
                    firstFeature = false;
                }
                if (_definite.Count > 0)
                    sb.Append("]");
            }
            else
            {
                sb.Append("ANY");
            }

            return sb.ToString();
        }
    }
}
