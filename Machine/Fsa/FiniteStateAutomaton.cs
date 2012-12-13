using System;
using System.Collections.Generic;
using System.IO;
using SIL.Collections;

namespace SIL.Machine.Fsa
{
	public class FiniteStateAutomaton<TData, TOffset, TResult> where TData : IData<TOffset>
	{
		private readonly List<State<TData, TOffset, TResult>> _states;
		private readonly SimpleReadOnlyCollection<State<TData, TOffset, TResult>> _readonlyStates;

		public FiniteStateAutomaton()
		{
			_states = new List<State<TData, TOffset, TResult>>();
			_readonlyStates = _states.AsSimpleReadOnlyCollection();
		}

		public bool Deterministic { get; protected set; }

		public State<TData, TOffset, TResult> CreateAcceptingState()
		{
			var state = new State<TData, TOffset, TResult>(_states.Count, true);
			_states.Add(state);
			return state;
		}

		protected State<TData, TOffset, TResult> CreateAcceptingState(IEnumerable<AcceptInfo<TData, TOffset, TResult>> acceptInfos)
		{
			var state = new State<TData, TOffset, TResult>(StatesList.Count, acceptInfos);
			StatesList.Add(state);
			return state;
		}

		public State<TData, TOffset, TResult> CreateAcceptingState(string id, Func<TData, TResult, bool> acceptable, int priority)
		{
			var state = new State<TData, TOffset, TResult>(StatesList.Count, new AcceptInfo<TData, TOffset, TResult>(id, acceptable, priority).ToEnumerable());
			StatesList.Add(state);
			return state;
		}

		public State<TData, TOffset, TResult> CreateState()
		{
			var state = new State<TData, TOffset, TResult>(_states.Count, false);
			_states.Add(state);
			return state;
		}

		public State<TData, TOffset, TResult> StartState { get; set; } 

		public IReadOnlyCollection<State<TData, TOffset, TResult>> States
		{
			get { return _readonlyStates; }
		}

		public void Reset()
		{
			_states.Clear();
			StartState = null;
			Deterministic = false;
		}

		protected IList<State<TData, TOffset, TResult>> StatesList
		{
			get { return _states; }
		}

		public void ToGraphViz(TextWriter writer)
		{
			writer.WriteLine("digraph G {");

			var stack = new Stack<State<TData, TOffset, TResult>>();
			var processed = new HashSet<State<TData, TOffset, TResult>>();
			stack.Push(StartState);
			while (stack.Count != 0)
			{
				State<TData, TOffset, TResult> state = stack.Pop();
				processed.Add(state);

				writer.Write("  {0} [shape=\"{1}\", color=\"{2}\"", state.Index, state.Equals(StartState) ? "diamond" : "circle",
					state.Equals(StartState) ? "green" : state.IsAccepting ? "red" : "black");
				if (state.IsAccepting)
					writer.Write(", peripheries=\"2\"");
				writer.WriteLine("];");

				foreach (Arc<TData, TOffset, TResult> arc in state.Arcs)
				{
					writer.WriteLine("  {0} -> {1} [label=\"{2}\"];", state.Index, arc.Target.Index,
						arc.ToString().Replace("\"", "\\\""));
					if (!processed.Contains(arc.Target) && !stack.Contains(arc.Target))
						stack.Push(arc.Target);
				}
			}

			writer.WriteLine("}");
		}
	}
}
