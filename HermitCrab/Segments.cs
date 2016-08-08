using SIL.Machine.Annotations;

namespace SIL.HermitCrab
{
	public class Segments
	{
		private readonly string _representation;
		private readonly CharacterDefinitionTable _table;
		private readonly Shape _shape;

		public Segments(CharacterDefinitionTable table, string representation)
			: this(table, representation, table.Segment(representation))
		{
		}

		public Segments(CharacterDefinitionTable table, string representation, Shape shape)
		{
			_representation = representation;
			_table = table;
			_shape = shape;
			_shape.Freeze();
		}

		public string Representation
		{
			get { return _representation; }
		}

		public CharacterDefinitionTable CharacterDefinitionTable
		{
			get { return _table; }
		}

		public Shape Shape
		{
			get { return _shape; }
		}

		public override string ToString()
		{
			return _representation;
		}
	}
}
