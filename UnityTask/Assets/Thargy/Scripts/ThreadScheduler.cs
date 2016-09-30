#region Copyright (C) Craig Anthony Dean 2016

// Copyright (C) Craig Anthony Dean 2016
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
// 
// Written by Craig Anthony Dean <support@thargy.com>

#endregion

using System;
using System.Threading;

namespace Thargy.UnityTask
{
    public partial class TaskManager
    {
        /// <summary>
        ///     The ThreadScheduler schedules <see cref="Task">tasks</see> to run in their own threads.
        /// </summary>
        /// <remarks>
        ///     <para>This should be used for long running operations only; otherwise consider <see cref="ThreadPoolScheduler" />.</para>
        ///     <para>Any interactions with the Unity framework should be scheduled to run using the <see cref="TaskManager" />.</para>
        /// </remarks>
        /// <seealso cref="ITaskScheduler" />
        private class ThreadScheduler : ITaskScheduler
        {
            /// <summary>
            ///     Schedules the specified action.
            /// </summary>
            /// <param name="action">The action.</param>
            public void Schedule(Action action)
            {
                new Thread(_ => action()).Start();
            }

            public override string ToString()
            {
                return "Thread Scheduler";
            }
        }
    }
}