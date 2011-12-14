using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HTM
{
	public class AlsingClassifier : IClassifier
	{
		Dictionary<string, Class> classes = new Dictionary<string, Class>();

		public void Train(string identifier, bool[] values)
		{
			Class c;
			if (!classes.TryGetValue(identifier, out c)) classes.Add(identifier, c = new Class());
			c.trainedValues.Add(values.ToArray());
		}

		public IEnumerable<ClassifierResult> FindMatches(bool[] values)
		{
			var results = 
				classes.Select(c => new ClassifierResult
				{
					Identifier = c.Key,
					Strength = c.Value.Match(values)
				})
				.ToList();

			var sum = results.Sum(r => r.Strength);
			foreach (var r in results) r.Strength /= sum;

			return results;
		}

		class Class
		{
			public List<bool[]> trainedValues = new List<bool[]>();
			
			public float Match(bool[] values)
			{
				float bestMatch = 0;
				foreach (var trained in trainedValues)
				{
					float match = 0;
					for (int i = 0, l = Math.Min(values.Length, trained.Length); i < l; i++)
						if (trained[i] == values[i])
							match++;
					if (match > bestMatch) bestMatch = match;
				}
				return bestMatch;
			}
		}
	}
}
