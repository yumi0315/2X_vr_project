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
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace Meta.XR.BuildingBlocks.Editor
{
    public class InterfaceBlockData : BlockData
    {
        // Stateful singleton of the current VariantsSelection
        internal static VariantsSelection Selection = new();

        private InstallationRoutine _selectedRoutine;

        protected override bool UsesPrefab => false;

        private void Awake()
        {
            _selectedRoutine = null;
        }

        protected override void SetupBlockComponent(BuildingBlock block)
        {
            base.SetupBlockComponent(block);

            block.InstallationRoutineCheckpoint = _selectedRoutine.ToCheckpoint();
        }

        internal override bool IsInstallable => HasInstallationRoutine && base.IsInstallable;

        internal override bool IsInteractable =>
            HasInstallationRoutine
            && !HasMissingDependencies
            && !IsSingletonAndAlreadyPresent
            && !Utils.IsApplicationPlaying();

        internal IReadOnlyList<string> ComputeMissingPackageDependencies(VariantsSelection selection)
        {
            return ComputePackageDependencies(this, selection, new HashSet<string>())
                .Where(packageId => !Utils.IsPackageInstalled(packageId)).ToList();
        }

        internal static IEnumerable<BlockData> ComputeOptionalDependencies(InterfaceBlockData blockData, VariantsSelection selection)
        {
            var possibleRoutines = selection.ComputePossibleInstallationRoutines(blockData);
            var idealRoutine = selection.ComputeIdealInstallationRoutine(possibleRoutines);
            return idealRoutine != null ? idealRoutine.ComputeOptionalDependencies(selection) : Enumerable.Empty<BlockData>();
        }

        internal static IEnumerable<string> ComputePackageDependencies(InterfaceBlockData blockData, VariantsSelection selection)
        {
            var possibleRoutines = selection.ComputePossibleInstallationRoutines(blockData);
            var idealRoutine = selection.ComputeIdealInstallationRoutine(possibleRoutines);
            var dependencies = blockData.PackageDependencies ?? Enumerable.Empty<string>();
            if (idealRoutine != null)
            {
                dependencies = dependencies.Concat(idealRoutine.ComputePackageDependencies(selection) ?? Enumerable.Empty<string>());
            }
            return dependencies;
        }

        private static HashSet<string> ComputePackageDependencies(InterfaceBlockData blockData, VariantsSelection selection, HashSet<string> set)
        {
            foreach (var packageDependency in ComputePackageDependencies(blockData, selection))
            {
                set.Add(packageDependency);
            }

            foreach (var dependency in blockData.Dependencies)
            {
                if (dependency is InterfaceBlockData interfaceDependency)
                {
                    ComputePackageDependencies(interfaceDependency, selection, set);
                }
                else
                {
                    dependency.CollectPackageDependencies(set);
                }
            }

            return set;
        }

        internal override async Task<List<GameObject>> InstallWithDependencies(List<GameObject> selectedGameObjects)
        {
            Selection.SetupForSelection(this);

            try
            {
                _selectedRoutine = await Selection.SelectInstallationRoutine(this);

                if (Selection.Canceled)
                {
                    throw new InstallationCancelledException();
                }

                var createdObjects = new List<GameObject>();
                if (_selectedRoutine != null)
                {
                    foreach (var obj in selectedGameObjects.DefaultIfEmpty())
                    {
                        createdObjects.AddRange(await base.InstallWithDependencies(obj));
                    }
                }
                // reset the static caches
                _selectedRoutine = null;
                Selection.Release(this);
                return createdObjects;
            }
            catch (Exception)
            {
                // reset the static caches
                _selectedRoutine = null;
                Selection.Release(this, true);
                throw;
            }
        }

        internal override async Task<List<GameObject>> InstallWithDependencies(GameObject selectedGameObject = null)
        {
            return await InstallWithDependencies(new List<GameObject> { selectedGameObject });
        }

        protected override async Task<List<GameObject>> InstallRoutineAsync(GameObject selectedGameObject)
            => await _selectedRoutine.InstallAsync(this, selectedGameObject);

        internal override IEnumerable<OVRConfigurationTask> GetAssociatedRules(BuildingBlock block)
            => Utils.GetInstallationRoutine(block.InstallationRoutineCheckpoint?.InstallationRoutineId)
                ?.GetAssociatedRules(block) ?? Enumerable.Empty<OVRConfigurationTask>();

        #region InstallationRoutine

        internal bool HasInstallationRoutine => GetAvailableInstallationRoutines().Any();

        internal IEnumerable<InstallationRoutine> GetAvailableInstallationRoutines() =>
            InstallationRoutine.Registry.Values.Where(x => x.TargetBlockDataId == Id);

        #endregion

    }
}
