using System;
using UniRx;
using UnityEngine;

namespace Codebase.Extension
{
    public static class RX
    {
        public static IDisposable LoopedTimer(float initialDelay, float interval, Action callback, bool inThreadPool = false) =>
            Observable
                .Timer(TimeSpan.FromSeconds(initialDelay), TimeSpan.FromSeconds(interval), inThreadPool ? Scheduler.ThreadPool : Scheduler.MainThread)
                .Subscribe(_ => callback?.Invoke());

        public static IDisposable CountedTimer(float initialDelay, float interval, int repeatingCount, Action callback,
            Action totalCompleteCallback = null) =>
            Observable
                .Timer(TimeSpan.FromSeconds(initialDelay), TimeSpan.FromSeconds(interval))
                .Take(repeatingCount)
                .Subscribe(_ => callback?.Invoke(), () => totalCompleteCallback?.Invoke());

        public static IDisposable Delay(float delay, Action callback, bool withNativeTimescale = true) =>
            Observable
                .Timer(TimeSpan.FromSeconds(delay), TimeSpan.FromSeconds(0), withNativeTimescale ? Scheduler.MainThread : Scheduler.MainThreadIgnoreTimeScale)
                .Take(1)
                .Subscribe(_ => callback?.Invoke());

        public static IObservable<float> DoValue(float startValue, float endValue, float duration, Action onComplete = null) =>
            Observable.Create<float>(observer =>
            {
                float startTime = Time.realtimeSinceStartup;
                float progress = 0.0f;

                void UpdateProgress()
                {
                    float elapsed = Time.realtimeSinceStartup - startTime;
                    progress = Mathf.Clamp01(elapsed / duration);

                    if (progress >= 1.0f)
                    {
                        observer.OnNext(endValue);
                        observer.OnCompleted();
                    }
                    else
                    {
                        observer.OnNext(Mathf.Lerp(startValue, endValue, progress));
                    }
                }

                UpdateProgress();

                return new CompositeDisposable
                {
                    Observable.EveryUpdate().Subscribe(_ => UpdateProgress()),
                    Disposable.Create(() => onComplete?.Invoke())
                };
            });

        public static IObservable<long> DoValue(long startValue, long endValue, float duration, Action onComplete = null) =>
            Observable.Create<long>(observer =>
            {
                float startTime = Time.time;
                float progress = 0.0f;
                float step = endValue - startValue;

                void UpdateProgress()
                {
                    progress = Mathf.Clamp01((Time.time - startTime) / duration);

                    if (progress >= 1.0f)
                    {
                        observer.OnNext(endValue);
                        observer.OnCompleted();
                    }
                    else
                    {
                        observer.OnNext(startValue + (long)(step * progress));
                    }
                }

                UpdateProgress();

                return new CompositeDisposable
                {
                    Observable.EveryUpdate().Subscribe(_ => UpdateProgress()),
                    Disposable.Create(() => onComplete?.Invoke())
                };
            });
    }
}