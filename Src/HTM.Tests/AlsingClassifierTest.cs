using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HTM.Tests
{
	[TestClass]
	public class AlsingClassifierTest : Classifier
	{
		protected override IClassifier Create()
		{
			return new HTM.AlsingClassifier();
		}
	}
}
