#region Copyright (C) Craig Anthony Dean 2016 - All Rights Reserved

// Copyright (C) Craig Anthony Dean 2016 - All Rights Reserved
// Unauthorized copying of this file, via any medium is strictly prohibited
// Proprietary and confidential
// Written by Craig Anthony Dean<thargy@yahoo.com>

#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Apocraphy.Assets.Scripts.Threading
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
            /// The name of this scheduler.
            /// </summary>
            private readonly string Name;

            /// <summary>
            /// Initializes a new instance of the <see cref="QueueScheduler"/> class.
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
            /// Returns a <see cref="System.String" /> that represents this instance.
            /// </summary>
            /// <returns>A <see cref="System.String" /> that represents this instance.</returns>
            public override string ToString()
            {
                return Name;
            }
        }
    }
}