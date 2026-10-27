using System;
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
        private bool _hasEmptyAllomorph;
        private readonly Dictionary<string, int> _rootLengths = new Dictionary<string, int>();
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
            _rootLengths[allomorph.ID] = allomorph.Segments.Shape.Count(n => _filter(n.Annotation));
            ShapeNode first = allomorph.Segments.Shape.GetFirst(n => _filter(n.Annotation));
            if (first == allomorph.Segments.Shape.End)
            {
                _hasEmptyAllomorph = true;
                _fsa.StartState.Arcs.Add(_fsa.CreateAcceptingState(allomorph.ID, (s, match) => true, _shapeCount));
            }
            else
            {
                AddNode(first, _fsa.StartState, allomorph.ID);
            }
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

        public bool IsEmpty => _allomorphs.Count == 0;

        /// <summary>
        /// Whether any root occurs as a contiguous run of the shape's segments, starting anywhere and ending
        /// anywhere. Stops at the first accepting path.
        /// </summary>
        public bool ContainsRootSubstring(Shape shape)
        {
            if (_hasEmptyAllomorph)
                return true;
            Annotation<ShapeNode> startAnn = shape.Annotations.GetFirst(_filter);
            return _fsa.Transduce(shape, startAnn, null, false, false, false, out FstResult<Shape, ShapeNode> _);
        }

        /// <summary>
        /// Finds the first root run by trying each start segment in turn, which costs the same arcs as
        /// <see cref="ContainsRootSubstring"/> and also says where the run is, in segment indices.
        /// </summary>
        public bool TryFindRootRun(Shape shape, out RootRun run)
        {
            run = null;
            if (_hasEmptyAllomorph)
                return true;
            int index = 0;
            foreach (Annotation<ShapeNode> ann in shape.Annotations)
            {
                if (!_filter(ann))
                    continue;
                if (_fsa.Transduce(shape, ann, null, true, false, false, out FstResult<Shape, ShapeNode> result))
                {
                    run = new RootRun(index, index + _rootLengths[result.ID]);
                    return true;
                }
                index++;
            }
            return false;
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
