using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SIL.Extensions;
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

        // Plain Dictionary rather than IDBearerDictionary: the latter kept a *second* parallel
        // Dictionary<string, FeatureValue> to serve string-ID lookups, doubling the dictionary
        // allocation on every unify-output / COW-inflation. String-ID lookups are rare (cold external
        // API) so they now scan _definite by Feature.ID instead (see TryGetValueById/ContainsKeyById).
        private Dictionary<Feature, FeatureValue> _definite;
        private int? _hashCode;

        /// <summary>
        /// On/off switch for the bit-packed flat-vector unify fast path. Default on; internal so a
        /// test can flip it to verify parity against the original unification engine. Not part of
        /// the public API.
        /// </summary>
        internal static bool FlatUnifyEnabled = true;

        // Bit-packed flat unify vector, computed lazily and cached (reset on mutation):
        //   _flatBits[feature.FlatIndex] = allowed-symbol bits (present) or ~0UL (absent = unconstrained).
        // _flatState: 0 = not computed, 1 = computed.
        // _flatComplete: every feature was bit-packable -> safe to use as the *constraint* (arc input).
        // _flatSafeSegment: every NON-packable feature is non-symbolic (string/complex), which a
        //   symbolic input can never constrain -> safe to use as the *segment* (extras are ignored).
        private ulong[] _flatBits;
        private byte _flatState;
        private bool _flatComplete;
        private bool _flatSafeSegment;

        // Copy-on-write: a clone of a FROZEN feature struct borrows the source's (immutable)
        // backing dictionary instead of deep-copying it. _shared is true until the first
        // mutation inflates a private copy; _sharedSource is the frozen FS we borrowed from
        // (needed to seed the re-entrancy map on inflate so the deep copy matches a normal clone).
        private bool _shared;
        private FeatureStruct _sharedSource;

        /// <summary>
        /// Initializes a new instance of the <see cref="FeatureStruct"/> class.
        /// </summary>
        public FeatureStruct()
        {
            _definite = new Dictionary<Feature, FeatureValue>();
        }

        protected FeatureStruct(FeatureStruct other)
            : this(other, new Dictionary<FeatureValue, FeatureValue>()) { }

        /// <summary>
        /// Copy constructor.
        /// </summary>
        /// <param name="other">The fs.</param>
        /// <param name="copies"></param>
        private FeatureStruct(FeatureStruct other, IDictionary<FeatureValue, FeatureValue> copies)
            : this()
        {
            copies[other] = this;
            foreach (KeyValuePair<Feature, FeatureValue> featVal in other._definite)
                _definite[featVal.Key] = Dereference(featVal.Value).CloneImpl(copies);
        }

        // Copy-on-write clone of a frozen source: share its immutable backing; inflate on write.
        private FeatureStruct(FeatureStruct frozenSource, bool sharedClone)
        {
            _definite = frozenSource._definite;
            _shared = sharedClone;
            _sharedSource = frozenSource;
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

            EnsureWritable();
            _definite[feature] = value;
        }

        public void AddValue(IEnumerable<Feature> path, FeatureValue value)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            if (value == null)
                throw new ArgumentNullException("value");

            EnsureWritable();
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

            EnsureWritable();
            _definite.Remove(feature);
        }

        public void RemoveValue(IEnumerable<Feature> path)
        {
            if (path == null)
                throw new ArgumentNullException("path");

            EnsureWritable();
            Feature lastFeature;
            FeatureStruct lastFS;
            if (FollowPath(path, out lastFeature, out lastFS))
                lastFS._definite.Remove(lastFeature);

            throw new ArgumentException("The feature path is invalid.", "path");
        }

        public void ReplaceVariables(VariableBindings varBindings)
        {
            EnsureWritable();
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
            EnsureWritable();
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

            EnsureWritable();
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

            EnsureWritable();
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

            EnsureWritable();
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

            EnsureWritable();
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
            EnsureWritable();
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
            if (TryGetValueById(_definite, featureID, out val))
                return Dereference(val, out value);
            value = null;
            return false;
        }

        // String-ID lookups over the plain _definite dictionary (replaces the dropped parallel
        // string-keyed dictionary). Feature IDs are unique within a struct, so first match wins.
        private static bool TryGetValueById(
            Dictionary<Feature, FeatureValue> definite,
            string id,
            out FeatureValue value
        )
        {
            foreach (KeyValuePair<Feature, FeatureValue> kvp in definite)
            {
                if (kvp.Key.ID == id)
                {
                    value = kvp.Value;
                    return true;
                }
            }
            value = null;
            return false;
        }

        private static bool ContainsKeyById(Dictionary<Feature, FeatureValue> definite, string id)
        {
            foreach (KeyValuePair<Feature, FeatureValue> kvp in definite)
            {
                if (kvp.Key.ID == id)
                    return true;
            }
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
                if (TryGetValueById(lastFS._definite, lastID, out val))
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

            return ContainsKeyById(_definite, featureID);
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
                return ContainsKeyById(lastFS._definite, lastID);
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
                    if (!TryGetValueById(lastFS._definite, lastID, out curValue) || !Dereference(curValue, out lastFS))
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

        // Builds (once, on a frozen struct) the bit-packed flat unify vector. _flatState becomes
        // 1 (Simple: vector valid) only if every feature is a flat-indexed symbolic feature with a
        // non-empty ulong value and no variable; otherwise 2 (Complex: must use the slow path).
        private void EnsureFlat()
        {
            if (_flatState != 0)
                return;
            int maxIdx = -1;
            bool complete = true; // all features bit-packable (usable as a constraint/input)
            bool safeSegment = true; // every non-packable feature is non-symbolic (ignorable in a segment)
            foreach (KeyValuePair<Feature, FeatureValue> featVal in _definite)
            {
                if (
                    featVal.Key is SymbolicFeature sf
                    && sf.FlatIndex >= 0
                    && Dereference(featVal.Value) is SymbolicFeatureValue sv
                    && sv.TryGetFlatBits(out _)
                )
                {
                    if (sf.FlatIndex > maxIdx)
                        maxIdx = sf.FlatIndex;
                }
                else
                {
                    complete = false;
                    // A symbolic-but-unpackable feature (variable/empty/>64 symbols) CAN be
                    // constrained by a symbolic input, so it can't be safely ignored in a segment.
                    if (featVal.Key is SymbolicFeature)
                        safeSegment = false;
                }
            }
            var arr = new ulong[maxIdx + 1];
            for (int i = 0; i <= maxIdx; i++)
                arr[i] = ulong.MaxValue; // absent feature = unconstrained
            foreach (KeyValuePair<Feature, FeatureValue> featVal in _definite)
            {
                if (
                    featVal.Key is SymbolicFeature sf
                    && sf.FlatIndex >= 0
                    && Dereference(featVal.Value) is SymbolicFeatureValue sv
                    && sv.TryGetFlatBits(out ulong bits)
                )
                {
                    arr[sf.FlatIndex] = bits;
                }
            }
            _flatBits = arr;
            _flatComplete = complete;
            _flatSafeSegment = safeSegment;
            _flatState = 1;
        }

        // Bit-packed unifiability fast path. Returns false (not handled) when either struct isn't a
        // frozen, Simple symbolic struct; otherwise sets result and returns true. Provably identical
        // to IsUnifiable(other, useDefaults:false, varBindings:null) for the simple/no-variable case:
        // a feature absent on either side is ~0 (the "no constraint" branch), and overlap == unifiable.
        // this = the segment being matched; other = the arc-input constraint.
        internal bool TryFastUnifiable(FeatureStruct other, out bool result)
        {
            result = false;
            if (!FlatUnifyEnabled)
                return false;
            EnsureFlat();
            other.EnsureFlat();
            // The constraint (input) must be fully bit-packed; the segment may carry extra
            // non-symbolic features the symbolic input can't constrain (so they're ignorable).
            if (!other._flatComplete || !_flatSafeSegment)
                return false;
            ulong[] a = _flatBits;
            ulong[] b = other._flatBits;
            int n = a.Length > b.Length ? a.Length : b.Length;
            for (int i = 0; i < n; i++)
            {
                ulong av = i < a.Length ? a[i] : ulong.MaxValue;
                ulong bv = i < b.Length ? b[i] : ulong.MaxValue;
                if ((av & bv) == 0)
                    return true; // result already false: a feature has no common symbol
            }
            result = true;
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
            // A clone of a frozen FS borrows its immutable backing (copy-on-write); a clone of an
            // unfrozen FS must be an independent deep copy, since the caller may mutate both.
            if (IsFrozen)
                return new FeatureStruct(this, sharedClone: true);
            return new FeatureStruct(this);
        }

        internal override bool ValueEqualsImpl(
            FeatureValue other,
            ISet<FeatureValue> visitedSelf,
            ISet<FeatureValue> visitedOther,
            IDictionary<FeatureValue, FeatureValue> visitedPairs
        )
        {
            if (other == null)
                return false;

            FeatureStruct otherFS;
            if (!Dereference(other, out otherFS))
                return false;

            if (this == otherFS)
                return true;

            if (visitedSelf.Contains(this) || visitedOther.Contains(otherFS))
            {
                FeatureValue fv;
                if (visitedPairs.TryGetValue(this, out fv))
                    return fv == otherFS;
                return false;
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
                if (!thisValue.ValueEqualsImpl(otherValue, visitedSelf, visitedOther, visitedPairs))
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

            return ValueEqualsImpl(
                other,
                new HashSet<FeatureValue>(),
                new HashSet<FeatureValue>(),
                new Dictionary<FeatureValue, FeatureValue>()
            );
        }

        public int GetFrozenHashCode()
        {
            if (!IsFrozen)
            {
                throw new InvalidOperationException(
                    "The feature structure does not have a valid hash code, because it is mutable."
                );
            }

            if (!_hashCode.HasValue)
                _hashCode = FreezeImpl(new HashSet<FeatureValue>());
            return _hashCode.Value;
        }

        public override bool ValueEquals(FeatureValue other)
        {
            return other is FeatureStruct otherFS && ValueEquals(otherFS);
        }

        public bool IsFrozen { get; private set; }

        // Guards every mutation. Frozen structs stay immutable (throw). A copy-on-write shell
        // that is still borrowing a frozen backing inflates a private deep copy first, so neither
        // this struct's mutation nor any recursion into its children can touch shared frozen data.
        private void EnsureWritable()
        {
            if (IsFrozen)
                throw new InvalidOperationException("The feature structure is immutable.");
            // Any mutation invalidates the cached flat unify vector.
            _flatState = 0;
            _flatBits = null;
            if (!_shared)
                return;
            var copies = new Dictionary<FeatureValue, FeatureValue> { [_sharedSource] = this };
            var owned = new Dictionary<Feature, FeatureValue>();
            foreach (KeyValuePair<Feature, FeatureValue> featVal in _definite)
                owned[featVal.Key] = Dereference(featVal.Value).CloneImpl(copies);
            _definite = owned;
            _shared = false;
            _sharedSource = null;
        }

        public void Freeze()
        {
            if (IsFrozen)
                return;

            _hashCode = FreezeImpl(new HashSet<FeatureValue>());
        }

        internal override int FreezeImpl(ISet<FeatureValue> visited)
        {
            if (visited.Contains(this))
                return 1;

            visited.Add(this);
            IsFrozen = true;

            int code = 23;
            foreach (KeyValuePair<Feature, FeatureValue> kvp in _definite.OrderBy(kvp => kvp.Key.ID))
            {
                code = code * 31 + kvp.Key.GetHashCode();
                FeatureValue value = Dereference(kvp.Value);
                code = code * 31 + value.FreezeImpl(visited);
            }

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
