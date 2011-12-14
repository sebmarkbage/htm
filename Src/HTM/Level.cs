using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HTM
{
	public class Level : IEnumerable<bool>
	{
		private Node[] nodes;

		public Level(params Node[] nodes)
		{
			this.nodes = nodes;
		}

		public bool this[int index]
		{
			get
			{
				int offset = 0;
				foreach (var node in nodes)
				{
					int count = node.Count;
					if (index - offset < count) return node[index - offset];
					offset += count;
				}
				throw new IndexOutOfRangeException();
			}
		}

		public bool Learning
		{
			get
			{
				return nodes.Any(n => n.Learning);
			}
			set
			{
				foreach (var n in nodes)
					n.Learning = value;
			}
		}

		public void Feed(Func<int, bool> input)
		{
			int offset = 0;
			foreach (var node in nodes)
			{
				node.Feed(i => input(offset + i));
				offset += node.InputCount;
			}
		}

		public IEnumerator<bool> GetEnumerator()
		{
			var offset = 0;
			foreach (var node in nodes)
			{
				int l = node.Count;
				for (int i = 0; i < l; i++)
					yield return node[i];
				offset += l;
			}
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}
