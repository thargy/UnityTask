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

namespace UnityTask.Test
{
    [TestClass]
    public class TestBase
    {
        [NotNull]
        private static readonly Type[] _emptyTypes = new Type[0];

        /// <summary>
        /// The <see cref="TaskManager"/>.
        /// </summary>
        [NotNull]
        protected static readonly TaskManager Manager;

        /// <summary>
        /// The unity thread.
        /// </summary>
        [NotNull]
        private static readonly Thread _unityThread;

        [NotNull]
        private static readonly System.Threading.CancellationTokenSource _unityThreadCancellationTokenSource = new System.Threading.CancellationTokenSource();
        
        [NotNull]
        private static readonly ConcurrentQueue<Action> _actions = new ConcurrentQueue<Action>();

        [UsedImplicitly]
        public static int UnityThreadID { get; private set; }

        /// <summary>
        /// The test stopwatch.
        /// </summary>
        [NotNull] [UsedImplicitly] protected static readonly Stopwatch Stopwatch;

        [NotNull] private static readonly Action _awakeAction;
        [NotNull] private static readonly Action _fixedUpdateAction;
        [NotNull] private static readonly Action _updateAction;
        [NotNull] private static readonly Action _lateUpdateAction;
        [NotNull] private static readonly Action _onValidateAction;

        /// <summary>
        /// Gets an action that will call a <param name="name">Behaviour's method</param> on the specified 
        /// <paramref name="instance"/>.
        /// </summary>
        /// <param name="instance">The instance.</param>
        /// <param name="name">The method name.</param>
        /// <returns>Action.</returns>
        private static Action GetAction(TaskManager instance, string name)
        {
            MethodInfo method = typeof (TaskManager).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(method);

            return Expression.Lambda<Action>(Expression.Call(Expression.Constant(instance), method)).Compile();
        }

        /// <summary>
        /// Gets a value indicating whether the current thread is the unity thread.
        /// </summary>
        /// <value><see langword="true" /> if this thread is the unity thread; otherwise, <see langword="false" />.</value>
        public static bool IsUnityThread => Thread.CurrentThread.ManagedThreadId == UnityThreadID;

        static TestBase()
        {
            // Create instance of TaskManager
            Manager = typeof(TaskManager)
                .GetConstructor(
                    BindingFlags.CreateInstance | BindingFlags.NonPublic | BindingFlags.Instance |
                    BindingFlags.OptionalParamBinding,
                    null,
                    _emptyTypes,
                    null)
                .Invoke(null) as TaskManager;
            Assert.IsNotNull(Manager);

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
        /// The unity thread simulator
        /// </summary>
        private static void Start()
        {
            // Start the stopwatch (for game time simulation)
            Stopwatch.Start();

            // This will be the simulated Unity Thread, store it's ID.
            UnityThreadID = Thread.CurrentThread.ManagedThreadId;

            // Run awake only once, setting the time first
            Time.time = Stopwatch.ElapsedMilliseconds / 1000f;
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

        [AssemblyInitialize]
        public static void AssemblyInitialize(TestContext context)
        {
            _unityThread.Start();
        }

        /// <summary>
        /// Kills the unity thread once all tests have completed.
        /// </summary>
        [AssemblyCleanup]
        public static void AssemblyCleanup()
        {
            _unityThreadCancellationTokenSource.Cancel();
            Thread.Yield();
            _unityThread.Abort();

            Assert.IsTrue(_actions.IsEmpty);
        }

        [UsedImplicitly]
        [NotNull]
        protected System.Threading.Tasks.Task Invoke(
            Action action,
            float? time = null,
            System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken))
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
                    Time.time = time ?? Stopwatch.ElapsedMilliseconds / 1000f;
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

        [UsedImplicitly]
        [NotNull]
        protected System.Threading.Tasks.Task FixedUpdate(
            float? time = null,
            System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken))
            => Invoke(_fixedUpdateAction, time, cancellationToken);

        [UsedImplicitly]
        [NotNull]
        protected System.Threading.Tasks.Task Update(
            float? time = null,
            System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) 
            => Invoke(_updateAction, time, cancellationToken);

        [UsedImplicitly]
        [NotNull]
        protected System.Threading.Tasks.Task LateUpdate(
            float? time = null,
            System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken))
            => Invoke(_lateUpdateAction, time, cancellationToken);

        [UsedImplicitly]
        [NotNull]
        protected System.Threading.Tasks.Task OnValidate(
            float? time = null,
            System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken))
            => Invoke(_onValidateAction, time, cancellationToken);
    }
}
