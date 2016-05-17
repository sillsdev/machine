using System;
using System.IO;
using System.Threading;
using NUnit.Framework;
using SIL.ObjectModel;

namespace SIL.Machine.Translation.Thot.Tests
{
	[TestFixture]
	public class ThotSmtEngineTests
	{
		[Test]
		public void TrainModels()
		{
			string tempDir = CreateTempDirectory("ThotSmtEngineTests");
			try
			{
				string cfgFileName = Path.Combine(tempDir, "test.cfg");
				File.WriteAllText(cfgFileName, "-tm tm/src_trg\n-lm lm/trg.lm\n");

				ReadOnlyList<string[]> sourceCorpus = new ReadOnlyList<string[]>(new[]
				{
					"¿ le importaría darnos las llaves de la habitación , por favor ?".Split(' '),
					"he hecho la reserva de una habitación tranquila doble con teléfono y televisión a nombre de rosario cabedo .".Split(' '),
					"¿ le importaría cambiarme a otra habitación más tranquila ?".Split(' '),
					"por favor , tengo reservada una habitación .".Split(' '),
					"me parece que existe un problema .".Split(' ')
				});
				ReadOnlyList<string[]> targetCorpus = new ReadOnlyList<string[]>(new[]
				{
					"would you mind giving us the keys to the room , please ?".Split(' '),
					"i have made a reservation for a quiet , double room with a telephone and a tv for rosario cabedo .".Split(' '),
					"would you mind moving me to a quieter room ?".Split(' '),
					"i have booked a room .".Split(' '),
					"i think that there is a problem .".Split(' ')
				});
				ThotSmtEngine.TrainModels(cfgFileName, sourceCorpus, targetCorpus);

				Assert.That(File.Exists(Path.Combine(tempDir, "lm", "trg.lm")), Is.True);
				Assert.That(File.Exists(Path.Combine(tempDir, "tm", "src_trg_swm.hmm_alignd")), Is.True);
				Assert.That(File.Exists(Path.Combine(tempDir, "tm", "src_trg_invswm.hmm_alignd")), Is.True);
				Assert.That(File.Exists(Path.Combine(tempDir, "tm", "src_trg.ttable")), Is.True);
				// TODO: test for more than just existence of files
			}
			finally
			{
				DeleteFolderThatMayBeInUse(tempDir);
			}
		}

		[Test]
		public void Translate_TranslationCorrect()
		{
			using (var engine = new ThotSmtEngine(TestHelpers.ToyCorpusConfigFileName))
			{
				TranslationResult result = engine.Translate("voy a marcharme hoy por la tarde .".Split());
				Assert.That(result.TargetSegment, Is.EqualTo("i am leaving today in the afternoon .".Split()));
			}
		}

		private static string CreateTempDirectory(string name)
		{
			string path = Path.Combine(Path.GetTempPath(), name);
			DeleteFolderThatMayBeInUse(path);
			Directory.CreateDirectory(path);
			return path;
		}

		private static void DeleteFolderThatMayBeInUse(string folder)
		{
			if (Directory.Exists(folder))
			{
				try
				{
					Directory.Delete(folder, true);
				}
				catch (Exception)
				{
					try
					{
						//maybe we can at least clear it out a bit
						string[] files = Directory.GetFiles(folder, "*.*", SearchOption.AllDirectories);
						foreach (string s in files)
						{
							File.Delete(s);
						}
						//sleep and try again (seems to work)
						Thread.Sleep(1000);
						Directory.Delete(folder, true);
					}
					catch (Exception)
					{
					}
				}
			}
		}
	}
}
