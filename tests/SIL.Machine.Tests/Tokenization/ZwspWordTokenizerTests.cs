using NUnit.Framework;

namespace SIL.Machine.Tokenization
{
	[TestFixture]
	public class ZwspWordTokenizerTests
	{
		[Test]
		public void Tokenize_Empty()
		{
			var tokenizer = new ZwspWordTokenizer();
			Assert.That(tokenizer.Tokenize(""), Is.Empty);
		}

		[Test]
		public void Tokenize_Zwsp()
		{
			var tokenizer = new ZwspWordTokenizer();
			Assert.That(tokenizer.Tokenize("\u200b"), Is.Empty);
		}

		[Test]
		public void Tokenize_Space()
		{
			var tokenizer = new ZwspWordTokenizer();
			Assert.That(tokenizer.Tokenize("គែស\u200bមាង់ អី\u200bនៃ\u200bជេង\u200bនារ\u200bត៝ល់\u200bព្វាន់។"),
				Is.EqualTo(new[] { "គែស", "មាង់", " ", "អី", "នៃ", "ជេង", "នារ", "ត៝ល់", "ព្វាន់", "។" }));
		}

		[Test]
		public void Tokenize_Guillemet()
		{
			var tokenizer = new ZwspWordTokenizer();
			Assert.That(tokenizer.Tokenize("ឞ្ក្នៃ\u200bរាញា «នារ» ជេសរី"),
				Is.EqualTo(new[] { "ឞ្ក្នៃ", "រាញា", "«", "នារ", "»", "ជេសរី" }));
		}

		[Test]
		public void Tokenize_Punctuation()
		{
			var tokenizer = new ZwspWordTokenizer();
			Assert.That(tokenizer.Tokenize("ไป\u200bไหน\u200bมา? เขา\u200bถาม\u200bผม."),
				Is.EqualTo(new[] { "ไป", "ไหน", "มา", "?", "เขา", "ถาม", "ผม", "." }));

			Assert.That(tokenizer.Tokenize("ช้าง, ม้า, วัว, กระบือ"),
				Is.EqualTo(new[] { "ช้าง", ",", "ม้า", ",", "วัว", ",", "กระบือ" }));
		}

		[Test]
		public void Tokenize_PunctuationInsideWord()
		{
			var tokenizer = new ZwspWordTokenizer();
			Assert.That(tokenizer.Tokenize("เริ่ม\u200bต้น\u200bที่ 7,999 บาท"),
				Is.EqualTo(new[] { "เริ่ม", "ต้น", "ที่", " ", "7,999", " ", "บาท" }));
		}

		[Test]
		public void Tokenize_MultipleSpaces()
		{
			var tokenizer = new ZwspWordTokenizer();
			Assert.That(tokenizer.Tokenize("គែស\u200bមាង់  អី\u200bនៃ\u200bជេង\u200bនារ\u200bត៝ល់\u200bព្វាន់។"),
				Is.EqualTo(new[] { "គែស", "មាង់", "  ", "អី", "នៃ", "ជេង", "នារ", "ត៝ល់", "ព្វាន់", "។" }));

			Assert.That(tokenizer.Tokenize("ไป\u200bไหน\u200bมา?  เขา\u200bถาม\u200bผม."),
				Is.EqualTo(new[] { "ไป", "ไหน", "มา", "?", "เขา", "ถาม", "ผม", "." }));
		}
	}
}
