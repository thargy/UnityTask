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

using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace Thargy.UnityTask
{
    /// <summary>
    ///     The TaskManager allows scheduling of <see cref="Task">tasks</see> on the Unity UI Thread.
    /// </summary>
    /// <seealso cref="ITaskScheduler" />
    /// <seealso cref="ThreadPoolScheduler" />
    /// <seealso cref="ThreadScheduler" />
    [UsedImplicitly]
    public partial class TaskManager : MonoBehaviour
    {
        /// <summary>
        ///     The singleton instance.
        /// </summary>
        private static TaskManager _instance;

        private static long _time;
        private static int _unityThread;

        /// <summary>
        ///     Will schedule <see cref="Task">tasks</see> to run during immediately.
        /// </summary>
        /// <value>The immediate scheduler.</value>
        [NotNull]
        public static readonly ITaskScheduler Immediate = new ImmediateScheduler();

        /// <summary>
        ///     Will schedule <see cref="Task">tasks</see> to run on the <see cref="System.Threading.ThreadPool">thread pool</see>.
        /// </summary>
        /// <value>The thread pool scheduler.</value>
        [NotNull]
        public static readonly ITaskScheduler ThreadPool = new ThreadScheduler();

        /// <summary>
        ///     Will schedule <see cref="Task">tasks</see> to run on a dedicated <see cref="Thread">thread</see>.
        /// </summary>
        /// <value>The thread scheduler.</value>
        [NotNull]
        public static readonly ITaskScheduler Thread = new ThreadScheduler();

        /// <summary>
        ///     Maximum time to spend executing actions during a fixed update.
        /// </summary>
        [Header("Batch Durations (ms)")]
        [Tooltip("Maximum time to spend executing actions during a fixed update, once exceeded no more actions will" +
                 "be run by the scheduler until the next Fixed Update.")]
        [Range(1, 100)]
        [UsedImplicitly]
        public int FixedUpdateBatchDurationMs = 4;

        /// <summary>
        ///     Maximum time to spend executing actions during an update.
        /// </summary>
        [Tooltip("Maximum time to spend executing actions during an update, once exceeded no more actions will" +
                 "be run by the scheduler until the next Fixed Update.")]
        [Range(1, 100)]
        [UsedImplicitly]
        public int UpdateBatchDurationMs = 8;

        /// <summary>
        ///     Maximum time to spend executing actions during a late update.
        /// </summary>
        [Tooltip("Maximum time to spend executing actions during a late update, once exceeded no more actions will" +
                 "be run by the scheduler until the next Fixed Update.")]
        [Range(1, 100)]
        [UsedImplicitly]
        public int LateUpdateBatchDurationMs = 4;


        /// <summary>
        ///     Called when the editor validates parameters for the <see cref="MonoBehaviour" />.
        /// </summary>
        private void OnValidate()
        {
            FixedUpdateBatchDurationMs = Mathf.Clamp(FixedUpdateBatchDurationMs, 1, 100);
            UpdateBatchDurationMs = Mathf.Clamp(UpdateBatchDurationMs, 1, 100);
            LateUpdateBatchDurationMs = Mathf.Clamp(LateUpdateBatchDurationMs, 1, 100);
        }

        /// <summary>
        ///     Prevents any instance of the <see cref="TaskManager" /> class from being created programmatically.
        /// </summary>
        private TaskManager()
        {
        }

        /// <summary>
        ///     The <see cref="System.Threading.Thread.ManagedThreadId">unity thread id</see>.
        /// </summary>
        public static int UnityThread
        {
            get { return _unityThread; }
        }

        /// <summary>
        ///     Gets a value indicating whether this instance is the Unity UI thread.
        /// </summary>
        /// <value><c>true</c> if this instance is Unity UI thread; otherwise, <c>false</c>.</value>
        public static bool IsUnityThread
        {
            get { return System.Threading.Thread.CurrentThread.ManagedThreadId == _unityThread; }
        }

        /// <summary>
        ///     Gets the current game time in milliseconds.
        /// </summary>
        /// <value>The time.</value>
        public static long Time
        {
            get { return Interlocked.Read(ref _time); }
            private set { Interlocked.Exchange(ref _time, value); }
        }

        /// <summary>
        ///     Will schedule <see cref="Task">tasks</see> to run during FixedUpdate.
        /// </summary>
        /// <value>The on fixed update scheduler.</value>
        public static ITaskScheduler OnFixedUpdate
        {
            get { return _onFixedUpdate; }
        }

        /// <summary>
        ///     Will schedule <see cref="Task">tasks</see> to run during FixedUpdate.
        /// </summary>
        /// <value>The on update scheduler.</value>
        public static ITaskScheduler OnUpdate
        {
            get { return _onUpdate; }
        }

        /// <summary>
        ///     Will schedule <see cref="Task">tasks</see> to run during LastUpdate.
        /// </summary>
        /// <value>The on late update scheduler.</value>
        public static ITaskScheduler OnLateUpdate
        {
            get { return _onLateUpdate; }
        }

        /// <summary>
        ///     Will schedule <see cref="Task">tasks</see> to run no sooner than the specified <paramref name="delay" />.
        /// </summary>
        /// <param name="delay">The delay.</param>
        /// <param name="scheduler">The scheduler to run the task on after the delay.</param>
        /// <returns>The delay scheduler.</returns>
        /// <exception cref="ArgumentOutOfRangeException">delay</exception>
        public static ITaskScheduler AfterDelay(TimeSpan delay, ITaskScheduler scheduler = null)
        {
            long millisecondsDelay = (long) delay.TotalMilliseconds;
            if (millisecondsDelay < 1)
                throw new ArgumentOutOfRangeException("delay");

            return new DelayScheduler(millisecondsDelay, scheduler);
        }

        /// <summary>
        ///     Will schedule <see cref="Task">tasks</see> to run no sooner than the specified
        ///     <paramref name="millisecondsDelay">delay</paramref>.
        /// </summary>
        /// <param name="millisecondsDelay">The milliseconds delay.</param>
        /// <param name="scheduler">The scheduler to run the task on after the delay.</param>
        /// <returns>The delay scheduler.</returns>
        /// <exception cref="ArgumentOutOfRangeException">delay</exception>
        public static ITaskScheduler AfterDelay(long millisecondsDelay, ITaskScheduler scheduler = null)
        {
            if (millisecondsDelay < 1)
                throw new ArgumentOutOfRangeException("millisecondsDelay");

            return new DelayScheduler(millisecondsDelay, scheduler);
        }

        private void FixedUpdate()
        {
            Time = (long) (1000*UnityEngine.Time.time);
            _onFixedUpdate.Execute(FixedUpdateBatchDurationMs);

            // Process delayed actions
            if (_delayedActions.Count < 1)
                return;

            // Calculate which delayed actions are due
            List<DelayedAction> actions;
            lock (_delayedActions)
            {
                actions = new List<DelayedAction>(_delayedActions.Count);
                LinkedListNode<DelayedAction> node = _delayedActions.First;
                while (node != null)
                {
                    LinkedListNode<DelayedAction> nextNode = node.Next;

                    DelayedAction delayedAction = node.Value;
                    if (delayedAction.ScheduleAfter <= Time)
                    {
                        _delayedActions.Remove(node);
                        actions.Add(delayedAction);
                    }
                    else
                    // As the linked list is always sorted we can bail early as we won't find any more actions that are scheduled.
                        break;

                    node = nextNode;
                }
            }

            // Schedule actions
            foreach (DelayedAction action in actions)
                action.Schedule(true);
        }

        private void Update()
        {
            Time = (long) (1000*UnityEngine.Time.time);
            _onUpdate.Execute(UpdateBatchDurationMs);
        }

        private void LateUpdate()
        {
            Time = (long) (1000*UnityEngine.Time.time);
            _onLateUpdate.Execute(LateUpdateBatchDurationMs);
        }

        private void Awake()
        {
            Time = (long) (1000*UnityEngine.Time.time);
            _unityThread = System.Threading.Thread.CurrentThread.ManagedThreadId;
            if (_instance == null)
                _instance = this;
            else
                throw new InvalidOperationException(
                    "Cannot instantiate the TaskManager MonoBehaviour more than once in any engine!");
        }
    }
}