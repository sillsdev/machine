using System;
using System.Collections.Specialized;
using System.ComponentModel;
using Eto.Forms;
using Eto.Drawing;
using Eto.Serialization.Xaml;
using SIL.Extensions;

namespace SIL.Machine.Translation.TestApp
{
    public class TextView : Panel
    {
        private TextViewModel _prevDataContext;

        public TextView()
        {
            XamlReader.Load(this);

            SourceTextArea.Cursor = Cursors.Pointer;
            TargetTextArea.Cursor = Cursors.Pointer;
        }

        protected StackLayout SuggestionsContainer { get; set; }
        protected RichTextArea SourceTextArea { get; set; }
        protected RichTextArea SourceSegmentTextArea { get; set; }
        protected RichTextArea TargetTextArea { get; set; }
        protected TextArea TargetSegmentTextArea { get; set; }

        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);

            if (_prevDataContext != null)
            {
                _prevDataContext.Suggestions.CollectionChanged -= SuggestionsChanged;
                _prevDataContext.PropertyChanged -= ViewModelPropertyChanged;
                _prevDataContext.UnapprovedTargetSegmentRanges.CollectionChanged -=
                    UnapprovedTargetSegmentRangesChanged;
                _prevDataContext.AlignedSourceWords.CollectionChanged -= AlignedSourceWordsChanged;
            }

            var vm = DataContext as TextViewModel;
            if (vm != null)
            {
                vm.Suggestions.CollectionChanged += SuggestionsChanged;
                vm.PropertyChanged += ViewModelPropertyChanged;
                vm.UnapprovedTargetSegmentRanges.CollectionChanged += UnapprovedTargetSegmentRangesChanged;
                vm.AlignedSourceWords.CollectionChanged += AlignedSourceWordsChanged;

                UpdateSuggestions(vm);
                UpdateTargetTextBackground(vm);
                UpdateSourceSegmentBackground(vm);
                UpdateTextForeground(SourceTextArea, vm.CurrentSourceSegmentRange);
                UpdateTextForeground(TargetTextArea, vm.CurrentTargetSegmentRange);
                TargetSegmentTextArea.Focus();
            }
            _prevDataContext = vm;
        }

        private void AlignedSourceWordsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            var vm = DataContext as TextViewModel;
            if (vm != null)
                UpdateSourceSegmentBackground(vm);
        }

        private void UnapprovedTargetSegmentRangesChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            var vm = DataContext as TextViewModel;
            if (vm != null)
                UpdateTargetTextBackground(vm);
        }

        private void UpdateTargetTextBackground(TextViewModel vm)
        {
            int endOffset = GetRichTextAreaEndOffset(TargetTextArea);
            if (endOffset > 0)
                TargetTextArea.Buffer.SetBackground(new Range<int>(0, endOffset), Colors.White);
            foreach (Range<int> range in vm.UnapprovedTargetSegmentRanges)
                TargetTextArea.Buffer.SetBackground(range, Colors.LightYellow);
        }

        private void ViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var vm = DataContext as TextViewModel;
            if (vm == null)
                return;

            switch (e.PropertyName)
            {
                case nameof(TextViewModel.CurrentSourceSegmentRange):
                    UpdateTextForeground(SourceTextArea, vm.CurrentSourceSegmentRange);
                    break;

                case nameof(TextViewModel.CurrentTargetSegmentRange):
                    UpdateTextForeground(TargetTextArea, vm.CurrentTargetSegmentRange);
                    break;
            }
        }

        private void UpdateSourceSegmentBackground(TextViewModel vm)
        {
            int endOffset = GetRichTextAreaEndOffset(SourceSegmentTextArea);
            if (endOffset > 0)
                SourceSegmentTextArea.Buffer.SetBackground(new Range<int>(0, endOffset), Colors.White);
            foreach (AlignedWordViewModel alignedWord in vm.AlignedSourceWords)
            {
                Color c = Colors.White;
                switch (alignedWord.Level)
                {
                    case WordTranslationLevel.Transfer:
                        c = Colors.DarkGray;
                        break;
                    case WordTranslationLevel.LowConfidence:
                        c = Colors.SkyBlue;
                        break;
                    case WordTranslationLevel.HighConfidence:
                        c = Colors.Orange;
                        break;
                }
                SourceSegmentTextArea.Buffer.SetBackground(alignedWord.Range, c);
            }
        }

        private static void UpdateTextForeground(RichTextArea rta, Range<int>? currentSegmentRange)
        {
            int endOffset = GetRichTextAreaEndOffset(rta);
            if (endOffset > 0)
                rta.Buffer.SetForeground(new Range<int>(0, endOffset), Colors.Gray);
            if (currentSegmentRange != null)
                rta.Buffer.SetForeground(currentSegmentRange.Value, Colors.Black);
        }

        private void SuggestionsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            var vm = DataContext as TextViewModel;
            if (vm != null)
                UpdateSuggestions(vm);
        }

        private void UpdateSuggestions(TextViewModel vm)
        {
            SuggestionsContainer.Items.Clear();
            foreach (SuggestionViewModel suggestion in vm.Suggestions)
            {
                var button = new LinkButton { DataContext = suggestion };
                button.TextBinding.BindDataContext((SuggestionViewModel svm) => svm.Text);
                button.BindDataContext(b => b.Command, (SuggestionViewModel svm) => svm.Command);
                SuggestionsContainer.Items.Add(new StackLayoutItem(button));
            }
        }

        protected void SourceTextSelectionChanged(object sender, EventArgs e)
        {
            var vm = DataContext as TextViewModel;
            if (vm != null)
            {
                vm.SelectSourceSegmentCommand.Execute(SourceTextArea.Selection.Start);
                TargetSegmentTextArea.Focus();
            }
        }

        protected void TargetTextSelectionChanged(object sender, EventArgs e)
        {
            var vm = DataContext as TextViewModel;
            if (vm != null)
            {
                vm.SelectTargetSegmentCommand.Execute(TargetTextArea.Selection.Start);
                TargetSegmentTextArea.Focus();
            }
        }

        protected void TargetSegmentCaretIndexChanged(object sender, EventArgs e)
        {
            TargetSegmentTextArea.Focus();
        }

        protected void TargetSegmentKeyDown(object sender, KeyEventArgs e)
        {
            var vm = DataContext as TextViewModel;
            if (vm?.Suggestions.Count > 0 && e.Control)
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

        protected void TargetSegmentTextChanged(object sender, EventArgs e)
        {
            string text = TargetSegmentTextArea.Text;
            if (text.Length >= 2 && text[text.Length - 2] == ' ' && text[text.Length - 1].IsOneOf('.', ',', ';'))
            {
                Range<int> selection = TargetSegmentTextArea.Selection;
                TargetSegmentTextArea.Text = text.Substring(0, text.Length - 2) + text.Substring(text.Length - 1) + " ";
                TargetSegmentTextArea.Selection = selection;
            }
        }

        private static int GetRichTextAreaEndOffset(RichTextArea rta)
        {
            return rta.Text.TrimEnd('\n').Length;
        }
    }
}
