using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnityTask.Test
{
    [TestClass]
    public class ManualTasks : TestBase
    {
        [TestMethod]
        public void TestComplete()
        {
            Thread.Sleep(1);
        }

    }
}
