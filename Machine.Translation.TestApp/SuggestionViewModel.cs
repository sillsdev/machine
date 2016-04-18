using System;
using System.Text;
using Eto.Forms;
using GalaSoft.MvvmLight;

namespace SIL.Machine.Translation.TestApp
{
	public class SuggestionViewModel : ViewModelBase
	{
		private readonly MainFormViewModel _mainFormViewModel;
		private readonly string _text;
		private readonly RelayCommand<object> _command; 

		public SuggestionViewModel(MainFormViewModel mainFormViewModel, string text)
		{
			_text = text;
			_mainFormViewModel = mainFormViewModel;
			_command = new RelayCommand<object>(o => InsertSuggestion());
		}

		internal void InsertSuggestion()
		{
			var sb = new StringBuilder(_mainFormViewModel.TargetSegment.Trim());
			if (!_mainFormViewModel.TargetSegment.EndsWith(" "))
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
			_mainFormViewModel.TargetSegment = sb.ToString();
			_mainFormViewModel.CurrentTargetSegmentIndex = _mainFormViewModel.TargetSegment.Length;
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
