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

            private readonly Pattern<PhoneticShapeNode> _leftEnv;
        	private readonly Pattern<PhoneticShapeNode> _rightEnv;
            private readonly Pattern<PhoneticShapeNode> _rhs;
            private readonly Pattern<PhoneticShapeNode> _analysisTarget;
        	private readonly Pattern<PhoneticShapeNode> _synthesisTarget;
			private readonly StandardPhonologicalRule _rule;

            private IDBearerSet<PartOfSpeech> _requiredPartsOfSpeech;
            private MprFeatureSet _excludedMprFeatures;
            private MprFeatureSet _requiredMprFeatures;

        	private readonly int _delReapplications;

        	/// <summary>
        	/// Initializes a new instance of the <see cref="Subrule"/> class.
        	/// </summary>
        	/// <param name="delReapplications"></param>
        	/// <param name="rhs">The RHS.</param>
        	/// <param name="leftEnv">The left environment.</param>
        	/// <param name="rightEnv">The right environment.</param>
        	/// <param name="rule">The phonological rule.</param>
        	/// <exception cref="System.ArgumentException">Thrown when the size of the RHS is greater than the
        	/// size of the specified rule's LHS and the LHS's size is greater than 1. A standard phonological
        	/// rule does not currently support this type of widening.</exception>
        	public Subrule(int delReapplications, Pattern<PhoneticShapeNode> rhs, Pattern<PhoneticShapeNode> leftEnv, Pattern<PhoneticShapeNode> rightEnv,
				StandardPhonologicalRule rule)
            {
                _rhs = rhs;
                _leftEnv = leftEnv;
        		_rightEnv = rightEnv;
                _rule = rule;
        		_delReapplications = delReapplications;
        		_synthesisTarget = CreateTarget(_rule._lhs);
                switch (Type)
                {
                    case ChangeType.Narrow:
                    case ChangeType.Epenthesis:
                        // analysis target is a copy of the RHS, because there is no LHS
                		_analysisTarget = CreateTarget(_rhs);
                        break;

                    case ChangeType.Widen:
                        // before generating the analysis we must extend the length of the LHS
                        // to match the length of the RHS
                        Pattern<PhoneticShapeNode> lhs = _rule._lhs.Clone();
                        while (lhs.Count != _rhs.Count)
                            lhs.Add(lhs.First.Clone());
                		_analysisTarget = CreateTarget(_rhs, lhs);
                        break;

                    case ChangeType.Feature:
                        _analysisTarget = CreateTarget(_rhs, _rule._lhs);
                        break;

                    case ChangeType.Unknown:
						throw new ArgumentException(HCStrings.kstidInvalidSubruleType, "rhs");
                }
            }

			private Pattern<PhoneticShapeNode> CreateTarget(Pattern<PhoneticShapeNode> pattern)
			{
				var target = new Pattern<PhoneticShapeNode>(_rule._spanFactory, new[] { "Segment", "Boundary" }, new[] { "Segment" });
				if (_leftEnv != null)
					target.Add(new Group<PhoneticShapeNode>("leftEnv", _leftEnv.Select(node => node.Clone())));
				if (pattern.Count > 0)
					target.Add(new Group<PhoneticShapeNode>("target", pattern.Select(node => node.Clone())));
				if (_rightEnv != null)
					target.Add(new Group<PhoneticShapeNode>("rightEnv", _rightEnv.Select(node => node.Clone())));
				return target;
			}

			private Pattern<PhoneticShapeNode> CreateTarget(Pattern<PhoneticShapeNode> rhs, Pattern<PhoneticShapeNode> lhs)
			{
				var target = new Pattern<PhoneticShapeNode>(_rule._spanFactory, new [] {"Segment", "Boundary"}, new [] {"Segment"});
				if (_leftEnv != null)
					target.AddMany(_leftEnv.Select(node => node.Clone()));

				var group = new Group<PhoneticShapeNode>("target");
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
						group.Nodes.Add(result);
					}
				}
				target.Add(group);

				if (_rightEnv != null)
					target.AddMany(_rightEnv.Select(node => node.Clone()));
				return target;
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
									if (!IsNonSelfOpaquing(constraints, _leftEnv))
										return true;
									if (!IsNonSelfOpaquing(constraints, _rightEnv))
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
                            var nestedPattern = (Group<PhoneticShapeNode>) node;
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

            private bool UnapplyIterative(PhoneticShape shape, Direction dir)
            {
                bool unapplied = false;
				PhoneticShapeNode node = shape.GetFirst(dir);
            	Span<PhoneticShapeNode> span;
            	FeatureStructure varValues;
            	while (FindNextMatch(_analysisTarget, shape, node, dir, ModeType.Analysis, out span, out varValues))
            	{
        			if (CheckVacuousUnapplication(span, dir))
        			{
						// unapply the subrule
						UnapplyRhs(dir, span, varValues);
						unapplied = true;
        				node = span.GetEnd(dir).GetNext(dir);
        			}
        			else
        			{
        				node = node.GetNext(dir);
        			}
            	}

                return unapplied;
            }

            private bool UnapplyNarrow(PhoneticShape shape)
            {
				var matches = new List<Tuple<Span<PhoneticShapeNode>, FeatureStructure>>();
				PhoneticShapeNode node = shape.First;
            	Span<PhoneticShapeNode> span;
            	FeatureStructure varValues;
				// deletion subrules are always treated like simultaneous subrules during unapplication
				while (FindNextMatch(_analysisTarget, shape, node, Direction.LeftToRight, ModeType.Analysis, out span, out varValues))
				{
					matches.Add(Tuple.Create(span, varValues));
					node = span.Start.Next;
				}

				// deletion subrules are always treated like simultaneous subrules during unapplication
				foreach (Tuple<Span<PhoneticShapeNode>, FeatureStructure> match in matches)
				{
					PhoneticShapeNode cur = match.Item1.End;
					foreach (PatternNode<PhoneticShapeNode> lhsNode in _rule._lhs)
					{
						var constraints = lhsNode as AnnotationConstraints<PhoneticShapeNode>;
						if (constraints == null)
							continue;

						var newNode = new PhoneticShapeNode(_rule._spanFactory, constraints.AnnotationType,
						                                    _rule._phoneticFeatSys.CreateFeatureStructure());
						newNode.Annotation.FeatureStructure.UninstantiateAll();
						newNode.Annotation.FeatureStructure.Instantiate(constraints.FeatureStructure);
						// mark the undeleted segment as optional
						newNode.Annotation.IsOptional = true;
						cur.Insert(newNode, Direction.LeftToRight);
						cur = newNode;
					}

					if (_rhs.Count > 0)
					{
						foreach (PhoneticShapeNode matchNode in match.Item1.Start.GetNodes(match.Item1.End))
							matchNode.Annotation.IsOptional = true;
					}
				}

            	return matches.Count > 0;
            }

			private bool FindNextMatch(Pattern<PhoneticShapeNode> pattern, PhoneticShape shape, PhoneticShapeNode startNode, Direction dir, ModeType mode,
				out Span<PhoneticShapeNode> span, out FeatureStructure varValues)
			{
				foreach (PhoneticShapeNode curNode in startNode.GetNodes(dir).Where(node => node.Annotation.Type == "Segment"))
				{
					if (pattern.Count == 0)
					{
						span = _rule._spanFactory.Create(curNode);
						varValues = _rule._phoneticFeatSys.CreateFeatureStructure();
						return true;
					}

					PatternMatch<PhoneticShapeNode> match;
					if (pattern.IsMatch(shape.Annotations.GetView(curNode.Annotation, dir), dir, mode, _rule._phoneticFeatSys.CreateFeatureStructure(),
					                    out match))
					{
						if (match["target"] != null)
							span = match["target"];
						else if (match["leftEnv"] != null)
							span = _rule._spanFactory.Create(match["leftEnv"].End);
						else
							span = _rule._spanFactory.Create(match["rightEnv"].Start.Prev);
						varValues = match.VariableValues;
						// TODO: remove ambiguous variable values
						return true;
					}
				}

				span = null;
				varValues = null;
				return false;
			}

			public bool FindNextMatchLhs(PhoneticShape shape, PhoneticShapeNode startNode, Direction dir, out Span<PhoneticShapeNode> span,
				out FeatureStructure varValues)
			{
				return FindNextMatch(_synthesisTarget, shape, startNode, dir, ModeType.Synthesis, out span, out varValues);
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
					var newNode = new PhoneticShapeNode(_rule._spanFactory, constraints.AnnotationType, _rule._phoneticFeatSys.CreateFeatureStructure());
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

            /// <summary>
            /// Checks if the subrule will be unapplied vacuously. Vacuous unapplication means that
            /// the subrule will actually make changes to the phonetic shape. This is important to know
            /// for self-opaquing, simultaneously applying subrules, since we unapply these subrules
            /// until they unapply nonvacuously.
            /// </summary>
            /// <param name="match">The match.</param>
            /// <param name="dir">The direction.</param>
            /// <returns></returns>
            private bool CheckVacuousUnapplication(Span<PhoneticShapeNode> match, Direction dir)
            {
                PatternNode<PhoneticShapeNode> rhsNode = _rhs.GetFirst(dir);
            	PhoneticShapeNode shapeNode = match.GetStart(dir);
				while (shapeNode != match.GetEnd(dir).GetNext(dir))
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

        private static void ApplySimultaneous(PhoneticShape shape, IEnumerable<Subrule> subrules)
        {
            foreach (Subrule sr in subrules)
            {
                // first find all segments which match the LHS
            	var matches = new List<Tuple<Span<PhoneticShapeNode>, FeatureStructure>>();
				PhoneticShapeNode node = shape.First;
            	Span<PhoneticShapeNode> span;
            	FeatureStructure varValues;
            	while (sr.FindNextMatchLhs(shape, node, Direction.LeftToRight, out span, out varValues))
            	{
            		matches.Add(Tuple.Create(span, varValues));
            		node = span.End.Next;
            	}

                // then apply changes
				foreach (Tuple<Span<PhoneticShapeNode>, FeatureStructure> match in matches)
                    sr.ApplyRhs(shape, match.Item1, Direction.LeftToRight, match.Item2);
            }
        }

        private static void ApplyIterative(PhoneticShape shape, Direction dir, IEnumerable<Subrule> subrules)
        {
			PatternMatch<PhoneticShapeNode> match;
			PhoneticShapeNode node = shape.GetFirst(dir);
			foreach (Subrule sr in subrules)
			{
				
			}
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

        public void Reset()
        {
            _multApplication = MultAppOrder.LeftToRightIterative;
            _alphaVars = null;
            _lhs = null;
            _subrules.Clear();
        }
    }
}
