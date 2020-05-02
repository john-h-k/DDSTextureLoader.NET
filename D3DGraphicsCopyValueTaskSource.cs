using System;
using System.Threading.Tasks.Sources;
using TerraFX.Interop;
using static TerraFX.Interop.Kernel32;
using static TerraFX.Interop.Windows;
using System.Threading;
using System.Threading.Tasks;

#nullable enable

namespace DDSTextureLoader.NET
{
    internal sealed class D3DGraphicsCopyValueTaskSource : IValueTaskSource
    {
        private IntPtr _eventHandle;

        public unsafe void Init(ID3D12Device* device, ID3D12CommandQueue* queue)
        {
            Guid iid = D3D12.IID_ID3D12Fence;
            ID3D12Fence* fence;

            // TODO throw if failed
            device->CreateFence(0, D3D12_FENCE_FLAGS.D3D12_FENCE_FLAG_NONE, &iid, (void**)&fence);

            // TODO set debug name

            const int EVENT_ALL_ACCESS = 0x1F0003;
            HANDLE completion = CreateEventExA(null, null, 0, EVENT_ALL_ACCESS);

            queue->Signal(fence, 1);
            fence->SetEventOnCompletion(1, completion);
        }

        public void GetResult(short token)
        {
            var status = WaitForSingleObjectEx(_eventHandle, INFINITE, FALSE);

            if (status != WAIT_OBJECT_0)
            {
                throw null!; // TODO
            }
        }

        private static ValueTaskSourceStatus Win32ToValueTaskStatus(uint hresult)
        {
            return hresult switch
            {
                WAIT_FAILED => ValueTaskSourceStatus.Faulted,
                WAIT_ABANDONED => ValueTaskSourceStatus.Canceled,
                WAIT_OBJECT_0 => ValueTaskSourceStatus.Succeeded,
                _ => throw null!,
            };
        }

        public ValueTaskSourceStatus GetStatus(short token)
        {
            var status = WaitForSingleObjectEx(_eventHandle, 0, FALSE);
            if (status == WAIT_TIMEOUT)
            {
                return ValueTaskSourceStatus.Pending;
            }

            return Win32ToValueTaskStatus(status);
        }

        public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags)
        {
            if (continuation == null)
            {
                throw new ArgumentNullException(nameof(continuation));
            }

            ExecutionContext? executionContext = null;
            object? capturedContext = null;

            if ((flags & ValueTaskSourceOnCompletedFlags.FlowExecutionContext) != 0)
            {
                executionContext = ExecutionContext.Capture();
            }

            if ((flags & ValueTaskSourceOnCompletedFlags.UseSchedulingContext) != 0)
            {
                SynchronizationContext? sc = SynchronizationContext.Current;
                if (sc != null && sc.GetType() != typeof(SynchronizationContext))
                {
                    capturedContext = sc;
                }
                else
                {
                    TaskScheduler ts = TaskScheduler.Current;
                    if (ts != TaskScheduler.Default)
                    {
                        capturedContext = ts;
                    }
                }
            }

            switch (capturedContext)
            {
                case null:
                    if (executionContext is object)
                    {
                        ThreadPool.QueueUserWorkItem(continuation, state, preferLocal: true);
                    }
                    else
                    {
                        ThreadPool.UnsafeQueueUserWorkItem(continuation, state, preferLocal: true);
                    }
                    break;

                case SynchronizationContext sc:
                    sc.Post(s =>
                    {
                        var tuple = (Tuple<Action<object?>, object?>)s!;
                        tuple.Item1(tuple.Item2);
                    }, Tuple.Create(continuation, state));
                    break;

                case TaskScheduler ts:
                    Task.Factory.StartNew(continuation, state, CancellationToken.None, TaskCreationOptions.DenyChildAttach, ts);
                    break;
            }
        }
    }
}
