#region Copyright (C) Craig Anthony Dean 2016 - All Rights Reserved

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
using System.Collections.Generic;
using System.Diagnostics;

namespace Thargy.UnityTask
{
    public partial class TaskManager
    {
        private static readonly QueueScheduler _onFixedUpdate = new QueueScheduler("Fixed Update Scheduler");
        private static readonly QueueScheduler _onUpdate = new QueueScheduler("Update Scheduler");
        private static readonly QueueScheduler _onLateUpdate = new QueueScheduler("Late Update Scheduler");

        /// <summary>
        ///     Holds individual queues for the <see cref="TaskManager" /> to distinguish between the update methods to run
        ///     actions on.
        /// </summary>
        /// <seealso cref="ITaskScheduler" />
        private class QueueScheduler : ITaskScheduler
        {
            /// <summary>
            ///     The actions to run on this scheduler.
            /// </summary>
            private readonly Queue<Action> _actions = new Queue<Action>();

            /// <summary>
            ///     The name of this scheduler.
            /// </summary>
            private readonly string Name;

            /// <summary>
            ///     Initializes a new instance of the <see cref="QueueScheduler" /> class.
            /// </summary>
            /// <param name="name">The name.</param>
            public QueueScheduler(string name)
            {
                Name = name;
            }

            /// <summary>
            ///     Schedules the specified action.
            /// </summary>
            /// <param name="action">The action.</param>
            void ITaskScheduler.Schedule(Action action)
            {
                lock (_actions)
                    _actions.Enqueue(action);
            }

            /// <summary>
            ///     Executes as many actions as possible before the remaining time exits.
            /// </summary>
            /// <param name="remaining">The remaining.</param>
            public void Execute(int remaining)
            {
                // Shortcut - bailout if no actions pending, or we are waiting for a particular time-stamp.
                if (_actions.Count < 1)
                    return;

                Stopwatch stopwatch = Stopwatch.StartNew();
                do
                {
                    Action action;

                    // Get next action
                    lock (_actions)
                    {
                        if (_actions.Count < 1)
                            break;

                        action = _actions.Dequeue();
                    }

                    // Execute action - note actions, should never throw exceptions, as task system already wraps!
                    action();
                }
                while (stopwatch.ElapsedMilliseconds < remaining);
                stopwatch.Stop();
            }

            /// <summary>
            ///     Returns a <see cref="System.String" /> that represents this instance.
            /// </summary>
            /// <returns>A <see cref="System.String" /> that represents this instance.</returns>
            public override string ToString()
            {
                return Name;
            }
        }
    }
}