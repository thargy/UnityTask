#region Copyright (C) Craig Anthony Dean 2016 - All Rights Reserved

// Copyright (C) Craig Anthony Dean 2016 - All Rights Reserved
// Unauthorized copying of this file, via any medium is strictly prohibited
// Proprietary and confidential
// Written by Craig Anthony Dean<thargy@yahoo.com>

#endregion

using System;
using System.Threading;

namespace Apocraphy.Assets.Scripts.Threading
{
    public partial class TaskManager
    {
        /// <summary>
        /// The ThreadScheduler schedules <see cref="Task">tasks</see> to run in their own threads.
        /// </summary>
        /// <remarks><para>This should be used for long running operations only; otherwise consider <see cref="ThreadPoolScheduler"/>.</para>
        /// <para>Any interactions with the Unity framework should be scheduled to run using the <see cref="TaskManager"/>.</para></remarks>
        /// <seealso cref="ITaskScheduler" />
        private class ThreadScheduler : ITaskScheduler
        {
            /// <summary>
            /// Schedules the specified action.
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