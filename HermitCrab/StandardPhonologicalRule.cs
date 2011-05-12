using System;
using System.Collections.Generic;
using System.Linq;
using SIL.APRE;

namespace SIL.HermitCrab
{
    /// <summary>
    /// This class represents a standard rewrite phonological rule as defined in classical generative phonology
    /// theory. It consists of a LHS, a RHS, a left environment, and a right environment. It also supports
    /// disjunctive subrules.
    /// </summary>
    public class StandardPhonologicalRule : PhonologicalRule
    {
        /// <summary>
        /// This class represents a phonological subrule. A subrule consists of the RHS,
        /// the left environment, and the right environment.
        /// </summary>
        public class Subrule
        {
            /// <summary>
            /// Change type
            /// </summary>
            public enum ChangeType { Feature, Epenthesis, Widen, Narrow, Unknown };

            private readonly Environment _env;
            private readonly Pattern<PhoneticShapeNode> _rhs;
            private readonly Pattern<PhoneticShapeNode> _analysisTarget;
            private readonly StandardPhonologicalRule _rule;

            private IDBearerSet<PartOfSpeech> _requiredPartsOfSpeech;
            private MprFeatureSet _excludedMprFeatures;
            private MprFeatureSet _requiredMprFeatures;

        	private readonly SpanFactory<PhoneticShapeNode> _spanFactory;
        	private readonly FeatureSystem _phoneticFeatSys;
        	private readonly int _delReapplications;

        	/// <summary>
        	/// Initializes a new instance of the <see cref="Subrule"/> class.
        	/// </summary>
        	/// <param name="spanFactory"></param>
        	/// <param name="phoneticFeatSys"></param>
        	/// <param name="delReapplications"></param>
        	/// <param name="rhs">The RHS.</param>
        	/// <param name="env">The environment.</param>
        	/// <param name="rule">The phonological rule.</param>
        	/// <exception cref="System.ArgumentException">Thrown when the size of the RHS is greater than the
        	/// size of the specified rule's LHS and the LHS's size is greater than 1. A standard phonological
        	/// rule does not currently support this type of widening.</exception>
        	public Subrule(SpanFactory<PhoneticShapeNode> spanFactory, FeatureSystem phoneticFeatSys,
				int delReapplications, Pattern<PhoneticShapeNode> rhs, Environment env, StandardPhonologicalRule rule)
            {
            	_spanFactory = spanFactory;
            	_phoneticFeatSys = phoneticFeatSys;
                _rhs = rhs;
                _env = env;
                _rule = rule;
        		_delReapplications = delReapplications;

                switch (Type)
                {
                    case ChangeType.Narrow:
                    case ChangeType.Epenthesis:
                        // analysis target is a copy of the RHS, because there is no LHS
                        _analysisTarget = _rhs.Clone();
                        break;

                    case ChangeType.Widen:
                        // before generating the analysis we must extend the length of the LHS
                        // to match the length of the RHS
                        Pattern<PhoneticShapeNode> lhs = _rule._lhs.Clone();
                        while (lhs.Count != _rhs.Count)
                            lhs.Add(lhs.First.Clone());
                		_analysisTarget = CreateAnalysisTarget(_rhs, lhs);
                        break;

                    case ChangeType.Feature:
                        _analysisTarget = CreateAnalysisTarget(_rhs, _rule._lhs);
                        break;

                    case ChangeType.Unknown:
						throw new ArgumentException(HCStrings.kstidInvalidSubruleType, "rhs");
                }
            }

			private Pattern<PhoneticShapeNode> CreateAnalysisTarget(Pattern<PhoneticShapeNode> rhs, Pattern<PhoneticShapeNode> lhs)
			{
				var analysisTarget = new Pattern<PhoneticShapeNode>(_spanFactory, new [] {"Segment", "Boundary"}, new [] {"Segment"});
				IEnumerator<PatternNode<PhoneticShapeNode>> rhsEnum = rhs.GetEnumerator();
				IEnumerator<PatternNode<PhoneticShapeNode>> lhsEnum = lhs.GetEnumerator();
				while (rhsEnum.MoveNext() && lhsEnum.MoveNext())
				{
					var rhsConstraints = rhsEnum.Current as AnnotationConstraints<PhoneticShapeNode>;
					var lhsConstraints = lhsEnum.Current as AnnotationConstraints<PhoneticShapeNode>;
					if (rhsConstraints != null && lhsConstraints != null)
					{
						var result = (AnnotationConstraints<PhoneticShapeNode>) lhsConstraints.Clone();
						result.FeatureStructure.Instantiate(rhsConstraints.FeatureStructure);
						if (rhsConstraints.Variables != null)
						{
							foreach (KeyValuePair<string, bool> varPolarity in rhsConstraints.Variables)
								result.Variables[varPolarity.Key] = varPolarity.Value;
						}
						analysisTarget.Add(result);
					}
				}
				return analysisTarget;
			}

            /// <summary>
            /// Gets the rule.
            /// </summary>
            /// <value>The rule.</value>
            public StandardPhonologicalRule Rule
            {
                get
                {
                    return _rule;
                }
            }

            /// <summary>
            /// Gets or sets the required parts of speech.
            /// </summary>
            /// <value>The required parts of speech.</value>
            public IEnumerable<PartOfSpeech> RequiredPartsOfSpeech
            {
                get
                {
                    return _requiredPartsOfSpeech;
                }

                set
                {
                    _requiredPartsOfSpeech = new IDBearerSet<PartOfSpeech>(value);
                }
            }

            /// <summary>
            /// Gets or sets the excluded MPR features.
            /// </summary>
            /// <value>The excluded MPR features.</value>
            public MprFeatureSet ExcludedMprFeatures
            {
                get
                {
                    return _excludedMprFeatures;
                }

                set
                {
                    _excludedMprFeatures = value;
                }
            }

            /// <summary>
            /// Gets or sets the required MPR features.
            /// </summary>
            /// <value>The required MPR features.</value>
            public MprFeatureSet RequiredMprFeatures
            {
                get
                {
                    return _requiredMprFeatures;
                }

                set
                {
                    _requiredMprFeatures = value;
                }
            }

            /// <summary>
            /// Gets the change type.
            /// </summary>
            /// <value>The change type.</value>
            ChangeType Type
            {
                get
                {
                    if (_rule._lhs.Count == _rhs.Count)
                        return ChangeType.Feature;
                    else if (_rule._lhs.Count == 0)
                        return ChangeType.Epenthesis;
                    else if (_rule._lhs.Count == 1 && _rhs.Count > 1)
                        return ChangeType.Widen;
                    else if (_rule._lhs.Count > _rhs.Count)
                        return ChangeType.Narrow;
                    else
                        return ChangeType.Unknown;
                }
            }

            /// <summary>
            /// Gets a value indicating whether this subrule is self-opaquing.
            /// Self-opaquing basically means that an application of the subrule can
            /// alter the environment for succeeding applications of the subrule. This is only important
            /// for simultaneously applying subrules. During analysis, it might be necessary
            /// to unapply a subrule multiple times if it is self-opaquing, because a segment might have
            /// been altered during the first pass so that it now matches the subrule's environment. If
            /// we do not unapply the rule multiple times, we would miss segments that should be been
            /// unapplied. Note: although iteratively applying rules can be self-opaquing, we do not
            /// return <c>true</c>, unless it is a deletion rule, because we don't care.
            /// </summary>
            /// <value>
            /// 	<c>true</c> if this subrule is self-opaquing, otherwise <c>false</c>.
            /// </value>
            bool IsSelfOpaquing
            {
                get
                {
                    if (Type == ChangeType.Narrow)
                    {
                        // deletion subrules are always self-opaquing
                        return true;
                    }
                    else if (_rule._multApplication == MultAppOrder.Simultaneous)
                    {
                        if (Type == ChangeType.Feature)
                        {
                            foreach (PatternNode<PhoneticShapeNode> node in _rhs)
                            {
                            	var constraints = node as AnnotationConstraints<PhoneticShapeNode>;
								if (constraints != null && constraints.AnnotationType == "Segment")
								{
									// check if there is any overlap of features between
									// the context and the environments
									if (!IsNonSelfOpaquing(constraints, _env.LeftEnvironment))
										return true;
									if (!IsNonSelfOpaquing(constraints, _env.RightEnvironment))
										return true;
								}
                            }

                            return false;
                        }
                        else
                        {
                            return true;
                        }
                    }
                    else
                    {
                        // all other types of iteratively applying subrules are never self-opaquing
                        // ok, so they might be self-opaquing, but we don't care
                        return false;
                    }
                }
            }

            /// <summary>
            /// Checks for overlap of features between the specified simple context and the specified
            /// environment.
            /// </summary>
            /// <param name="constraints">The simple context.</param>
            /// <param name="env">The environment.</param>
            /// <returns>
            /// 	<c>true</c> if there is no overlap, otherwise <c>false</c>.
            /// </returns>
            private static bool IsNonSelfOpaquing(AnnotationConstraints<PhoneticShapeNode> constraints, IEnumerable<PatternNode<PhoneticShapeNode>> env)
            {
                foreach (PatternNode<PhoneticShapeNode> node in env)
                {
                    switch (node.Type)
                    {
                        case PatternNode<PhoneticShapeNode>.NodeType.Constraints:
                    		var curConstraints = (AnnotationConstraints<PhoneticShapeNode>) node;
							if (curConstraints.AnnotationType == "Segment")
							{
								if (curConstraints.FeatureStructure.IsUnifiable(constraints.FeatureStructure))
									return false;
							}
                    		break;

						case PatternNode<PhoneticShapeNode>.NodeType.Group:
                            var nestedPattern = (CapturingGroup<PhoneticShapeNode>) node;
                            if (!IsNonSelfOpaquing(constraints, nestedPattern.Nodes))
                                return false;
                            break;
                    }
                }

                return true;
            }

            /// <summary>
            /// Unapplies this subrule to specified input phonetic shape.
            /// </summary>
            /// <param name="shape">The input phonetic shape.</param>
            public void Unapply(PhoneticShape shape)
            {
                if (Type == ChangeType.Narrow)
                {
                    int i = 0;
                    // because deletion rules are self-opaquing it is unclear how many segments
                    // could have been deleted during synthesis, so we unapply deletion rules
                    // multiple times. Unfortunately, this could create a situation where the
                    // deletion rule is unapplied infinitely, so we put an upper limit on the
                    // number of times a deletion rule can unapply.
                    while (i <= _delReapplications && UnapplyNarrow(shape))
                        i++;
                }
                else
                {
                    Direction dir = Direction.RightToLeft;
                    switch (_rule._multApplication)
                    {
                        case MultAppOrder.LeftToRightIterative:
                        case MultAppOrder.Simultaneous:
                            // simultaneous subrules could be unapplied left-to-right or
                            // right-to-left, we arbitrarily choose left-to-right
                            dir = Direction.RightToLeft;
                            break;

                        case MultAppOrder.RightToLeftIterative:
                            dir = Direction.LeftToRight;
                            break;
                    }

                    // only simultaneous subrules can be self-opaquing
                    if (IsSelfOpaquing)
                        // unapply the subrule until it no longer makes a change
                        while (UnapplyIterative(shape, dir)) { }
                    else
                        UnapplyIterative(shape, dir);
                }
            }

            bool UnapplyIterative(PhoneticShape shape, Direction dir)
            {
                bool unapplied = false;
                PhoneticShapeNode node = shape.GetFirst(dir);
                PatternMatch<PhoneticShapeNode> match;
                // iterate thru all matches
                while (FindNextMatchRhs(shape, node, dir, out match))
                {
                    // unapply the subrule
                    Span<PhoneticShapeNode> span = match.EntireMatch;
                    UnapplyRhs(dir, span, match.VariableValues);
                    unapplied = true;
                    node = span.GetEnd(dir).GetNext(dir);
                }

                return unapplied;
            }

            private bool UnapplyNarrow(PhoneticShape shape)
            {
                var matches = new List<PatternMatch<PhoneticShapeNode>>();
                PhoneticShapeNode node = shape.First;
                PatternMatch<PhoneticShapeNode> match;
                // deletion subrules are always treated like simultaneous subrules during unapplication
                while (FindNextMatchRhs(shape, node, Direction.LeftToRight, out match))
                {
                    matches.Add(match);
                    node = match.EntireMatch.Start.Next;
                }

                foreach (PatternMatch<PhoneticShapeNode> m in matches)
                {
                    PhoneticShapeNode cur = m.EntireMatch.End;
                    foreach (PatternNode<PhoneticShapeNode> lhsNode in _rule._lhs)
                    {
                    	var constraints = lhsNode as AnnotationConstraints<PhoneticShapeNode>;
                        if (constraints == null)
                            continue;

                        var newNode = new PhoneticShapeNode(_spanFactory, constraints.AnnotationType, _phoneticFeatSys.CreateFeatureStructure());
						newNode.Annotation.FeatureStructure.UninstantiateAll();
						newNode.Annotation.FeatureStructure.Instantiate(constraints.FeatureStructure);
						//constraints.UnapplyDeletion(newNode.Annotation, m.VariableValues);
                        // mark the undeleted segment as optional
                        newNode.Annotation.IsOptional = true;
                        cur.Insert(newNode, Direction.LeftToRight);
                        cur = newNode;
                    }

                    if (_analysisTarget.Count > 0)
                    {
                        foreach (PhoneticShapeNode matchNode in m.EntireMatch.Start.GetNodes(m.EntireMatch.End))
                            matchNode.Annotation.IsOptional = true;
                    }
                }

                return matches.Count > 0;
            }

        	/// <summary>
        	/// Applies the RHS to the matched segments.
        	/// </summary>
        	/// <param name="shape">The phonetic shape.</param>
        	/// <param name="match">The matched segments.</param>
			/// <param name="dir">The direction.</param>
        	/// <param name="varValues">The instantiated variables.</param>
        	public void ApplyRhs(PhoneticShape shape, Span<PhoneticShapeNode> match, Direction dir, FeatureStructure varValues)
            {
                switch (Type)
                {
                    case ChangeType.Feature:
                		PhoneticShapeNode shapeNode = match.GetStart(dir);
                        foreach (PatternNode<PhoneticShapeNode> patNode in _rhs.GetNodes(dir))
                        {
                        	var constraints = patNode as AnnotationConstraints<PhoneticShapeNode>;
							if (constraints == null)
								continue;

							switch (constraints.AnnotationType)
							{
								case "Segment":
									while (shapeNode.Annotation.Type == "Boundary")
										shapeNode = shapeNode.GetNext(dir);
									// match[i] should be a segment, should I check that here?
									shapeNode.Annotation.FeatureStructure.Instantiate(constraints.FeatureStructure);
									constraints.InstantiateVariables(shapeNode.Annotation, varValues);
									// marked the segment as altered
									shapeNode.Annotation.IsClean = false;
									break;

								case "Boundary":
									// boundaries should match, should I check that here?
									break;
							}
                        	shapeNode = shapeNode.GetNext(dir);
                        }
                        break;

                    case ChangeType.Narrow:
                        ApplyInsertion(match, dir, varValues);
                        // remove matching segments
                		shape.GetView(match.Start, match.End).Clear();
                        break;

                    case ChangeType.Epenthesis:
                        // insert new segments or boundaries
                        ApplyInsertion(match, dir, varValues);
                        break;
                }
            }

            private void ApplyInsertion(Span<PhoneticShapeNode> match, Direction dir, FeatureStructure varValues)
            {
            	PhoneticShapeNode cur = match.GetEnd(dir);
                foreach (PatternNode<PhoneticShapeNode> patNode in _rhs.GetNodes(dir))
                {
                	var constraints = patNode as AnnotationConstraints<PhoneticShapeNode>;
					if (constraints == null)
						continue;
					var newNode = new PhoneticShapeNode(_spanFactory, constraints.AnnotationType, _phoneticFeatSys.CreateFeatureStructure());
					newNode.Annotation.FeatureStructure.Instantiate(constraints.FeatureStructure);
                	constraints.InstantiateVariables(newNode.Annotation, varValues);
            		try
            		{
            			cur.Insert(newNode, dir);
            		}
            		catch (InvalidOperationException ioe)
            		{
            			var me = new MorphException(MorphException.MorphErrorType.TooManySegs,
            			                            string.Format(HCStrings.kstidTooManySegs, _rule.ID), ioe);
            			me.Data["rule"] = _rule.ID;
            			throw me;
            		}
            		cur = newNode;
                }
            }

            private void UnapplyRhs(Direction dir, Span<PhoneticShapeNode> match, FeatureStructure varValues)
            {
                switch (Type)
                {
                    case ChangeType.Feature:
                		PhoneticShapeNode shapeNode = match.GetStart(dir);
						IEnumerator<PatternNode<PhoneticShapeNode>> rhsEnum = _rhs.GetNodes(dir).GetEnumerator();
						IEnumerator<PatternNode<PhoneticShapeNode>> lhsEnum = _rule._lhs.GetNodes(dir).GetEnumerator();
						while (rhsEnum.MoveNext() && lhsEnum.MoveNext())
						{
							var rhsConstraints = rhsEnum.Current as AnnotationConstraints<PhoneticShapeNode>;
							var lhsConstraints = lhsEnum.Current as AnnotationConstraints<PhoneticShapeNode>;
							if (rhsConstraints != null && lhsConstraints != null)
							{
								switch (rhsConstraints.AnnotationType)
								{
									case "Segment":
										// match[i] should be a segment, should I check that here?
										shapeNode.Annotation.FeatureStructure.Uninstantiate(rhsConstraints.FeatureStructure);
										shapeNode.Annotation.FeatureStructure.Instantiate(lhsConstraints.FeatureStructure);
										rhsConstraints.UninstantiateVariables(shapeNode.Annotation, varValues);
										lhsConstraints.InstantiateVariables(shapeNode.Annotation, varValues);
										break;

									case "Boundary":
										// skip boundaries
										continue;
								}
								shapeNode = shapeNode.GetNext(dir);
							}
						}
                        break;

                    case ChangeType.Epenthesis:
                        // do not remove epenthesized segments, since it is possible that they will not
                        // be epenthesized during synthesis, we just mark them as optional
                        foreach (PhoneticShapeNode node in match.Start.GetNodes(match.End))
                            node.Annotation.IsOptional = true;
                        break;

                }
            }

            private bool FindNextMatchRhs(PhoneticShape shape, PhoneticShapeNode startNode, Direction dir, out PatternMatch<PhoneticShapeNode> match)
            {
                foreach (PhoneticShapeNode node in startNode.GetNodes(dir).Where(node => node.Annotation.Type == "Segment"))
                {
                    if (_analysisTarget.Count == 0)
                    {
                        // if the analysis target is empty (deletion rule),
                        // just check environment
                    	FeatureStructure varValues = _phoneticFeatSys.CreateFeatureStructure();
                    	Span<PhoneticShapeNode> span = _spanFactory.Create(node);
                        if (MatchEnvEmpty(shape, span, dir, ModeType.Analysis, varValues))
                        {
                            match = new PatternMatch<PhoneticShapeNode>(span, varValues);
                            return true;
                        }
                    }
                    else
                    {
                        // analysis target is non-empty, check everything
                        if (MatchAnalysisTarget(shape, node, dir, out match))
                            return true;
                    }
                }

                match = null;
                return false;
            }

            private bool MatchAnalysisTarget(PhoneticShape shape, PhoneticShapeNode node, Direction dir, out PatternMatch<PhoneticShapeNode> match)
            {
                // check analysis target
            	FeatureStructure varValues = _phoneticFeatSys.CreateFeatureStructure();
                if (!_analysisTarget.IsMatch(shape.Annotations.GetView(node.Annotation, dir), dir, ModeType.Analysis, varValues, out match))
                {
                    match = null;
                    return false;
                }

                // check vacuous unapplication
                // even if the subrule matches, we do not want to successfully unapply if no real changes are
                // going to be made to the phonetic shape
                if (!CheckVacuousUnapplication(match, dir))
                {
                    match = null;
                    return false;
                }

                // finally, check environment
                if (!MatchEnvNonempty(shape, match.EntireMatch, dir, ModeType.Analysis, match.VariableValues))
                {
                    match = null;
                    return false;
                }

                return true;
            }

            public bool MatchEnvNonempty(PhoneticShape shape, Span<PhoneticShapeNode> match, Direction dir, ModeType mode,
                FeatureStructure varValues)
            {
                PhoneticShapeNode leftAtom = null;
                PhoneticShapeNode rightAtom = null;
                switch (dir)
                {
                    case Direction.RightToLeft:
                        rightAtom = match.Start.GetNext(Direction.LeftToRight);
                        leftAtom = match.End.GetNext(Direction.RightToLeft);
                        break;

                    case Direction.LeftToRight:
                        rightAtom = match.End.GetNext(Direction.LeftToRight);
                        leftAtom = match.Start.GetNext(Direction.RightToLeft);
                        break;
                }

                if (!_env.IsMatch(shape, leftAtom, rightAtom, mode, varValues))
                    return false;

                return true;
            }

            public bool MatchEnvEmpty(PhoneticShape shape, Span<PhoneticShapeNode> match, Direction dir, ModeType mode,
                FeatureStructure varValues)
            {
                PhoneticShapeNode leftNode;
                PhoneticShapeNode rightNode;
				if (dir == Direction.LeftToRight)
				{
					rightNode = match.Start.GetNext(Direction.LeftToRight);
					leftNode = match.Start;
				}
				else
				{
					rightNode = match.Start;
					leftNode = match.Start.GetNext(Direction.RightToLeft);
				}

                // in case this is an epenthesis rule, we want to ensure that the segment to the right
                // of where we're going to do the epenthesis is not a boundary marker, unless the
                // environment calls for one.
                if (mode == ModeType.Synthesis && _env.RightEnvironment != null && _env.RightEnvironment.Count > 0)
                {
                	var constraints = _env.RightEnvironment.First as AnnotationConstraints<PhoneticShapeNode>;
                    if (rightNode.Annotation.Type == "Boundary" && (constraints != null && constraints.AnnotationType != "Boundary"))
                        return false;
                }

                // there is a small difference between legacy HC and HC.NET in matching environments when the
                // analysis target is empty and one of the environments is empty. In this case, legacy HC does
                // not try to skip the initial optional segments when matching the right environment. I think
                // this will cause HC.NET to overproduce a little more during analysis, which isn't that big of a
                // deal
                if (!_env.IsMatch(shape, leftNode, rightNode, mode, varValues))
                    return false;

#if WANTPORT
                // remove ambiguous variables
                varValues.RemoveAmbiguousVariables();
#endif
                return true;
            }

            /// <summary>
            /// Checks if the subrule will be unapplied vacuously. Vacuous unapplication means that
            /// the subrule will actually make changes to the phonetic shape. This is important to know
            /// for self-opaquing, simultaneously applying subrules, since we unapply these subrules
            /// until they unapply nonvacuously.
            /// </summary>
            /// <param name="match">The match.</param>
            /// <param name="dir">The direction.</param>
            /// <returns></returns>
            private bool CheckVacuousUnapplication(PatternMatch<PhoneticShapeNode> match, Direction dir)
            {
                PatternNode<PhoneticShapeNode> rhsNode = _rhs.GetFirst(dir);
                Span<PhoneticShapeNode> span = match.EntireMatch;
            	PhoneticShapeNode shapeNode = span.GetStart(dir);
				while (shapeNode != span.GetEnd(dir).GetNext(dir))
				{
					if (Type == ChangeType.Epenthesis)
					{
						// for epenthesis subrules, simply check if the epenthesized segment is
						// already marked as optional
						if (!shapeNode.Annotation.IsOptional)
							return true;
						shapeNode = shapeNode.GetNext(dir);
					}
					else
					{
						var constraints = rhsNode as AnnotationConstraints<PhoneticShapeNode>;
						if (constraints != null && constraints.AnnotationType == "Segment")
						{
							if (shapeNode.Annotation.FeatureStructure.IsUnifiable(constraints.FeatureStructure))
								return true;
							shapeNode = shapeNode.GetNext(dir);
						}
						rhsNode = rhsNode.GetNext(dir);
					}
				}

                return false;
            }

            /// <summary>
            /// Determines whether this subrule is applicable to the specified word analysis.
            /// </summary>
            /// <param name="input">The word analysis.</param>
            /// <returns>
            /// 	<c>true</c> if this subrule is applicable, otherwise <c>false</c>.
            /// </returns>
            public bool IsApplicable(WordSynthesis input)
            {
                // check part of speech and MPR features
                return ((_requiredPartsOfSpeech == null || _requiredPartsOfSpeech.Count == 0 || _requiredPartsOfSpeech.Contains(input.PartOfSpeech))
                    && (_requiredMprFeatures == null || _requiredMprFeatures.Count == 0 || _requiredMprFeatures.IsMatch(input.MPRFeatures))
                    && (_excludedMprFeatures == null || _excludedMprFeatures.Count == 0 || !_excludedMprFeatures.IsMatch(input.MPRFeatures)));
            }
        }

        private readonly List<Subrule> _subrules;

        private MultAppOrder _multApplication = MultAppOrder.LeftToRightIterative;
        private AlphaVariables<PhoneticShapeNode> _alphaVars;
        private Pattern<PhoneticShapeNode> _lhs;
    	private readonly SpanFactory<PhoneticShapeNode> _spanFactory;
    	private readonly FeatureSystem _phoneticFeatSys;

    	/// <summary>
    	/// Initializes a new instance of the <see cref="PhonologicalRule"/> class.
    	/// </summary>
    	/// <param name="id">The ID.</param>
    	/// <param name="desc">The description.</param>
    	/// <param name="spanFactory"></param>
    	/// <param name="phoneticFeatSys"></param>
    	public StandardPhonologicalRule(string id, string desc, SpanFactory<PhoneticShapeNode> spanFactory, FeatureSystem phoneticFeatSys)
            : base(id, desc)
        {
        	_spanFactory = spanFactory;
    		_phoneticFeatSys = phoneticFeatSys;
            _subrules = new List<Subrule>();
        }

        /// <summary>
        /// Gets or sets the LHS.
        /// </summary>
        /// <value>The LHS.</value>
        public Pattern<PhoneticShapeNode> Lhs
        {
            get
            {
                return _lhs;
            }

            set
            {
                _lhs = value;
            }
        }

        /// <summary>
        /// Gets or sets the alpha variables.
        /// </summary>
        /// <value>The alpha variables.</value>
        public AlphaVariables<PhoneticShapeNode> AlphaVariables
        {
            get
            {
                return _alphaVars;
            }

            set
            {
                _alphaVars = value;
            }
        }

        /// <summary>
        /// Gets or sets the multiple application order.
        /// </summary>
        /// <value>The multiple application order.</value>
        public override MultAppOrder MultApplication
        {
            get
            {
                return _multApplication;
            }

            set
            {
                _multApplication = value;
            }
        }

        /// <summary>
        /// Adds a subrule.
        /// </summary>
        /// <param name="sr">The subrule.</param>
		/// <exception cref="System.ArgumentException">Thrown when the specified subrule is not associated with
		/// this rule.</exception>
        public void AddSubrule(Subrule sr)
        {
            if (sr.Rule != this)
                throw new ArgumentException(HCStrings.kstidPhonSubruleError, "sr");

            _subrules.Add(sr);
        }

        /// <summary>
        /// Unapplies the rule to the specified word analysis.
        /// </summary>
        /// <param name="input">The input word analysis.</param>
        public override void Unapply(WordAnalysis input)
        {
			PhonologicalRuleAnalysisTrace trace = null;
			if (TraceAnalysis)
			{
				// create phonological rule analysis trace record
				trace = new PhonologicalRuleAnalysisTrace(this, input.Clone());
				input.CurrentTrace.AddChild(trace);
			}

            foreach (Subrule sr in _subrules)
                sr.Unapply(input.Shape);

			if (trace != null)
				// add output to trace record
				trace.Output = input.Clone();
        }

        /// <summary>
        /// Applies the rule to the specified word synthesis.
        /// </summary>
        /// <param name="input">The word synthesis.</param>
        public override void Apply(WordSynthesis input)
        {
			PhonologicalRuleSynthesisTrace trace = null;
			if (TraceSynthesis)
			{
				// create phonological rule synthesis trace record
				trace = new PhonologicalRuleSynthesisTrace(this, input.Clone());
				input.CurrentTrace.AddChild(trace);
			}

            // only try to apply applicable subrules
            List<Subrule> subrules = _subrules.Where(sr => sr.IsApplicable(input)).ToList();

        	if (subrules.Count > 0)
            {
                // set all segments to clean
                foreach (Annotation<PhoneticShapeNode> ann in input.Shape.Annotations.Where(ann => ann.Type == "Segment" || ann.Type == "Boundary"))
                	ann.IsClean = true;

                switch (_multApplication)
                {
                    case MultAppOrder.Simultaneous:
                        ApplySimultaneous(input.Shape, subrules);
                        break;

                    case MultAppOrder.LeftToRightIterative:
                        ApplyIterative(input.Shape, Direction.LeftToRight, subrules);
                        break;

                    case MultAppOrder.RightToLeftIterative:
                        ApplyIterative(input.Shape, Direction.RightToLeft, subrules);
                        break;
                }
            }

			// add output to phonological rule trace record
			if (trace != null)
				trace.Output = input.Clone();
        }

        private void ApplySimultaneous(PhoneticShape shape, IEnumerable<Subrule> subrules)
        {
            foreach (Subrule sr in subrules)
            {
                // first find all segments which match the LHS
                var matches = new List<PatternMatch<PhoneticShapeNode>>();
                PhoneticShapeNode node = shape.First;
                PatternMatch<PhoneticShapeNode> match;
                while (FindNextMatchLhs(shape, node, Direction.LeftToRight, out match))
                {
                    // check each candidate match against the subrule's environment
                    Span<PhoneticShapeNode> span = match.EntireMatch;
                    FeatureStructure instantiatedVars = match.VariableValues;
                    if (_lhs.Count == 0
                        ? sr.MatchEnvEmpty(shape, span, Direction.LeftToRight, ModeType.Synthesis, instantiatedVars)
                        : sr.MatchEnvNonempty(shape, span, Direction.LeftToRight, ModeType.Synthesis, instantiatedVars))
                    {
                        matches.Add(match);
                        node = span.End.Next;
                    }
                    else
                    {
                        node = span.Start.Next;
                    }
                }

                // then apply changes
                foreach (PatternMatch<PhoneticShapeNode> m in matches)
                    sr.ApplyRhs(shape, m.EntireMatch, Direction.LeftToRight, m.VariableValues);
            }
        }

        private void ApplyIterative(PhoneticShape shape, Direction dir, IEnumerable<Subrule> subrules)
        {
            PatternMatch<PhoneticShapeNode> match;
            PhoneticShapeNode node = shape.GetFirst(dir);
            // iterate thru each LHS match
            while (FindNextMatchLhs(shape, node, dir, out match))
            {
                Span<PhoneticShapeNode> span = match.EntireMatch;
                FeatureStructure instantiatedVars = match.VariableValues;
                bool matched = false;
                // check each subrule's environment
                foreach (Subrule sr in subrules)
                {
                    if (_lhs.Count == 0
                        ? sr.MatchEnvEmpty(shape, span, dir, ModeType.Synthesis, instantiatedVars)
                        : sr.MatchEnvNonempty(shape, span, dir, ModeType.Synthesis, instantiatedVars))
                    {
						sr.ApplyRhs(shape, span, dir, instantiatedVars);
                        matched = true;
                        break;
                    }
                }

                node = matched ? span.GetEnd(dir).GetNext(dir) : span.GetStart(dir).GetNext(dir);
            }
        }

        private bool FindNextMatchLhs(PhoneticShape shape, PhoneticShapeNode startNode, Direction dir, out PatternMatch<PhoneticShapeNode> match)
        {
            foreach (PhoneticShapeNode node in startNode.GetNodes(dir))
            {
            	FeatureStructure instantiatedVars = _phoneticFeatSys.CreateFeatureStructure();
                if (_lhs.Count == 0)
                {
                    // epenthesis rules always match the LHS
                    match = new PatternMatch<PhoneticShapeNode>(_spanFactory.Create(node), instantiatedVars);
                    return true;
                }
                else
                {
                    if (_lhs.IsMatch(shape.Annotations.GetView(node.Annotation, dir), dir, ModeType.Synthesis, instantiatedVars, out match))
                        return true;
                }
            }

            match = null;
            return false;
        }

        public void Reset()
        {
            _multApplication = MultAppOrder.LeftToRightIterative;
            _alphaVars = null;
            _lhs = null;
            _subrules.Clear();
        }
    }
}
