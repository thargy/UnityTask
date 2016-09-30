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

using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Thargy.UnityTask;
using UnityEngine;
using CancellationToken = System.Threading.CancellationToken;
using CancellationTokenSource = System.Threading.CancellationTokenSource;
using Task = System.Threading.Tasks.Task;

namespace UnityTask.Test
{
    /// <summary>
    ///     A base class for all tests, that initializes a <see cref="TaskManager" /> and allows simulation of the Unity
    ///     UI Thread behaviour.
    /// </summary>
    [TestClass]
    public class TestBase
    {
        /// <summary>
        ///     The <see cref="TaskManager" /> instance.
        /// </summary>
        [UsedImplicitly] [NotNull] protected static readonly TaskManager Manager;

        /// <summary>
        ///     The unity thread.
        /// </summary>
        [NotNull] private static readonly Thread _unityThread;

        /// <summary>
        ///     The unity thread simulator cancellation token source, allows for canceling the simulation.
        /// </summary>
        [NotNull] private static readonly CancellationTokenSource _unityThreadCancellationTokenSource =
            new CancellationTokenSource();

        /// <summary>
        ///     The actions that are queued to run on the unity thread.
        /// </summary>
        [NotNull] private static readonly ConcurrentQueue<Action> _actions = new ConcurrentQueue<Action>();

        /// <summary>
        ///     The test stopwatch.
        /// </summary>
        [NotNull] [UsedImplicitly] protected static readonly Stopwatch Stopwatch;

        /// <summary>
        ///     The awake action.
        /// </summary>
        [NotNull] private static readonly Action _awakeAction;

        /// <summary>
        ///     The fixed update action.
        /// </summary>
        [NotNull] private static readonly Action _fixedUpdateAction;

        /// <summary>
        ///     The update action.
        /// </summary>
        [NotNull] private static readonly Action _updateAction;

        /// <summary>
        ///     The late update action.
        /// </summary>
        [NotNull] private static readonly Action _lateUpdateAction;

        /// <summary>
        ///     The on validate action.
        /// </summary>
        [NotNull] private static readonly Action _onValidateAction;

        /// <summary>
        ///     Initializes static members of the <see cref="TestBase" /> class.
        /// </summary>
        static TestBase()
        {
            // Create instance of TaskManager
            // ReSharper disable AssignNullToNotNullAttribute
            Manager = typeof (TaskManager)
                .GetConstructor(
                    BindingFlags.CreateInstance | BindingFlags.NonPublic | BindingFlags.Instance |
                    BindingFlags.OptionalParamBinding,
                    null,
                    new Type[0],
                    null)
                .Invoke(null) as TaskManager;
            Assert.IsNotNull(Manager);
            // ReSharper restore AssignNullToNotNullAttribute

            _awakeAction = GetAction(Manager, "Awake");
            Assert.IsNotNull(_awakeAction);
            _fixedUpdateAction = GetAction(Manager, "FixedUpdate");
            Assert.IsNotNull(_fixedUpdateAction);
            _updateAction = GetAction(Manager, "Update");
            Assert.IsNotNull(_updateAction);
            _lateUpdateAction = GetAction(Manager, "LateUpdate");
            Assert.IsNotNull(_lateUpdateAction);
            _onValidateAction = GetAction(Manager, "OnValidate");
            Assert.IsNotNull(_onValidateAction);

            _unityThread = new Thread(Start);
            Stopwatch = new Stopwatch();
        }

        /// <summary>
        ///     Gets the unity thread <see cref="Thread.ManagedThreadId">identifier</see>.
        /// </summary>
        /// <value>The unity thread identifier.</value>
        [UsedImplicitly]
        public static int UnityThreadID { get; private set; }

        /// <summary>
        ///     Gets a value indicating whether the current thread is the unity thread.
        /// </summary>
        /// <value><see langword="true" /> if this thread is the unity thread; otherwise, <see langword="false" />.</value>
        [UsedImplicitly]
        protected static bool IsUnityThread => Thread.CurrentThread.ManagedThreadId == UnityThreadID;

        /// <summary>
        ///     Gets an action that will call a
        ///     <param name="name">Behaviour's method</param>
        ///     on the specified
        ///     <paramref name="instance" />.
        /// </summary>
        /// <param name="instance">The instance.</param>
        /// <param name="name">The method name.</param>
        /// <returns>An <see cref="Action" />.</returns>
        private static Action GetAction(TaskManager instance, string name)
        {
            MethodInfo method = typeof (TaskManager).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(method);

            return Expression.Lambda<Action>(Expression.Call(Expression.Constant(instance), method)).Compile();
        }

        /// <summary>
        ///     The unity thread simulator.
        /// </summary>
        private static void Start()
        {
            // Start the stopwatch (for game time simulation)
            Stopwatch.Start();

            // This will be the simulated Unity Thread, store it's ID.
            UnityThreadID = Thread.CurrentThread.ManagedThreadId;

            // Run Awake() only once, setting the time first, this will initialise the TaskManager
            Time.time = Stopwatch.ElapsedMilliseconds/1000f;
            _awakeAction();

            // Loop until aborted
            while (true)
            {
                if (_unityThreadCancellationTokenSource.IsCancellationRequested)
                    return;
                // Action loop
                Action action;
                while (_actions.TryDequeue(out action))
                {
                    if (_unityThreadCancellationTokenSource.IsCancellationRequested)
                        return;
                    action();
                    Thread.Yield();
                }

                if (_unityThreadCancellationTokenSource.IsCancellationRequested)
                    return;
                Thread.Sleep(10);
            }
        }

        /// <summary>
        ///     Run once when test run starts.
        /// </summary>
        /// <param name="context">The context.</param>
        [AssemblyInitialize]
        public static void AssemblyInitialize(TestContext context)
        {
            // Start simulating the unity UI thread.
            _unityThread.Start();
        }

        /// <summary>
        ///     Kills the unity thread once all tests have completed.
        /// </summary>
        [AssemblyCleanup]
        public static void AssemblyCleanup()
        {
            _unityThreadCancellationTokenSource.Cancel();
            Thread.Yield();
            _unityThread.Abort();

            Assert.IsTrue(_actions.IsEmpty);
        }

        /// <summary>
        ///     Invokes the specified action on the Unity thread.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <param name="time">The optional time.</param>
        /// <param name="cancellationToken">The optional cancellation token.</param>
        /// <returns>An awaitable <see cref="System.Threading.Tasks.Task" />.</returns>
        [UsedImplicitly]
        [NotNull]
        protected Task Invoke(
            Action action,
            float? time = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
            if (cancellationToken.IsCancellationRequested)
            {
                tcs.SetCanceled();
                return tcs.Task;
            }

            Action wrappedAction = () =>
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    tcs.SetCanceled();
                    return;
                }

                try
                {
                    Time.time = time ?? Stopwatch.ElapsedMilliseconds/1000f;
                    action();
                    if (cancellationToken.IsCancellationRequested)
                        tcs.SetCanceled();
                    else
                        tcs.SetResult(true);
                }
                catch (Exception e)
                {
                    tcs.SetException(e);
                }
            };

            // Run immediately if on unity thread, otherwise queue to run on unity thread
            if (IsUnityThread)
                wrappedAction();
            else
                _actions.Enqueue(wrappedAction);

            return tcs.Task;
        }

        /// <summary>
        ///     Invokes the <see cref="TaskManager">TaskManager's</see> <see cref="TaskManager.FixedUpdate" /> method.
        /// </summary>
        /// <param name="time">The optional time.</param>
        /// <param name="cancellationToken">The optional cancellation token.</param>
        /// <returns>An awaitable <see cref="System.Threading.Tasks.Task" />.</returns>
        [UsedImplicitly]
        [NotNull]
        protected Task FixedUpdate(
            float? time = null,
            CancellationToken cancellationToken = default(CancellationToken))
            => Invoke(_fixedUpdateAction, time, cancellationToken);

        /// <summary>
        ///     Invokes the <see cref="TaskManager">TaskManager's</see> <see cref="TaskManager.Update" /> method.
        /// </summary>
        /// <param name="time">The optional time.</param>
        /// <param name="cancellationToken">The optional cancellation token.</param>
        /// <returns>An awaitable <see cref="System.Threading.Tasks.Task" />.</returns>
        [UsedImplicitly]
        [NotNull]
        protected Task Update(
            float? time = null,
            CancellationToken cancellationToken = default(CancellationToken))
            => Invoke(_updateAction, time, cancellationToken);

        /// <summary>
        ///     Invokes the <see cref="TaskManager">TaskManager's</see> <see cref="TaskManager.LateUpdate" /> method.
        /// </summary>
        /// <param name="time">The optional time.</param>
        /// <param name="cancellationToken">The optional cancellation token.</param>
        /// <returns>An awaitable <see cref="System.Threading.Tasks.Task" />.</returns>
        [UsedImplicitly]
        [NotNull]
        protected Task LateUpdate(
            float? time = null,
            CancellationToken cancellationToken = default(CancellationToken))
            => Invoke(_lateUpdateAction, time, cancellationToken);

        /// <summary>
        ///     Invokes the <see cref="TaskManager">TaskManager's</see> <see cref="TaskManager.OnValidate" /> method.
        /// </summary>
        /// <param name="time">The optional time.</param>
        /// <param name="cancellationToken">The optional cancellation token.</param>
        /// <returns>An awaitable <see cref="System.Threading.Tasks.Task" />.</returns>
        [UsedImplicitly]
        [NotNull]
        protected Task OnValidate(
            float? time = null,
            CancellationToken cancellationToken = default(CancellationToken))
            => Invoke(_onValidateAction, time, cancellationToken);
    }
}