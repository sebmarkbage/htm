using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HTM
{
	public class SoftmaxClassifier : IClassifier
	{
		public IEnumerable<ClassifierResult> FindMatches(bool[] values)
		{
			throw new NotImplementedException();
		}

		public void Train(string identifier, bool[] values)
		{
			throw new NotImplementedException();
		}

		private float Cost(float[][] y, float[][] yPredication)
		{
			throw new NotImplementedException();
		}
	}
}
