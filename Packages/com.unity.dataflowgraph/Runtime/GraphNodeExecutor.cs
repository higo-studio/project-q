﻿using System;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;

namespace Unity.DataFlowGraph
{
    [JobProducerType(typeof(IGraphNodeExecutorExtensions.JobStruct<>))]
    interface IGraphNodeExecutor
    {
        void Execute();
    }

    interface IVirtualFunctionDeclaration
    {
        IntPtr ReflectionData { get; }
    }

    static class IGraphNodeExecutorExtensions
    {
        internal struct JobStruct<T> where T : struct, IGraphNodeExecutor
        {
            public static IntPtr JobReflectionData = Initialize();

            static IntPtr Initialize()
            {
                return JobsUtility.CreateJobReflectionData(typeof(T), (ExecuteJobFunction)Execute);
            }

            delegate void ExecuteJobFunction(ref T data, IntPtr additionalPtr, IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex);
            static void Execute(ref T data, IntPtr additionalPtr, IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex)
            {
                data.Execute();
            }
        }
    }
}
