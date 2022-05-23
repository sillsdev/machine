using System;
using System.Text;
using System.Windows.Input;
using GalaSoft.MvvmLight;

namespace SIL.Machine.Translation.TestApp
{
    public class SuggestionViewModel : ViewModelBase
    {
        private readonly TextViewModel _textViewModel;
        private readonly RelayCommand _command;

        public SuggestionViewModel(TextViewModel textViewModel, string text)
        {
            Text = text;
            _textViewModel = textViewModel;
            _command = new RelayCommand(InsertSuggestion);
        }

        internal void InsertSuggestion()
        {
            var sb = new StringBuilder(_textViewModel.TargetSegment.Trim());
            if (!_textViewModel.TargetSegment.EndsWith(" "))
            {
                int index = sb.ToString().LastIndexOf(" ", StringComparison.Ordinal);
                if (index == -1)
                    index = 0;
                sb.Remove(index, sb.Length - index);
            }
            if (sb.Length > 0)
                sb.Append(" ");
            sb.Append(Text);
            sb.Append(" ");
            _textViewModel.TargetSegment = sb.ToString();
            _textViewModel.CurrentTargetSegmentIndex = _textViewModel.TargetSegment.Length;
        }

        public string Text { get; }

        public ICommand Command => _command;
    }
}
