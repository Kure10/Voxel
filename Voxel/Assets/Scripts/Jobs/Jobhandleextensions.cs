using Cysharp.Threading.Tasks;
using Unity.Jobs;

namespace VoxelWorld
{
    /// <summary>
    /// Glue between the Job System and UniTask. A JobHandle has no await support
    /// by itself — you'd normally call handle.Complete(), but that BLOCKS the calling
    /// thread until the job finishes, which is exactly what we don't want on the main
    /// thread. This lets you write:
    ///
    ///     JobHandle handle = myJob.Schedule(count, batchSize);
    ///     await handle.ToUniTask();
    ///     // job is guaranteed finished here, safe to read its NativeArrays
    ///
    /// without ever blocking a frame — Unity's job worker threads do the real work,
    /// UniTask just checks in on it once per Update until it's done.
    /// </summary>
    public static class JobHandleExtensions
    {
        public static async UniTask ToUniTask(this JobHandle handle,
            PlayerLoopTiming timing = PlayerLoopTiming.Update)
        {
            await UniTask.WaitUntil(() => handle.IsCompleted, timing);

            // Even after IsCompleted is true, Complete() still needs to be called once —
            // it's what releases the Job System's safety fence on the NativeArrays this
            // job touched. Skipping it throws when you try to read/dispose them.
            // It's cheap here since the job has already finished.
            handle.Complete();
        }
    }
}