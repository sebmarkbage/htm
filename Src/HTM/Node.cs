using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HTM
{
	public class Node : IEnumerable<bool>
	{
		readonly SpatialPooler spatialPooler;
		readonly TemporalPooler temporalPooler;

		int cellsPerColumn;
		int columnCount;
		int inputCount;

		public int Count
		{
			get { return cellsPerColumn * columnCount; }
		}

		public int InputCount
		{
			get { return inputCount; }
		}

		public bool Learning
		{
			get
			{
				return spatialPooler.Learning || temporalPooler.Learning;
			}
			set
			{
				spatialPooler.Learning = value;
				temporalPooler.Learning = value;
			}
		}

		public Node(int inputCount, int columnCount, int cellsPerColumn, int desiredLocalActivity, int minOverlap, int maxSynapsesPerColumn, float connectedPerm = 0.2f, float permanenceInc = 0.01f, float permanenceDec = 0.01f, int slidingWindowLength = 1000, int activationThreshold = 1, int minThreshold = 1, int learningRadius = 10, int newSynapseCount = 10)
		{
			spatialPooler = new SpatialPooler(
				inputCount,
				columnCount,
				desiredLocalActivity: desiredLocalActivity,
				minOverlap: minOverlap,
				maxSynapsesPerColumn: maxSynapsesPerColumn,
				connectedPerm: connectedPerm,
				permanenceInc: permanenceInc,
				permanenceDec: permanenceDec,
				slidingWindowLength: slidingWindowLength
			);

			temporalPooler = new TemporalPooler(
				columnCount,
				cellsPerColumn,
				connectedPerm: connectedPerm,
				permanenceInc: permanenceInc,
				permanenceDec: permanenceDec,
				activationThreshold: activationThreshold,
				minThreshold: minThreshold,
				learningRadius: learningRadius,
				newSynapseCount: newSynapseCount
			);

			this.inputCount = inputCount;
			this.columnCount = columnCount;
			this.cellsPerColumn = cellsPerColumn;
		}

		public bool this[int index]
		{
			get
			{
				return temporalPooler[index / cellsPerColumn, index % cellsPerColumn];
			}
		}

		public void Feed(Func<int, bool> input)
		{
			spatialPooler.Feed(input);
			temporalPooler.Feed(i => spatialPooler[i]);
		}

		public IEnumerator<bool> GetEnumerator()
		{
			for (int i = 0, l = Count; i < l; i++)
				yield return this[i];
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}
