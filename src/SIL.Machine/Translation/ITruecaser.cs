using System.Collections.Generic;
using System.Threading.Tasks;
using SIL.Machine.Corpora;

namespace SIL.Machine.Translation
{
	public interface ITruecaser
	{
		ITrainer CreateTrainer(IEnumerable<TextRow> corpus);

		void TrainSegment(IReadOnlyList<string> segment, bool sentenceStart = true);

		Task SaveAsync();
		void Save();

		IReadOnlyList<string> Truecase(IReadOnlyList<string> segment);
	}
}
