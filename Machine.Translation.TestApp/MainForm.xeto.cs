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
		private RichTextArea _sourceSegmentTextArea;
		private TextArea _targetSegmentTextArea;
		private Range<int>? _prevSourceSegmentSelection; 

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
					if (_prevSourceSegmentSelection != null)
						_sourceSegmentTextArea.Buffer.SetBackground(_prevSourceSegmentSelection.Value, Colors.White);
					if (vm.SourceSegmentSelection != null)
					{
						_sourceSegmentTextArea.Selection = vm.SourceSegmentSelection.Value;
						_sourceSegmentTextArea.SelectionBackground = Colors.DarkGray;
					}
					_prevSourceSegmentSelection = vm.SourceSegmentSelection;
					break;
			}
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
