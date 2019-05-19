using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading;

public class ThreadedDataRequester : MonoBehaviour
{
    private static ThreadedDataRequester m_instance;
    private Queue<ThreadInfo> m_dataQueue = new Queue<ThreadInfo>();

    private void Awake()
    {
        m_instance = FindObjectOfType<ThreadedDataRequester>();
    }

    public static void RequestData(Func<object> generateData, Action<object> callback)
    {
        ThreadStart threadStart = delegate
        {
            m_instance.DataThread(generateData, callback);
        };

        new Thread(threadStart).Start();
    }

    private void DataThread(Func<object> generateData, Action<object> callback)
    {
        object data = generateData();
        lock (m_dataQueue)
        {
            m_dataQueue.Enqueue(new ThreadInfo(callback, data));
        }
    }

    private void Update()
    {
        if (m_dataQueue.Count > 0)
        {
            for (int i = 0; i < m_dataQueue.Count; i++)
            {
                ThreadInfo threadInfo = m_dataQueue.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }
        }
    }

    private struct ThreadInfo
    {
        public readonly Action<object> callback;
        public readonly object parameter;

        public ThreadInfo(Action<object> callback, object parameter)
        {
            this.callback = callback;
            this.parameter = parameter;
        }
    }
}