﻿using System;
using System.Collections.Generic;
using System.Linq;
using SIL.Machine.Annotations;
using SIL.Machine.DataStructures;
using SIL.Machine.FeatureModel;
using SIL.Machine.FiniteState;

namespace SIL.Machine.Morphology.HermitCrab
{
    internal class RootAllomorphTrie
    {
        private readonly Fst<Shape, ShapeNode> _fsa;
        private readonly Func<Annotation<ShapeNode>, bool> _filter;
        private int _shapeCount;
        private readonly Dictionary<string, RootAllomorph> _allomorphs;

        public RootAllomorphTrie(Func<Annotation<ShapeNode>, bool> filter)
        {
            _fsa = new Fst<Shape, ShapeNode> { Filter = filter, UseUnification = true };
            _fsa.StartState = _fsa.CreateState();
            _filter = filter;
            _allomorphs = new Dictionary<string, RootAllomorph>();
        }

        public void Add(RootAllomorph allomorph)
        {
            _allomorphs[allomorph.ID] = allomorph;
            ShapeNode first = allomorph.Segments.Shape.GetFirst(n => _filter(n.Annotation));
            if (first == allomorph.Segments.Shape.End)
                _fsa.StartState.Arcs.Add(_fsa.CreateAcceptingState(allomorph.ID, (s, match) => true, _shapeCount));
            else
                AddNode(first, _fsa.StartState, allomorph.ID);
            _shapeCount++;
        }

        private void AddNode(ShapeNode node, State<Shape, ShapeNode> state, string id)
        {
            Arc<Shape, ShapeNode> arc = state.Arcs.FirstOrDefault(a =>
                node.Annotation.FeatureStruct.ValueEquals(a.Input.FeatureStruct)
            );
            ShapeNode nextNode = node.GetNext(n => _filter(n.Annotation));
            State<Shape, ShapeNode> nextState;
            if (arc != null)
            {
                nextState = arc.Target;
                if (nextNode == node.List.End)
                {
                    nextState.IsAccepting = true;
                    nextState.AcceptInfos.Add(
                        new AcceptInfo<Shape, ShapeNode>(id, (shape, match) => true, _shapeCount)
                    );
                }
            }
            else
            {
                nextState =
                    nextNode == node.List.End
                        ? _fsa.CreateAcceptingState(id, (shape, match) => true, _shapeCount)
                        : _fsa.CreateState();
                FeatureStruct condition = node.Annotation.FeatureStruct.Clone();
                condition.Freeze();
                state.Arcs.Add(condition, nextState);
            }

            if (nextNode != node.List.End)
                AddNode(nextNode, nextState, id);
        }

        public IEnumerable<RootAllomorph> Search(Shape shape)
        {
            Annotation<ShapeNode> startAnn = shape.Annotations.GetFirst(_filter);
            IEnumerable<FstResult<Shape, ShapeNode>> matches;
            if (_fsa.Transduce(shape, startAnn, null, true, true, false, out matches))
            {
                foreach (FstResult<Shape, ShapeNode> match in matches)
                    yield return _allomorphs[match.ID];
            }
        }
    }
}
