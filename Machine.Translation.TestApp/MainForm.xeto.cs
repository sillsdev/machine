using System;
using System.Collections.Specialized;
using System.ComponentModel;
using Eto.Drawing;
using Eto.Forms;
using Eto.Serialization.Xaml;

namespace SIL.Machine.Translation.TestApp
{
	public class MainForm : Form
	{
		private StackLayout _suggestionsContainer;
		private RichTextArea _sourceTextArea;
		private RichTextArea _sourceSegmentTextArea;
		private RichTextArea _targetTextArea;
		private TextArea _targetSegmentTextArea;

		public MainForm()
		{
			XamlReader.Load(this);

			var vm = (MainFormViewModel) DataContext;
			vm.Suggestions.CollectionChanged += SuggestionsChanged;
			vm.PropertyChanged += ViewModelPropertyChanged;
		}

		private void ViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			var vm = (MainFormViewModel) DataContext;
			switch (e.PropertyName)
			{
				case "SourceSegmentSelection":
					_sourceSegmentTextArea.Buffer.SetBackground(new Range<int>(0, _sourceSegmentTextArea.Text.Length), Colors.White);
					if (vm.SourceSegmentSelection != null)
						_sourceSegmentTextArea.Buffer.SetBackground(FixRichTextAreaInputRange(_sourceSegmentTextArea, vm.SourceSegmentSelection.Value), Colors.DarkGray);
					break;

				case "SourceTextSelection":
					_sourceTextArea.Buffer.SetForeground(new Range<int>(0, _sourceTextArea.Text.Length), Colors.Gray);
					if (vm.SourceTextSelection != null)
						_sourceTextArea.Buffer.SetForeground(FixRichTextAreaInputRange(_sourceTextArea, vm.SourceTextSelection.Value), Colors.Black);
					break;

				case "TargetTextSelection":
					_targetTextArea.Buffer.SetForeground(new Range<int>(0, _targetTextArea.Text.Length), Colors.Gray);
					if (vm.TargetTextSelection != null)
						_targetTextArea.Buffer.SetForeground(FixRichTextAreaInputRange(_targetTextArea, vm.TargetTextSelection.Value), Colors.Black);
					break;
			}
		}

		private void GetRichTextAreaFixDelta(RichTextArea rta, Range<int> range, out int startDelta, out int endDelta)
		{
			int delta = 0;
			int index = 0;
			while ((index = rta.Text.IndexOf(Environment.NewLine, index, StringComparison.Ordinal)) != -1)
			{
				if (index > range.Start)
					break;
				delta += Environment.NewLine.Length;
				index += Environment.NewLine.Length;
			}
			startDelta = delta;
			endDelta = delta + (index == range.End ? 1 : 0);
		}

		private Range<int> FixRichTextAreaInputRange(RichTextArea rta, Range<int> range)
		{
			if (rta.Platform.IsWpf)
			{
				int startDelta, endDelta;
				GetRichTextAreaFixDelta(rta, range, out startDelta, out endDelta);
				return new Range<int>(range.Start - startDelta, range.End - endDelta);
			}

			return range;
		}

		private Range<int> FixRichTextAreaOutputRange(RichTextArea rta, Range<int> range)
		{
			if (rta.Platform.IsWpf)
			{
				int startDelta, endDelta;
				GetRichTextAreaFixDelta(rta, range, out startDelta, out endDelta);
				return new Range<int>(range.Start + startDelta, range.End + endDelta);
			}

			return range;
		}

		private void SuggestionsChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			var vm = (MainFormViewModel) DataContext;
			_suggestionsContainer.Items.Clear();
			foreach (SuggestionViewModel suggestion in vm.Suggestions)
			{
				var button = new LinkButton {DataContext = suggestion};
				button.TextBinding.BindDataContext((SuggestionViewModel svm) => svm.Text);
				button.BindDataContext(b => b.Command, (SuggestionViewModel svm) => svm.Command);
				_suggestionsContainer.Items.Add(new StackLayoutItem(button));
			}
		}

		private void SourceTextSelectionChanged(object sender, EventArgs e)
		{
			var vm = (MainFormViewModel) DataContext;
			vm.SelectSourceSegmentCommand.Execute(FixRichTextAreaOutputRange(_sourceTextArea, _sourceTextArea.Selection).Start);
			_targetSegmentTextArea.Focus();
		}

		private void TargetTextSelectionChanged(object sender, EventArgs e)
		{
			var vm = (MainFormViewModel) DataContext;
			vm.SelectTargetSegmentCommand.Execute(FixRichTextAreaOutputRange(_targetTextArea, _targetTextArea.Selection).Start);
			_targetSegmentTextArea.Focus();
		}

		private void TargetSegmentCaretIndexChanged(object sender, EventArgs e)
		{
			_targetSegmentTextArea.Focus();
		}

		private void TargetSegmentKeyDown(object sender, KeyEventArgs e)
		{
			var vm = (MainFormViewModel) DataContext;

			if (vm.Suggestions.Count > 0 && e.Control)
			{
				if (e.Key == Keys.A)
				{
					vm.ApplyAllSuggestionsCommand.Execute(null);
					e.Handled = true;
				}
				else if (e.Key >= Keys.D1 && e.Key <= Keys.D0 + vm.Suggestions.Count)
				{
					int index = e.Key - Keys.D1;
					vm.Suggestions[index].Command.Execute(null);
					e.Handled = true;
				}
			}
		}

		private void SegmentNavigationClicked(object sender, EventArgs e)
		{
			_targetSegmentTextArea.Focus();
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			var vm = (MainFormViewModel) DataContext;
			e.Cancel = !vm.CloseCommand.CanExecute(null);
			if (!e.Cancel)
				vm.CloseCommand.Execute(null);
			base.OnClosing(e);
		}
	}
}
