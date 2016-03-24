using System;

namespace SIL.Machine.Translation
{
	public enum MorphemeType
	{
		Stem,
		Affix
	}

	/// <summary>
	/// This class contains information about a morpheme.
	/// </summary>
	public class MorphemeInfo : IEquatable<MorphemeInfo>
	{
		private readonly string _id;
		private readonly string _category;
		private readonly string _gloss;
		private readonly MorphemeType _morphemeType;

		public MorphemeInfo(string id, string category, string gloss, MorphemeType morphemeType)
		{
			_id = id;
			_category = category;
			_gloss = gloss;
			_morphemeType = morphemeType;
		}

		/// <summary>
		/// Gets the unique identifier.
		/// </summary>
		public string Id
		{
			get { return _id; }
		}

		/// <summary>
		/// Gets the category or part of speech.
		/// </summary>
		public string Category
		{
			get { return _category; }
		}

		/// <summary>
		/// Gets the gloss.
		/// </summary>
		public string Gloss
		{
			get { return _gloss; }
		}

		/// <summary>
		/// Gets the morpheme type.
		/// </summary>
		public MorphemeType MorphemeType
		{
			get { return _morphemeType; }
		}

		public override bool Equals(object obj)
		{
			var other = obj as MorphemeInfo;
			return other != null && Equals(other);
		}

		public bool Equals(MorphemeInfo other)
		{
			return other != null && _id == other._id && _category == other._category && _gloss == other._gloss && _morphemeType == other._morphemeType;
		}

		public override int GetHashCode()
		{
			int code = 23;
			code += code * 31 + _id.GetHashCode();
			code += code * 31 + (_category == null ? 0 : _category.GetHashCode());
			code += code * 31 + _gloss.GetHashCode();
			code += code * 31 + _morphemeType.GetHashCode();
			return code;
		}

		public override string ToString()
		{
			return _id;
		}
	}
}
