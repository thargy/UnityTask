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
        /// The ThreadPoolScheduler schedules <see cref="Task">tasks</see> to run on a background thread in the <see cref="ThreadPool"/>.
        /// </summary>
        /// <remarks><para>This should be used for relatively quick operations only; otherwise consider <see cref="ThreadPoolScheduler"/>.</para>
        /// <para>Any interactions with the Unity framework should be scheduled to run using the <see cref="TaskManager"/>.</para></remarks>
        /// <seealso cref="ITaskScheduler" />
        private class ThreadPoolScheduler : ITaskScheduler
        {
            /// <summary>
            /// Schedules the specified action.
            /// </summary>
            /// <param name="action">The action.</param>
            public void Schedule(Action action)
            {
                System.Threading.ThreadPool.QueueUserWorkItem(_ => action());
            }

            /// <summary>
            /// Returns a <see cref="System.String" /> that represents this instance.
            /// </summary>
            /// <returns>A <see cref="System.String" /> that represents this instance.</returns>
            public override string ToString()
            {
                return "ThreadPool Scheduler";
            }
        }
    }
}