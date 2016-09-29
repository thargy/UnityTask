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

namespace Thargy.UnityTask
{
    public partial class TaskManager
    {
        /// <summary>
        ///     The delayed actions to run.
        /// </summary>
        private static readonly LinkedList<DelayedAction> _delayedActions = new LinkedList<DelayedAction>();

        /// <summary>
        ///     The delay scheduler allows scheduling of actions that will only run after a fixed point in Unity time.
        /// </summary>
        /// <seealso cref="ITaskScheduler" />
        private class DelayScheduler : ITaskScheduler
        {
            public readonly long MillisecondsDelay;
            public readonly ITaskScheduler Scheduler;

            public DelayScheduler(long millisecondsDelay, ITaskScheduler scheduler)
            {
                MillisecondsDelay = millisecondsDelay;
                Scheduler = scheduler ?? ThreadPool;
            }

            public void Schedule(Action action)
            {
                // Create new delayed action.
                DelayedAction delayedAction = new DelayedAction(this, action);
                long scheduleAfter = delayedAction.ScheduleAfter;

                // Sanity check, there should always be a delay in practice unless this thread was interrupted across a frame boundary.
                if (scheduleAfter > Time)
                    lock (_delayedActions)
                        if (scheduleAfter > Time)
                        {
                            if (_delayedActions.Count < 1)
                            {
                                // Just add action
                                _delayedActions.AddLast(delayedAction);
                                return;
                            }

                            // Find insertion point, to maintain order in list - this makes for fast scans during
                            // each FixedUpdate.
                            LinkedListNode<DelayedAction> node = _delayedActions.First;
                            while ((node != null) &&
                                   (scheduleAfter > node.Value.ScheduleAfter))
                                node = node.Next;

                            if (node != null)
                                _delayedActions.AddBefore(node, delayedAction);
                            else
                                _delayedActions.AddLast(delayedAction);

                            return;
                        }

                // Schedule immediately
                delayedAction.Schedule();
            }

            /// <summary>
            ///     Returns a <see cref="System.String" /> that represents this instance.
            /// </summary>
            /// <returns>A <see cref="System.String" /> that represents this instance.</returns>
            public override string ToString()
            {
                return string.Format("Delay by {0}ms Scheduler", MillisecondsDelay);
            }
        }

        /// <summary>
        ///     Holds an action scheduled to run after a delay.
        /// </summary>
        private class DelayedAction
        {
            private readonly Action _action;
            private readonly ITaskScheduler _scheduler;
            public readonly long ScheduleAfter;

            /// <summary>
            ///     Initializes a new instance of the <see cref="DelayedAction" /> class.
            /// </summary>
            /// <param name="scheduler">The scheduler.</param>
            /// <param name="action">The action.</param>
            public DelayedAction(DelayScheduler scheduler, Action action)
            {
                ScheduleAfter = Time + scheduler.MillisecondsDelay;
                _scheduler = scheduler.Scheduler;
                _action = action;
            }

            /// <summary>
            ///     Executes this instance.
            /// </summary>
            public void Schedule(bool onFixedUpdate = false)
            {
                // If the scheduler is the OnFixedUpdate scheduler and we're scheduled to run on FixedUpdate then we run immediately;
                // otherwise we schedule the action for execution.
                if (onFixedUpdate && ReferenceEquals(_scheduler, OnFixedUpdate))
                    _action();
                else
                    _scheduler.Schedule(_action);
            }

            /// <summary>
            ///     Returns a <see cref="System.String" /> that represents this instance.
            /// </summary>
            /// <returns>A <see cref="System.String" /> that represents this instance.</returns>
            public override string ToString()
            {
                return string.Format("Action to run after timestamp '{0}', on the {1}", ScheduleAfter, _scheduler);
            }
        }
    }
}