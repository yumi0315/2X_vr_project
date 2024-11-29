/*
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
using Meta.XR.Util;
using static OVRPlugin;

/// <summary>
/// Low-level functionality related to Scene Understanding
/// </summary>
/// <remarks>
/// Scene empowers you to quickly build complex and scene-aware experiences with rich interactions in the user’s
/// physical environment.
///
/// See [Unity Scene Overview](https://developer.oculus.com/documentation/unity/unity-scene-overview) for more details.
/// </remarks>
[Feature(Feature.Scene)]
public static partial class OVRScene
{
    /// <summary>
    /// Requests Space Setup.
    /// </summary>
    /// <remarks>
    /// <parmaref name="labels"/> is collection of comma-separated semantic labels that the user must define during
    /// Space Setup. You may specify the same label multiple times.
    /// <example>
    /// For example, this code:
    /// <code><![CDATA[
    /// await OVRScene.RequestSpaceSetup("TABLE,TABLE");
    /// ]]></code>
    /// would prompt the user to define two tables.
    /// </example>
    /// </remarks>
    /// <param name="labels">The types of anchors that the user should define.</param>
    /// <returns>Returns a task that can be used to track the asynchronous operation.</returns>
    /// <exception cref="ArgumentException">Thrown if <paramref name="labels"/> contains a label that is not one of
    /// those provided by <see cref="OVRSemanticLabels.Classification"/></exception>
    [Obsolete("Requesting space setup with labels is deprecated (v71) with no replacement.")]
    public static OVRTask<bool> RequestSpaceSetup(string labels)
    {
#if DEVELOPMENT_BUILD || OVRPLUGIN_TESTING
        if (!string.IsNullOrEmpty(labels))
        {
            ValidateRequestString(labels.Split(','), nameof(labels));
        }
#endif

        return RequestSceneCapture(labels, out var requestId)
            ? OVRTask.FromRequest<bool>(requestId)
            : OVRTask.FromResult(false);
    }

    /// <summary>
    /// Requests Space Setup
    /// </summary>
    /// <remarks>
    /// Requests [Space Setup](https://developer.oculus.com/documentation/unity/unity-scene-overview/#how-does-scene-work).
    /// Space Setup pauses the application and prompts the user to setup their Space. The app resumes when the user
    /// either cancels or completes Space Setup.
    ///
    /// This method is asynchronous. The result of the task indicates whether the operation was successful. `False`
    /// usually indicates an unexpected failure; if the user cancels Space Setup, the operation still completes
    /// successfully.
    /// </remarks>
    /// <returns>A task that can be used to track the asynchronous operation.</returns>
    public static OVRTask<bool> RequestSpaceSetup()
    {
        return RequestSceneCapture(string.Empty, out var requestId)
            ? OVRTask.FromRequest<bool>(requestId)
            : OVRTask.FromResult(false);
    }

    /// <summary>
    /// Requests Space Setup using a list of classifications
    /// </summary>
    /// <param name="classifications">The types of anchors that the user should define.</param>
    /// <returns>Returns a task that can be used to track the asynchronous operation.</returns>
    /// <exception cref="ArgumentException">Thrown if <paramref name="classifications"/> contains a label that is not
    /// one of those provided by <see cref="OVRSemanticLabels.Classification"/></exception>
    [Obsolete("Requesting space setup with labels is deprecated (v71) with no replacement.")]
    public static OVRTask<bool> RequestSpaceSetup(IReadOnlyList<OVRSemanticLabels.Classification> classifications) =>
#pragma warning disable CS0618 // Type or member is obsolete
        RequestSpaceSetup(OVRSemanticLabels.ToApiString(classifications));
#pragma warning restore CS0618 // Type or member is obsolete

    static void ValidateRequestString(IEnumerable<string> labels, string paramName)
    {
        foreach (var label in labels.ToNonAlloc())
        {
#pragma warning disable CS0618 // Type or member is obsolete
            if (label == null || !OVRSceneManager.Classification.Set.Contains(label))
#pragma warning restore CS0618 // Type or member is obsolete
            {
                throw new ArgumentException($"'{label}' is not a valid label. See " +
                    $"{nameof(OVRSemanticLabels)}.{nameof(OVRSemanticLabels.Classification)}.", paramName);
            }
        }
    }
}
