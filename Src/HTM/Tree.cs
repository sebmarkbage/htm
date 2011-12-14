using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HTM
{
	public class Tree : IEnumerable<bool>
	{
		private Level[] levels;

		public Tree(int[] levels, int inputCount, int columnCountPerNode, int cellsPerColumn, int desiredLocalActivity, int minOverlap, int maxSynapsesPerColumn, float connectedPerm = 0.2f, float permanenceInc = 0.01f, float permanenceDec = 0.01f, int slidingWindowLength = 1000, int activationThreshold = 1, int minThreshold = 1, int learningRadius = 10, int newSynapseCount = 10)
		{
			var lvls = new Level[levels.Length];
			for (var i = 0; i < levels.Length; i++)
			{
				var nodesInLevel = levels[i];

				if (inputCount % nodesInLevel != 0) throw new ArgumentOutOfRangeException("inputCount");

				var nodes = new Node[nodesInLevel];
				for (var n = 0; n < nodesInLevel; n++)
					nodes[n] = new Node(inputCount / nodesInLevel, columnCountPerNode, cellsPerColumn, desiredLocalActivity, minOverlap, maxSynapsesPerColumn, connectedPerm, permanenceInc, permanenceDec, slidingWindowLength, activationThreshold, minThreshold, learningRadius, newSynapseCount);

				inputCount = columnCountPerNode * cellsPerColumn * nodesInLevel;

				lvls[i] = new Level(nodes);
			}

			this.levels = lvls;
		}

		public Tree(params Level[] levels)
		{
			this.levels = levels;
		}

		public bool this[int index]
		{
			get
			{
				return levels[levels.Length - 1][index];
			}
		}

		public bool Learning
		{
			get
			{
				return levels.Any(n => n.Learning);
			}
			set
			{
				foreach (var n in levels)
					n.Learning = value;
			}
		}

		public void Feed(Func<int, bool> input)
		{
			Level previousLevel;
			foreach (var level in levels)
			{
				level.Feed(input);
				previousLevel = level;
				input = i => previousLevel[i];
			}
		}

		public IEnumerator<bool> GetEnumerator()
		{
			return levels[levels.Length - 1].GetEnumerator();
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}
