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
		public MainForm()
		{
			XamlReader.Load(this);

			var vm = (MainFormViewModel) DataContext;
			vm.Suggestions.CollectionChanged += SuggestionsChanged;
			vm.PropertyChanged += ViewModelPropertyChanged;
			vm.UnapprovedTargetSegmentRanges.CollectionChanged += UnapprovedTargetSegmentRangesChanged;

			SourceTextArea.Cursor = Cursors.Pointer;
			TargetTextArea.Cursor = Cursors.Pointer;
		}

		protected StackLayout SuggestionsContainer { get; set; }
		protected RichTextArea SourceTextArea { get; set; }
		protected RichTextArea SourceSegmentTextArea { get; set; }
		protected RichTextArea TargetTextArea { get; set; }
		protected TextArea TargetSegmentTextArea { get; set; }

		private void UnapprovedTargetSegmentRangesChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			var vm = (MainFormViewModel) DataContext;
			TargetTextArea.Buffer.SetBackground(new Range<int>(0, TargetTextArea.Text.Length), Colors.White);
			foreach (Range<int> range in vm.UnapprovedTargetSegmentRanges)
				TargetTextArea.Buffer.SetBackground(FixRichTextAreaInputRange(TargetTextArea, range), Colors.LightYellow);
		}

		private void ViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			var vm = (MainFormViewModel) DataContext;
			switch (e.PropertyName)
			{
				case "CurrentSourceWordRange":
					SourceSegmentTextArea.Buffer.SetBackground(new Range<int>(0, SourceSegmentTextArea.Text.Length), Colors.White);
					if (vm.CurrentSourceWordRange != null)
					{
						Color c = Colors.White;
						switch (vm.CurrentSourceWordLevel)
						{
							case TranslationLevel.Transfer:
								c = Colors.DarkGray;
								break;
							case TranslationLevel.LowConfidence:
								c = Colors.SkyBlue;
								break;
							case TranslationLevel.HighConfidence:
								c = Colors.Orange;
								break;
						}
						SourceSegmentTextArea.Buffer.SetBackground(FixRichTextAreaInputRange(SourceSegmentTextArea, vm.CurrentSourceWordRange.Value), c);
					}
					break;

				case "CurrentSourceSegmentRange":
					SourceTextArea.Buffer.SetForeground(new Range<int>(0, SourceTextArea.Text.Length), Colors.Gray);
					if (vm.CurrentSourceSegmentRange != null)
						SourceTextArea.Buffer.SetForeground(FixRichTextAreaInputRange(SourceTextArea, vm.CurrentSourceSegmentRange.Value), Colors.Black);
					break;

				case "CurrentTargetSegmentRange":
					TargetTextArea.Buffer.SetForeground(new Range<int>(0, TargetTextArea.Text.Length), Colors.Gray);
					if (vm.CurrentTargetSegmentRange != null)
						TargetTextArea.Buffer.SetForeground(FixRichTextAreaInputRange(TargetTextArea, vm.CurrentTargetSegmentRange.Value), Colors.Black);
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
			SuggestionsContainer.Items.Clear();
			foreach (SuggestionViewModel suggestion in vm.Suggestions)
			{
				var button = new LinkButton {DataContext = suggestion};
				button.TextBinding.BindDataContext((SuggestionViewModel svm) => svm.Text);
				button.BindDataContext(b => b.Command, (SuggestionViewModel svm) => svm.Command);
				SuggestionsContainer.Items.Add(new StackLayoutItem(button));
			}
		}

		protected void SourceTextSelectionChanged(object sender, EventArgs e)
		{
			var vm = (MainFormViewModel) DataContext;
			vm.SelectSourceSegmentCommand.Execute(FixRichTextAreaOutputRange(SourceTextArea, SourceTextArea.Selection).Start);
			TargetSegmentTextArea.Focus();
		}

		protected void TargetTextSelectionChanged(object sender, EventArgs e)
		{
			var vm = (MainFormViewModel) DataContext;
			vm.SelectTargetSegmentCommand.Execute(FixRichTextAreaOutputRange(TargetTextArea, TargetTextArea.Selection).Start);
			TargetSegmentTextArea.Focus();
		}

		protected void TargetSegmentCaretIndexChanged(object sender, EventArgs e)
		{
			TargetSegmentTextArea.Focus();
		}

		protected void TargetSegmentKeyDown(object sender, KeyEventArgs e)
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

		protected void SegmentNavigationClicked(object sender, EventArgs e)
		{
			TargetSegmentTextArea.Focus();
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
