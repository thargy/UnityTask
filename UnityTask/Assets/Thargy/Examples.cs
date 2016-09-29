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
using System.Linq;
using System.Text;
using System.Threading;
using Thargy.UnityTask;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Example code for demonstrating how to use the <see cref="TaskManager"/>.
/// </summary>
public class Examples : MonoBehaviour
{
    #region Code for ensuring the behaviour is attached to a single GameObject
    /// <summary>
    /// The associated game object
    /// </summary>
    private static GameObject _gameObject;

    /// <summary>
    /// Gets the game object.
    /// </summary>
    /// <value>The game object.</value>
    /// <remarks><para>This will error if you've not actually attached the script to a <see cref="GameObject"/>.</para></remarks>
    /// <exception cref="System.NullReferenceException">You must attach the Game component to a single GameObject instance.</exception>
    [NotNull]
    public static GameObject GameObject
    {
        get
        {
            if (_gameObject == null)
                throw new NullReferenceException(
                    "You must attach the Game component to a single GameObject instance.");

            return _gameObject;
        }
    }

    /// <summary>
    /// Called once for each instance of the object, we will use to find the attached <see cref="GameObject"/> and store it.
    /// </summary>
    /// <exception cref="System.NullReferenceException">You must attach the Game component to a single GameObject instance.</exception>
    private void Awake()
    {
        if (_gameObject != null)
            throw new NullReferenceException("You must attach the Game component to a single GameObject instance.");
        _gameObject = gameObject;
    }
    #endregion

    /// <summary>
    /// The current cancellation token source for any running test.
    /// </summary>
    [CanBeNull]
    private CancellationTokenSource _currentCancellationTokenSource;

    /// <summary>
    /// The current task for any running test.
    /// </summary>
    [CanBeNull]
    private ITask _currentTask;

    /// <summary>
    /// The task text
    /// </summary>
    [Tooltip("The Text object to display Task information in.")]
    [NotNull]
    [UsedImplicitly]
    public Text TaskText;

    [Tooltip("The buttons used to run tests.")]
    [NotNull]
    [UsedImplicitly]
    public Button[] RunButtons;

    [Tooltip("The button used to cancel tests.")]
    [NotNull]
    [UsedImplicitly]
    public Button CancelButton;

    // Use this for initialization
    [UsedImplicitly]
    private void Start()
    {
    }

    // Update is called once per frame
    [UsedImplicitly]
    private void Update()
    {
        /*
         * Update the task list
         */
        // This creates a string with a line for each task
        TaskText.text = Task.All.Aggregate(
            new StringBuilder("Tasks:"+Environment.NewLine),
            (sb, t) => sb.AppendLine(t.ToString())).ToString();

        /*
         * Update button states
         */

        // Set the cancel button visibility
        CancelButton.gameObject.SetActive(CanCancel);
        
        // Get the run state once rather than calculating each time.
        bool canRun = CanRun;
        // We have multiple buttons for running tests, which hide/show as a group.
        foreach (Button button in RunButtons)
            button.gameObject.SetActive(canRun);
    }

    /// <summary>
    /// Gets a value indicating whether tests can be run.
    /// </summary>
    /// <value><see langword="true" /> if there are no running tests; otherwise, <see langword="false" />.</value>
    public bool CanRun
    {
        get
        {
            // We get a local copy of the _currentTask so that it doesn't change whilst
            // we are performing checks.
            ITask currentTask = _currentTask;
            return currentTask == null || currentTask.IsFinished;
        }
    }

    /// <summary>
    /// Gets a value indicating whether there is a running test that can be cancelled.
    /// </summary>
    /// <value><see langword="true" /> if a test can be cancelled; otherwise, <see langword="false" />.</value>
    public bool CanCancel
    {
        get {
            // We get a local copy of the _currentCancellationTokenSource so that it doesn't change whilst
            // we are performing checks.
            CancellationTokenSource currentCancellationTokenSource = _currentCancellationTokenSource;
            return currentCancellationTokenSource != null &&
                   currentCancellationTokenSource.CanBeCancelled &&
                   !currentCancellationTokenSource.IsCancellationRequested &&
                   !CanRun;
        }
    }

    /// <summary>
    /// Cancels any test that is currently running with a CancellationTokenSource.
    /// </summary>
    [UsedImplicitly]
    public void Cancel()
    {
        // This is a common thread-safe pattern, Interlocked.Exchange will grab the value of the current
        // CancellationTokenSource AND set is to null atomically, which means only one execution of this line of code
        // will ever succeed at a time.  Technically Cancel() will only run on the UI thread anyway, so this isn't
        // strictly necessary, however it's good practice to get in the habit as Cancel() is a public method and may
        // (at some future date) be called from code that is running on a different thread.
        CancellationTokenSource cts = Interlocked.Exchange(ref _currentCancellationTokenSource, null);
        if (cts != null && cts.CanBeCancelled && !cts.IsCancellationRequested)
            cts.Cancel();
    }

    /// <summary>
    /// Ensures there's only one test running at a time.
    /// </summary>
    /// <param name="task">The task.</param>
    /// <param name="cancelAfterMs">The cancel after ms.</param>
    private void RunTest(
        Func<CancellationToken, ITask> task,
        int cancelAfterMs = -1)
    {
        if (_currentTask != null)
            return;

        CancellationTokenSource cts = new CancellationTokenSource(cancelAfterMs);
        Task newTask = task(cts.Token) as Task;
        if (newTask == null)
            return;

        if (Interlocked.CompareExchange(ref _currentTask, newTask, null) != null)
        {
            // Failed to set task, so cancel.
            cts.Cancel();
            newTask.Cancel();
            return;
        }
        
        // Updated current task, set the CancellationTokenSource
        CancellationTokenSource oldCts = Interlocked.Exchange(ref _currentCancellationTokenSource, cts);

        // Ensure any existing CTS is cancelled properly.
        if (oldCts != null &&
            oldCts.CanBeCancelled)
            oldCts.Cancel();

        // TODO Change to OnFinished
        newTask.OnSuccess(
            () =>
            {
                // Blank existing task & CTS, once the task is finished
                Interlocked.CompareExchange(ref _currentTask, null, newTask);
                Interlocked.CompareExchange(ref _currentCancellationTokenSource, null, cts);
            });

        // Start the new task
        newTask.TryRun();
    }

    [UsedImplicitly]
    public void Test1()
    {
        RunTest(t => Task.Delay(3000, t));
    }
}
