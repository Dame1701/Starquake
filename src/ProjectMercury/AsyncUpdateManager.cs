// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

/*
 * Copyright © 2010 Project Mercury Team Members (http://mpe.codeplex.com/People/ProjectPeople.aspx)
 *
 * This program is licensed under the Microsoft Permissive License (Ms-PL). You should
 * have received a copy of the license along with the source code. If not, an online copy
 * of the license can be found at http://mpe.codeplex.com/license.
 */

using ProjectMercury.Emitters;

namespace ProjectMercury;

using ResetEvent = ManualResetEventSlim;

/// <summary>
///     Defines a class which handles updating of particle effects asynchronously.
/// </summary>
public class AsyncUpdateManager : IDisposable
{

    /// <summary>
    ///     Gets or sets the elapsed time in whole and fractional seconds.
    /// </summary>
    private volatile float DeltaSeconds;

    /// <summary>
    ///     Notifies the worker thread when to stop running.
    /// </summary>
    private volatile bool RunWorkerThread;

    /// <summary>
    ///     Initialises a new instance of the AsyncUpdateManager class.
    /// </summary>
    public AsyncUpdateManager()
    {
        WorkerThread = new Thread(WorkerThread_Body)
        {
            Name = "AsyncUpdateManager",
            IsBackground = true
        };

        WorkAvailable = new ResetEvent(false);
        WorkDone = new ResetEvent(true);
        WorkQueue = new Queue<ParticleEffect>();
    }
    /// <summary>
    ///     Gets or sets the worker thread.
    /// </summary>
    private Thread WorkerThread { get; }

    /// <summary>
    ///     Thread event, raised when there is work available for the worker thread.
    /// </summary>
    private ResetEvent WorkAvailable { get; }

    /// <summary>
    ///     Thread event, raised when the worker thread has finished its current work.
    /// </summary>
    private ResetEvent WorkDone { get; }

    /// <summary>
    ///     Gets or sets the queue of particle effects which need to be updated by the worker thread.
    /// </summary>
    private Queue<ParticleEffect> WorkQueue { get; }

    /// <summary>
    ///     Dispose any unmanaged resources being used by this instance.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    ///     Releases unmanaged and - optionally - managed resources.
    /// </summary>
    /// <param name="disposing">
    ///     <c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only
    ///     unmanaged resources.
    /// </param>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (WorkAvailable != null)
            {
                WorkAvailable.Dispose();
            }

            if (WorkDone != null)
            {
                WorkDone.Dispose();
            }
        }
    }

    /// <summary>
    ///     Releases unmanaged resources and performs other cleanup operations before the <see cref="AsyncUpdateManager" />
    ///     is reclaimed by garbage collection.
    /// </summary>
    ~AsyncUpdateManager()
    {
        Dispose(false);
    }

    /// <summary>
    ///     Starts the worker thread running in the background.
    /// </summary>
    public void Start()
    {
        Guard.IsTrue(RunWorkerThread, "AsyncUpdateManager has already been started!");

        RunWorkerThread = true;

        WorkerThread.Start();
    }

    /// <summary>
    ///     Stops the worker thread.
    /// </summary>
    public void Stop()
    {
        Guard.IsFalse(RunWorkerThread, "AsyncUpdateManager must first be started!");

        RunWorkerThread = false;

        WorkAvailable.Set();

        WorkerThread.Join(-1);

        WorkAvailable.WaitHandle.Close();
        WorkDone.WaitHandle.Close();

    }

    /// <summary>
    ///     Passes the specified particle effects to the worker thread for updating.
    /// </summary>
    /// <param name="deltaSeconds">Elapsed time in whole and fractional seconds.</param>
    /// <param name="effects">The particle effects that should be updated.</param>
    public void BeginUpdate(float deltaSeconds, params ParticleEffect[] effects)
    {
#if WINDOWS
            Guard.IsFalse(this.WorkDone.IsSet, "Must call EndUpdate first!");
#endif
        DeltaSeconds = deltaSeconds;

        foreach (ParticleEffect effect in effects)
        {
            WorkQueue.Enqueue(effect);
        }

        WorkDone.Reset();
        WorkAvailable.Set();
    }

    /// <summary>
    ///     Blocks the calling thread until the worker thread has finished updating outstanding particle effects.
    /// </summary>
    public void EndUpdate()
    {
        Guard.IsFalse(WorkAvailable.IsSet, "Must call BeginUpdate first!");

        WorkDone.Wait(-1, CancellationToken.None);
        WorkAvailable.Reset();
    }

    /// <summary>
    ///     Worker thread body.
    /// </summary>
    private void WorkerThread_Body()
    {
        while (RunWorkerThread)
        {
            WorkAvailable.Wait(-1, CancellationToken.None);

            lock (WorkQueue)
            {
                while (WorkQueue.Count > 0)
                {
                    ParticleEffect effect = WorkQueue.Dequeue();

                    lock (effect)
                    {
                        foreach (Emitter emitter in effect)
                        {
                            emitter.Update(DeltaSeconds);
                        }
                    }
                }

                WorkDone.Set();
            }
        }
    }
}