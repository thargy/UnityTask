#region Copyright (C) Craig Anthony Dean 2016 - All Rights Reserved

// Copyright (C) Craig Anthony Dean 2016 - All Rights Reserved
// Unauthorized copying of this file, via any medium is strictly prohibited
// Proprietary and confidential
// Written by Craig Anthony Dean<thargy@yahoo.com>

#endregion

using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace Apocraphy.Assets.Scripts.Threading
{
    /// <summary>
    ///     The TaskManager allows scheduling of <see cref="Task">tasks</see> on the Unity UI Thread.
    /// </summary>
    /// <seealso cref="ITaskScheduler"/>
    /// <seealso cref="ThreadPoolScheduler"/>
    /// <seealso cref="ThreadScheduler"/>
    public partial class TaskManager : MonoBehaviour
    {
        /// <summary>
        /// The singleton instance.
        /// </summary>
        private static TaskManager _instance;

        private static long _time;
        private static int _unityThread;

        /// <summary>
        /// The <see cref="System.Threading.Thread.ManagedThreadId">unity thread id</see>.
        /// </summary>
        public static int UnityThread
        {
            get { return _unityThread; }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is the Unity UI thread.
        /// </summary>
        /// <value><c>true</c> if this instance is Unity UI thread; otherwise, <c>false</c>.</value>
        public static bool IsUnityThread
        {
            get { return System.Threading.Thread.CurrentThread.ManagedThreadId == _unityThread; }
        }

        /// <summary>
        /// Gets the current game time in milliseconds.
        /// </summary>
        /// <value>The time.</value>
        public static long Time
        {
            get { return Interlocked.Read(ref _time); }
            private set { Interlocked.Exchange(ref _time, value); }
        }

        /// <summary>
        /// Prevents any instance of the <see cref="TaskManager"/> class from being created programmatically.
        /// </summary>
        private TaskManager()
        {   
        }

        /// <summary>
        ///     Maximum time to spend executing actions during a fixed update.
        /// </summary>
        public int FixedUpdateBatchDurationMs = 4;

        /// <summary>
        ///     Maximum time to spend executing actions during a late update.
        /// </summary>
        public int LateUpdateBatchDurationMs = 4;

        /// <summary>
        ///     Maximum time to spend executing actions during an update.
        /// </summary>
        public int UpdateBatchDurationMs = 8;

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
        /// Will schedule <see cref="Task">tasks</see> to run during immediately.
        /// </summary>
        /// <value>The immediate scheduler.</value>
        public static readonly ITaskScheduler Immediate = new ImmediateScheduler();

        /// <summary>
        /// Will schedule <see cref="Task">tasks</see> to run on the <see cref="System.Threading.ThreadPool">thread pool</see>.
        /// </summary>
        /// <value>The thread pool scheduler.</value>
        public static readonly ITaskScheduler ThreadPool = new ThreadScheduler();

        /// <summary>
        /// Will schedule <see cref="Task">tasks</see> to run on a dedicated <see cref="Thread">thread</see>.
        /// </summary>
        /// <value>The thread scheduler.</value>
        public static readonly ITaskScheduler Thread = new ThreadScheduler();

        /// <summary>
        /// Will schedule <see cref="Task">tasks</see> to run no sooner than the specified <paramref name="delay"/>.
        /// </summary>
        /// <param name="delay">The delay.</param>
        /// <param name="scheduler">The scheduler to run the task on after the delay.</param>
        /// <returns>The delay scheduler.</returns>
        /// <exception cref="ArgumentOutOfRangeException">delay</exception>
        public static ITaskScheduler AfterDelay(TimeSpan delay, ITaskScheduler scheduler = null)
        {
            long millisecondsDelay = (long)delay.TotalMilliseconds;
            if (millisecondsDelay < 1)
                throw new ArgumentOutOfRangeException("delay");

            return new DelayScheduler(millisecondsDelay, scheduler);
        }

        /// <summary>
        /// Will schedule <see cref="Task">tasks</see> to run no sooner than the specified <paramref name="millisecondsDelay">delay</paramref>.
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

        /// <summary>
        /// Called when the editor validates parameters for the <see cref="MonoBehaviour"/>.
        /// </summary>
        private void OnValidate()
        {
            FixedUpdateBatchDurationMs = Mathf.Clamp(FixedUpdateBatchDurationMs, 1, 100);
            UpdateBatchDurationMs = Mathf.Clamp(UpdateBatchDurationMs, 1, 100);
            LateUpdateBatchDurationMs = Mathf.Clamp(LateUpdateBatchDurationMs, 1, 100);
        }

        private void FixedUpdate()
        {
            Time = (long)(1000 * UnityEngine.Time.time);
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
            Time = (long)(1000 * UnityEngine.Time.time);
            _onUpdate.Execute(UpdateBatchDurationMs);
        }

        private void LateUpdate()
        {
            Time = (long)(1000 * UnityEngine.Time.time);
            _onLateUpdate.Execute(LateUpdateBatchDurationMs);
        }

        private void Awake()
        {
            Time = (long)(1000 * UnityEngine.Time.time);
            _unityThread = System.Threading.Thread.CurrentThread.ManagedThreadId;
            if (_instance == null)
                _instance = this;
            else
                throw new InvalidOperationException("Cannot instantiate the TaskManager MonoBehaviour more than once in any engine!");
        }
    }
}