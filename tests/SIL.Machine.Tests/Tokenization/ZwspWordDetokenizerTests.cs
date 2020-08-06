using System.Linq;
using NUnit.Framework;

namespace SIL.Machine.Tokenization
{
	[TestFixture]
	public class ZwspWordDetokenizerTests
	{
		[Test]
		public void Detokenize_Empty()
		{
			var detokenizer = new ZwspWordDetokenizer();
			Assert.That(detokenizer.Detokenize(Enumerable.Empty<string>()), Is.EqualTo(""));
		}

		[Test]
		public void Detokenize_Space()
		{
			var detokenizer = new ZwspWordDetokenizer();
			Assert.That(detokenizer.Detokenize(
				new[] { "គែស", "មាង់", " ", "អី", "នៃ", "ជេង", "នារ", "ត៝ល់", "ព្វាន់", "។" }),
				Is.EqualTo("គែស\u200bមាង់ អី\u200bនៃ\u200bជេង\u200bនារ\u200bត៝ល់\u200bព្វាន់។"));
		}

		[Test]
		public void Detokenize_Guillemet()
		{
			var detokenizer = new ZwspWordDetokenizer();
			Assert.That(detokenizer.Detokenize(new[] { "ឞ្ក្នៃ", "រាញា", "«", "នារ", "»", "ជេសរី" }),
				Is.EqualTo("ឞ្ក្នៃ\u200bរាញា «នារ» ជេសរី"));
		}

		[Test]
		public void Detokenize_Punctuation()
		{
			var detokenizer = new ZwspWordDetokenizer();
			Assert.That(detokenizer.Detokenize(new[] { "ไป", "ไหน", "มา", "?", "เขา", "ถาม", "ผม", "." }),
				Is.EqualTo("ไป\u200bไหน\u200bมา? เขา\u200bถาม\u200bผม."));

			Assert.That(detokenizer.Detokenize(new[] { "ช้าง", ",", "ม้า", ",", "วัว", ",", "กระบือ" }),
				Is.EqualTo("ช้าง, ม้า, วัว, กระบือ"));
		}

		[Test]
		public void Detokenize_PunctuationInsideWord()
		{
			var detokenizer = new ZwspWordDetokenizer();
			Assert.That(detokenizer.Detokenize(new[] { "เริ่ม", "ต้น", "ที่", " ", "7,999", " ", "บาท" }),
				Is.EqualTo("เริ่ม\u200bต้น\u200bที่ 7,999 บาท"));
		}

		[Test]
		public void Detokenize_MultipleSpaces()
		{
			var detokenizer = new ZwspWordDetokenizer();
			Assert.That(detokenizer.Detokenize(
				new[] { "គែស", "មាង់", "  ", "អី", "នៃ", "ជេង", "នារ", "ត៝ល់", "ព្វាន់", "។" }),
				Is.EqualTo("គែស\u200bមាង់  អី\u200bនៃ\u200bជេង\u200bនារ\u200bត៝ល់\u200bព្វាន់។"));
		}
	}
}
