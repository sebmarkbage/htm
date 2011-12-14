using System;
namespace HTM
{
	public interface IClassifier
	{
		System.Collections.Generic.IEnumerable<ClassifierResult> FindMatches(bool[] values);
		void Train(string identifier, bool[] values);
	}

	public class ClassifierResult
	{
		public string Identifier;
		public float Strength;
	}
}
