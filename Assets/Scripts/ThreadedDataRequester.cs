using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading;

/// <summary>
/// Manages the requesting and usage of threads.
/// </summary>
public class ThreadedDataRequester : MonoBehaviour
{
    /// <summary>
    /// An instance of the object for this class.
    /// </summary>
    private static ThreadedDataRequester m_instance;
    /// <summary>
    /// A queue containing all the threads that are wanting to run.
    /// </summary>
    private Queue<ThreadInfo> m_dataQueue = new Queue<ThreadInfo>();

    /// <summary>
    /// Gets an instance of the object containing this class.
    /// </summary>
    private void Awake()
    {
        m_instance = FindObjectOfType<ThreadedDataRequester>();
    }

    /// <summary>
    /// Starts a thread for the method.
    /// </summary>
    /// <param name="generateData">The function that will produce a parameter for the callback.</param>
    /// <param name="callback">The callback that will be run on the thread.</param>
    public static void RequestData(Func<object> generateData, Action<object> callback)
    {
        // creates a thread for the function
        ThreadStart threadStart = delegate
        {
            m_instance.DataThread(generateData, callback);
        };
        // starts the thread
        new Thread(threadStart).Start();
    }

    /// <summary>
    /// Adds the thread info to the queue and locks it when possible.
    /// </summary>
    /// <param name="generateData">The function that will produce a parameter for the callback.</param>
    /// <param name="callback">The callback that will be run on a thread.</param>
    private void DataThread(Func<object> generateData, Action<object> callback)
    {
        // gets the product of the function
        object data = generateData();
        // ensures that this queue is the only object accessing the thread
        lock (m_dataQueue)
        {
            // creates and stores the thread info to then be called on the next frame
            m_dataQueue.Enqueue(new ThreadInfo(callback, data));
        }
    }

    /// <summary>
    /// Removes and runs each thread every frame.
    /// </summary>
    private void Update()
    {
        if (m_dataQueue.Count > 0)
        {
            for (int i = 0; i < m_dataQueue.Count; i++)
            {
                // moves the thread into another reference
                ThreadInfo threadInfo = m_dataQueue.Dequeue();
                // calls the function and passes in the object
                threadInfo.callback(threadInfo.parameter);
            }
        }
    }

    /// <summary>
    /// Contains a callback and the parameter of the method.
    /// </summary>
    private struct ThreadInfo
    {
        /// <summary>
        /// The callback of the thread.
        /// </summary>
        public readonly Action<object> callback;
        /// <summary>
        /// The parameter of the callback.
        /// </summary>
        public readonly object parameter;

        /// <summary>
        /// Initialises the thread info.
        /// </summary>
        /// <param name="callback">Callback of the thread.</param>
        /// <param name="parameter">Parameter of the callback.</param>
        public ThreadInfo(Action<object> callback, object parameter)
        {
            this.callback = callback;
            this.parameter = parameter;
        }
    }
}