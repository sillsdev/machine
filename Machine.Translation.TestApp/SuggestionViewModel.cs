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

		private void InsertSuggestion()
		{
			var sb = new StringBuilder(_mainFormViewModel.TargetSegment.Trim());
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
