using System.Collections.Concurrent;
using FlowFlex.Domain.Shared;

namespace FlowFlex.Application.Services.OW.OnboardingServices
{
    /// <summary>
    /// In-memory lock service for Onboarding stage completion.
    /// Prevents concurrent Complete operations on the same Onboarding entity.
    /// Singleton lifetime — one instance per process.
    /// </summary>
    public class OnboardingCompleteLockService : ISingletonService
    {
        private readonly ConcurrentDictionary<long, SemaphoreSlim> _locks = new();

        /// <summary>
        /// AsyncLocal tracker for reentrant lock detection.
        /// If the current async flow already holds the lock for a given onboardingId,
        /// nested calls (e.g. from ConditionAction → CompleteCurrentStageInternalAsync) will skip acquisition.
        /// </summary>
        private static readonly AsyncLocal<HashSet<long>> _heldLocks = new();

        private static HashSet<long> HeldLocks => _heldLocks.Value ??= new HashSet<long>();

        /// <summary>
        /// Try to acquire the completion lock for the given onboarding.
        /// Returns true if acquired (caller must release via ReleaseLock).
        /// Returns false if another request already holds the lock (caller should fail fast).
        /// If the current async context already holds the lock (reentrant), returns true without acquiring again.
        /// </summary>
        public bool TryAcquireLock(long onboardingId, out bool isReentrant)
        {
            isReentrant = false;

            // Check reentrant: current async flow already holds this lock
            if (HeldLocks.Contains(onboardingId))
            {
                isReentrant = true;
                return true;
            }

            var semaphore = _locks.GetOrAdd(onboardingId, _ => new SemaphoreSlim(1, 1));

            // Non-blocking attempt
            if (!semaphore.Wait(0))
            {
                return false;
            }

            // Mark as held in current async context
            HeldLocks.Add(onboardingId);
            return true;
        }

        /// <summary>
        /// Release the completion lock for the given onboarding.
        /// Only call this if TryAcquireLock returned true AND isReentrant was false.
        /// </summary>
        public void ReleaseLock(long onboardingId)
        {
            HeldLocks.Remove(onboardingId);

            if (_locks.TryGetValue(onboardingId, out var semaphore))
            {
                semaphore.Release();
            }
        }
    }
}
