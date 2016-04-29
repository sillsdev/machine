using System;
using System.Text;
using Eto.Forms;
using GalaSoft.MvvmLight;

namespace SIL.Machine.Translation.TestApp
{
	public class SuggestionViewModel : ViewModelBase
	{
		private readonly TextViewModel _textViewModel;
		private readonly string _text;
		private readonly RelayCommand<object> _command; 

		public SuggestionViewModel(TextViewModel textViewModel, string text)
		{
			_text = text;
			_textViewModel = textViewModel;
			_command = new RelayCommand<object>(o => InsertSuggestion());
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
			sb.Append(_text);
			sb.Append(" ");
			_textViewModel.TargetSegment = sb.ToString();
			_textViewModel.CurrentTargetSegmentIndex = _textViewModel.TargetSegment.Length;
		}

		public string Text
		{
			get { return _text; }
		}

		public ICommand Command
		{
			get { return _command; }
		}
	}
}
