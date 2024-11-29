﻿/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * Licensed under the Oculus SDK License Agreement (the "License");
 * you may not use the Oculus SDK except in compliance with the License,
 * which is provided at the time of installation or download, or which
 * otherwise accompanies this software in either electronic or hard copy form.
 *
 * You may obtain a copy of the License at
 *
 * https://developer.oculus.com/licenses/oculussdk/
 *
 * Unless required by applicable law or agreed to in writing, the Oculus SDK
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Collections;
using UnityEngine;
using static OVRPlugin;

partial struct OVRAnchor
{
    /// <summary>
    /// Represents a configuration for a <see cref="Tracker"/>.
    /// </summary>
    /// <remarks>
    /// Use this struct to configure a <see cref="Tracker"/> by calling <see cref="Tracker.ConfigureAsync"/>, or to
    /// get a tracker's current configuration using <see cref="Tracker.Configuration"/>.
    /// </remarks>
    [Serializable]
    public struct TrackerConfiguration : IEquatable<TrackerConfiguration>
    {
        /// <summary>
        /// The <see cref="Tracker"/> should track keyboards.
        /// </summary>
        /// <remarks>
        /// You can test for keyboard tracking support with <see cref="KeyboardTrackingSupported"/>.
        /// </remarks>
        [field: SerializeField, Tooltip("When enabled, attempts to track physical keyboards in the environment.")]
        public bool KeyboardTrackingEnabled { get; set; }

        /// <summary>
        /// Whether keyboard tracking is supported.
        /// </summary>
        /// <remarks>
        /// Use this to test for keyboard tracking support before calling <see cref="Tracker.ConfigureAsync"/> with
        /// <see cref="KeyboardTrackingEnabled"/> set to `true`.
        /// </remarks>
        public static bool KeyboardTrackingSupported => GetDynamicObjectTrackerSupported(out var value).IsSuccess() && value;

        internal bool RequiresDynamicObjectTracker => KeyboardTrackingEnabled;

        internal OVRNativeList<DynamicObjectClass> ToDynamicObjectClasses(Allocator allocator)
        {
            var list = new OVRNativeList<DynamicObjectClass>(allocator);
            if (KeyboardTrackingEnabled)
            {
                list.Add(DynamicObjectClass.Keyboard);
            }

            return list;
        }

        internal void ResetDynamicObjects() => SetDynamicObjectState(default);

        internal void SetDynamicObjectState(in TrackerConfiguration other)
        {
            KeyboardTrackingEnabled = other.KeyboardTrackingEnabled;
        }


        /// <summary>
        /// Gets the collection of <see cref="TrackableType"/>s implied by this configuration.
        /// </summary>
        /// <remarks>
        /// This method provides the types of trackables that this configuration would enable. Use this in conjunction
        /// with the static method <see cref="OVRAnchor.FetchTrackablesAsync"/> to fetch all anchors relevant to a
        /// particular <see cref="TrackerConfiguration"/>.
        ///
        /// Note: The member method <see cref="OVRAnchor.Tracker.FetchTrackablesAsync"/> performs a similar operation as
        /// <see cref="GetTrackableTypes"/> followed by <see cref="OVRAnchor.FetchTrackablesAsync"/>.
        /// </remarks>
        /// <param name="trackableTypes">The list of <see cref="TrackableType"/> to populate. The list is cleared
        /// before adding any elements.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="trackableTypes"/> is `null`.</exception>
        /// <seealso cref="OVRAnchor.FetchTrackablesAsync"/>
        public void GetTrackableTypes(List<TrackableType> trackableTypes)
        {
            if (trackableTypes == null)
                throw new ArgumentNullException(nameof(trackableTypes));

            trackableTypes.Clear();

            if (KeyboardTrackingEnabled)
            {
                trackableTypes.Add(TrackableType.Keyboard);
            }

        }

        /// <summary>
        /// Generates a string representation of this <see cref="TrackerConfiguration"/>.
        /// </summary>
        /// <returns>Returns a string representation of this <see cref="TrackerConfiguration"/>.</returns>
        public override string ToString()
        {
            using (new OVRObjectPool.ListScope<string>(out var fields))
            {
                fields.Add($"{nameof(KeyboardTrackingEnabled)}={KeyboardTrackingEnabled}");
                return $"{nameof(TrackerConfiguration)}<{string.Join(", ", fields)}>";
            }
        }

        /// <summary>
        /// Determines whether two <see cref="TrackerConfiguration"/> instances are equal.
        /// </summary>
        /// <param name="other">The other <see cref="TrackerConfiguration"/> to compare with this one.</param>
        /// <returns>Returns `true` if <paramref name="other"/> is equal to this one; otherwise, `false`.</returns>
        public bool Equals(TrackerConfiguration other)
        {
            if (KeyboardTrackingEnabled != other.KeyboardTrackingEnabled) return false;
            return true;
        }

        /// <summary>
        /// Determines whether an `object` is equal to this <see cref="TrackerConfiguration"/>.
        /// </summary>
        /// <param name="obj">The `object` to compare with this <see cref="TrackerConfiguration"/>.</param>
        /// <returns>Returns `true` if <paramref name="obj"/> is an instance of an <see cref="TrackerConfiguration"/>
        /// and is equal to this one; otherwise, `false`.</returns>
        public override bool Equals(object obj) => obj is TrackerConfiguration other && Equals(other);

        /// <summary>
        /// Gets a hashcode for this <see cref="TrackerConfiguration"/>.
        /// </summary>
        /// <returns>Returns a hash code for this <see cref="TrackerConfiguration"/>.</returns>
        public override int GetHashCode()
        {
            var hashCode = 0;
            hashCode = HashCode.Combine(hashCode, KeyboardTrackingEnabled);
            return hashCode;
        }

        /// <summary>
        /// Determines whether two <see cref="TrackerConfiguration"/> instances are equal.
        /// </summary>
        /// <remarks>
        /// This is the same as comparing <paramref name="lhs"/> with <paramref name="rhs"/> using
        /// <see cref="Equals(TrackerConfiguration)"/>.
        /// </remarks>
        /// <param name="lhs">The <see cref="TrackerConfiguration"/> to compare with <paramref name="rhs"/>.</param>
        /// <param name="rhs">The <see cref="TrackerConfiguration"/> to compare with <paramref name="lhs"/>.</param>
        /// <returns>Returns `true` if <paramref name="lhs"/> is equal to <paramref name="rhs"/>; otherwise, false.</returns>
        public static bool operator ==(TrackerConfiguration lhs, TrackerConfiguration rhs) => lhs.Equals(rhs);

        /// <summary>
        /// Determines whether two <see cref="TrackerConfiguration"/> instances are not equal.
        /// </summary>
        /// <remarks>
        /// This is the same as comparing <paramref name="lhs"/> with <paramref name="rhs"/> using
        /// <see cref="Equals(TrackerConfiguration)"/> and negating the result.
        /// </remarks>
        /// <param name="lhs">The <see cref="TrackerConfiguration"/> to compare with <paramref name="rhs"/>.</param>
        /// <param name="rhs">The <see cref="TrackerConfiguration"/> to compare with <paramref name="lhs"/>.</param>
        /// <returns>Returns `false` if <paramref name="lhs"/> is equal to <paramref name="rhs"/>; otherwise, true.</returns>
        public static bool operator !=(TrackerConfiguration lhs, TrackerConfiguration rhs) => !lhs.Equals(rhs);
    }

    /// <summary>
    /// The result of <see cref="Tracker.ConfigureAsync"/>.
    /// </summary>
    [OVRResultStatus]
    public enum ConfigureTrackerResult
    {
        /// <summary>
        /// The tracker was configured successfully.
        /// </summary>
        Success = Result.Success,

        /// <summary>
        /// Tracker configuration failed unexpectedly.
        /// </summary>
        Failure = Result.Failure,

        /// <summary>
        /// The <see cref="OVRResult"/> does not represent a valid result.
        /// </summary>
        Invalid = Result.Failure_DataIsInvalid,

        /// <summary>
        /// A configuration is not supported.
        /// </summary>
        NotSupported = Result.Failure_Unsupported,
    }

    /// <summary>
    /// Represents system resources related to trackable anchors.
    /// </summary>
    /// <remarks>
    /// A "trackable" is a type of anchor that can be detected at runtime.
    ///
    /// When you create a new <see cref="Tracker"/>, you must then "configure" it using <see cref="ConfigureAsync"/>.
    ///
    /// When you no longer need a realtime tracker, you should disable trackers by disposing the <see cref="Tracker"/>
    /// (see <see cref="Dispose"/>).
    /// </remarks>
    public sealed class Tracker : IDisposable
    {
        /// <summary>
        /// The current configuration for this <see cref="Tracker"/>.
        /// </summary>
        /// <remarks>
        /// This property may differ from what was last requested with <see cref="ConfigureAsync"/> if one or more
        /// of the requested configuration options was not able to be satisfied. This represents the current state
        /// of the tracker.
        /// </remarks>
        public TrackerConfiguration Configuration => _configuration;

        private TrackerConfiguration _configuration;

        // Internal note: see AsyncLock
        private int _asyncOperationCount;


        private ulong _dynamicObjectTracker;

        private async OVRTask<Result> SetupDynamicObjectTracker(TrackerConfiguration config)
        {
            static OVRTask<OVRResult<Result>> SetClassesAsync(ulong tracker, TrackerConfiguration config)
            {
                using (var list = config.ToDynamicObjectClasses(Allocator.Temp))
                {
                    // The list is freed without waiting for the task to complete.
                    return SetDynamicObjectTrackedClassesAsync(tracker, list);
                }
            }

            static async OVRTask<OVRResult<ulong, Result>> CreateAndConfigureTrackerAsync(ulong tracker,
                TrackerConfiguration config)
            {
                if (tracker != 0)
                {
                    return OVRResult<ulong, Result>.From(tracker, (await SetClassesAsync(tracker, config)).Status);
                }

                // Create a new tracker
                var createResult = await CreateDynamicObjectTrackerAsync();
                if (!createResult.Success)
                {
                    return OVRResult<ulong, Result>.FromFailure(createResult.Status);
                }

                tracker = createResult.Value;
                var setClassesResult = await SetClassesAsync(tracker, config);

                // Unlike the case above where we already had a tracker, if we successfully created a tracker but
                // weren't able to set its classes, then we should destroy it to maintain the current state.
                if (!setClassesResult.Success)
                {
                    // Unable to set the classes; destroy the tracker
                    DestroyDynamicObjectTracker(tracker);
                    tracker = 0;
                }

                return OVRResult<ulong, Result>.From(tracker, setClassesResult.Status);
            }

            _configuration.ResetDynamicObjects();

            if (config.RequiresDynamicObjectTracker)
            {
                var result = await CreateAndConfigureTrackerAsync(_dynamicObjectTracker, config);
                if (result.Success)
                {
                    _dynamicObjectTracker = result.Value;
                    _configuration.SetDynamicObjectState(config);
                }
                else
                {
                    _dynamicObjectTracker = 0;
                }

                return result.Status;
            }
            else // We don't need any dynamic object tracking
            {
                // Destroy the tracker if one already exists
                if (_dynamicObjectTracker != 0)
                {
                    DestroyDynamicObjectTracker(_dynamicObjectTracker);
                    _dynamicObjectTracker = 0;
                }

                return Result.Success;
            }
        }

        private struct AsyncLock : IDisposable
        {
            Tracker _tracker;

            AsyncLock(Tracker tracker)
            {
                _tracker = tracker;
                _tracker._asyncOperationCount++;
            }

            public void Dispose() => _tracker._asyncOperationCount--;

            public static async OVRTask<AsyncLock> AcquireAsync(Tracker tracker)
            {
                while (tracker._asyncOperationCount > 0)
                {
                    await Task.Yield();
                }

                return new(tracker);
            }
        }

        /// <summary>
        /// Configures this <see cref="Tracker"/> with the specified <paramref name="configuration"/>.
        /// </summary>
        /// <remarks>
        /// It is possible for some configuration options to be satisfied while others are not, if, for example, one
        /// type of tracking is supported while another is not. In this case, <see cref="ConfigureAsync"/> will return
        /// something other than success (<see cref="OVRResult{T}.Success"/>) even though some options have been
        /// satisfied.
        ///
        /// <see cref="Configuration"/> represents the current state of the tracker at any given time; use this to
        /// determine the current configuration.
        /// </remarks>
        /// <param name="configuration">The configuration this tracker should use.</param>
        /// <returns>Returns an async task representing the state and eventual result of the operation.</returns>
        public async OVRTask<OVRResult<ConfigureTrackerResult>> ConfigureAsync(TrackerConfiguration configuration)
        {
            using (await AsyncLock.AcquireAsync(this))
            using (new OVRObjectPool.TaskScope<Result>(out var tasks, out var results))
            {
                tasks.Add(SetupDynamicObjectTracker(configuration));

                await OVRTask.WhenAll(tasks, results);

                // Report errors
                foreach (var result in results)
                {
                    if (!result.IsSuccess())
                    {
                        Debug.LogError($"Error while setting trackable configuration {configuration}: {result}");
                    }
                }

                foreach (var result in results)
                {
                    if (!result.IsSuccess())
                    {
                        return OVRResult.From((ConfigureTrackerResult)result);
                    }
                }

                return OVRResult.From(ConfigureTrackerResult.Success);
            }
        }

        /// <summary>
        /// Fetch anchors that match the <see cref="TrackableType"/> according to the current <see cref="Configuration"/>.
        /// </summary>
        /// <remarks>
        /// This method queries for anchors may be detected according to the current <see cref="Configuration"/>.
        ///
        /// Anchors may be returned in batches. If <paramref name="incrementalResultsCallback"/> is not `null`, then this
        /// delegate is invoked whenever results become available prior to the completion of the entire operation. New anchors
        /// are appended to <paramref name="anchors"/>. The delegate receives a reference to <paramref name="anchors"/> and
        /// the starting index of the anchors that have been added. The parameters are:
        /// - `anchors`: The same `List` provided by <paramref name="anchors"/>.
        /// - `index`: The starting index of the newly available anchors
        ///
        /// This is similar to calling
        /// <see cref="OVRAnchor.FetchTrackablesAsync(List{OVRAnchor},IEnumerable{TrackableType},Action{List{OVRAnchor},int})"/>
        /// with an array of <see cref="TrackableType"/>. You can get the trackables associated with a
        /// <see cref="TrackerConfiguration"/> with <see cref="TrackerConfiguration.GetTrackableTypes"/>.
        /// </remarks>
        /// <param name="anchors">Container to store the results. The list is cleared before adding any anchors.</param>
        /// <param name="incrementalResultsCallback">(Optional) A callback invoked when incremental results are available.</param>
        /// <returns>Returns an <see cref="OVRTask"/> that can be used to track the asynchronous fetch.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="anchors"/> is `null`.</exception>
        public OVRTask<OVRResult<List<OVRAnchor>, FetchResult>> FetchTrackablesAsync(List<OVRAnchor> anchors,
            Action<List<OVRAnchor>, int> incrementalResultsCallback = null)
        {
            if (anchors == null)
                throw new ArgumentNullException(nameof(anchors));

            using (new OVRObjectPool.ListScope<TrackableType>(out var trackableTypes))
            {
                _configuration.GetTrackableTypes(trackableTypes);
                return OVRAnchor.FetchTrackablesAsync(anchors, trackableTypes, incrementalResultsCallback);
            }
        }

        /// \cond
        ~Tracker()
        {
            if (
                _dynamicObjectTracker != 0)
            {
                Debug.LogError($"{nameof(Tracker)} was not disposed of while one or more trackers were active, " +
                               $"which leaks resources. Call Dispose() when no longer needed.");
            }
        }
        /// \endcond

        public async void Dispose()
        {
            using (await AsyncLock.AcquireAsync(this))
            {
                if (_dynamicObjectTracker != 0)
                {
                    DestroyDynamicObjectTracker(_dynamicObjectTracker);
                }

                _dynamicObjectTracker = 0;
                _configuration = default;
            }
        }
    }
}
