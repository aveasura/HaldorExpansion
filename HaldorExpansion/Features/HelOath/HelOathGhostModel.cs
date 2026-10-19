using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering;

namespace HaldorExpansion.Features.HelOath
{
    // Build a new render hierarchy explicitly: Instantiate(player/Visual) would also clone scripts,
    // animation event receivers, cloth and possibly mod-added gameplay/network components.
    internal sealed class HelOathGhostModel
    {
        private static readonly FieldInfo AnimatorField = AccessTools.Field(typeof(Character), "m_animator");
        private static readonly FieldInfo LeftItemField = AccessTools.Field(typeof(VisEquipment), "m_leftItemInstance");
        private readonly Dictionary<Transform, Transform> bones = new Dictionary<Transform, Transform>();

        private readonly List<KeyValuePair<Transform, Transform>> bowBones =
            new List<KeyValuePair<Transform, Transform>>();

        private readonly List<string> bowPaths = new List<string>();
        private VisEquipment equipment;
        private GameObject boundHeldItem;
        private string heldItemName;
        private Animator sourceAnimator;
        private Animator animator;
        private AnimatorControllerParameter[] parameters;
        private int drawHash;
        private bool hasDraw;

        private Player owner;
        private string drawStateName;
        private string fireTrigger;
        private int fireHash;
        private bool hasFireTrigger;
        private bool firePending;
        private readonly HelOathGhostBowCycle bowCycle = new HelOathGhostBowCycle();
        private int sourceCycle, scheduledCycle;
        private Snapshot lastDrawing;
        private readonly List<QueuedDraw> queuedDraws = new List<QueuedDraw>();

        private sealed class QueuedDraw
        {
            internal int Cycle;
            internal float Due;
            internal Snapshot Pose;
        }

        private Vector3 releaseAim;
        private Transform visualRoot;

        internal void SetRise(float amount)
        {
            if (visualRoot != null) visualRoot.localPosition = visualBasePosition + Vector3.up * amount;
        }

        private Vector3 visualBasePosition;
        internal float ReleaseDuration { get; private set; } = 0.8f;

        internal int QueueShot(float due)
        {
            Snapshot pose = lastDrawing;
            if (mirroring && sourceAnimator != null)
            {
                clock = Time.time;
                Capture();
            }

            if (sourceCycle <= scheduledCycle) sourceCycle = scheduledCycle + 1;
            scheduledCycle = sourceCycle;
            queuedDraws.Add(new QueuedDraw
            {
                Cycle = scheduledCycle, Due = due,
                Pose = pose ?? lastDrawing ?? (snapshots.Count > 0 ? snapshots[snapshots.Count - 1] : resting)
            });
            return scheduledCycle;
        }

        internal void CancelShot(int cycle) => queuedDraws.RemoveAll(p => p.Cycle == cycle);

        internal bool Fired(Vector3 direction, int cycle)
        {
            if (queuedDraws.Count == 0 || queuedDraws[0].Cycle != cycle
                                       || !bowCycle.Fire(cycle, Time.time, ReleaseDuration)) return false;
            queuedDraws.RemoveAt(0);
            firePending = true;
            releaseAim = direction.sqrMagnitude > 0.001f ? direction.normalized : AimDirection;
            AimDirection = releaseAim;
            IsAiming = true;
            return true;
        }

        private readonly List<Snapshot> snapshots = new List<Snapshot>(40);
        private int[] weaponValues;
        private float[] weaponFloats;
        private float clock, lastCapture = -1f, playbackTime = -1f;
        private bool sourceDrawing;
        private bool mirroring = true;
        private Snapshot resting;
        internal float ReactionDelay { get; private set; } = HelOathGhostRules.ReactionMin;
        internal Vector3 AimDirection { get; private set; } = Vector3.forward;
        internal bool IsAiming { get; private set; }

        private sealed class Snapshot
        {
            internal float Time;
            internal bool Drawing;
            internal int Cycle;
            internal bool[] DrawStates;
            internal Vector3 Aim;
            internal float[] Floats, Weights;
            internal int[] Ints;
            internal bool[] Bools;
            internal AnimatorStateInfo[] States;
            internal Vector3[] Positions, Scales;
            internal Quaternion[] Rotations;
        }

        internal void Build(Player owner, Transform parent, HelOathGhostAppearance appearance)
        {
            this.owner = owner;
            GameObject visual = owner.GetVisual();
            sourceAnimator = AnimatorField.GetValue(owner) as Animator;
            if (visual == null || sourceAnimator == null || sourceAnimator.runtimeAnimatorController == null)
                throw new InvalidOperationException("Player visual/animator is not ready.");
            Transform visualCopy = CopyHierarchy(visual.transform, parent);
            visualRoot = visualCopy;
            visualBasePosition = visualCopy.localPosition;
            // Strip transient tilt from swimming/sliding; the companion handles its own ground height.
            visualCopy.localRotation = Quaternion.identity;
            var lowerLods = new HashSet<Renderer>();
            foreach (LODGroup group in visual.GetComponentsInChildren<LODGroup>(true))
            {
                LOD[] lods = group.GetLODs();
                var first = new HashSet<Renderer>();
                if (lods.Length > 0)
                    foreach (Renderer renderer in lods[0].renderers)
                        first.Add(renderer);
                for (int i = 1; i < lods.Length; i++)
                    foreach (Renderer renderer in lods[i].renderers)
                        if (!first.Contains(renderer))
                            lowerLods.Add(renderer);
            }

            int count = 0;
            foreach (Renderer source in visual.GetComponentsInChildren<Renderer>(true))
            {
                if (!source.enabled || !source.gameObject.activeInHierarchy || lowerLods.Contains(source)) continue;
                Renderer copy;
                var skin = source as SkinnedMeshRenderer;
                if (skin != null)
                {
                    if (skin.sharedMesh == null) continue;
                    var target = bones[source.transform].gameObject.AddComponent<SkinnedMeshRenderer>();
                    target.sharedMesh = skin.sharedMesh;
                    target.rootBone = Remap(skin.rootBone);
                    Transform[] originalBones = skin.bones;
                    var mapped = new Transform[originalBones.Length];
                    for (int i = 0; i < mapped.Length; i++) mapped[i] = Remap(originalBones[i]);
                    target.bones = mapped;
                    target.localBounds = skin.localBounds;
                    target.quality = skin.quality;
                    target.updateWhenOffscreen = false;
                    for (int i = 0; i < skin.sharedMesh.blendShapeCount; i++)
                        target.SetBlendShapeWeight(i, skin.GetBlendShapeWeight(i));
                    copy = target;
                }
                else if (source is MeshRenderer)
                {
                    var mesh = source.GetComponent<MeshFilter>();
                    if (mesh == null || mesh.sharedMesh == null) continue;
                    var destination = bones[source.transform].gameObject;
                    destination.AddComponent<MeshFilter>().sharedMesh = mesh.sharedMesh;
                    copy = destination.AddComponent<MeshRenderer>();
                }
                else continue; // No copied particles, line effects, lights, audio or scripts.

                copy.sharedMaterials = appearance.CopyMaterials(source.sharedMaterials);
                copy.shadowCastingMode = ShadowCastingMode.Off;
                copy.receiveShadows = false;
                copy.lightProbeUsage = LightProbeUsage.Off;
                copy.reflectionProbeUsage = ReflectionProbeUsage.Off;
                count++;
            }

            if (count == 0) throw new InvalidOperationException("No visible player meshes available.");

            animator = bones[sourceAnimator.transform].gameObject.AddComponent<Animator>();
            animator.enabled = false; // Evaluated explicitly after movement, with events disabled.
            animator.avatar = sourceAnimator.avatar;
            animator.runtimeAnimatorController = sourceAnimator.runtimeAnimatorController;
            animator.applyRootMotion = false;
            animator.fireEvents = false;
            animator.logWarnings = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            parameters = sourceAnimator.parameters;
            weaponValues = new int[parameters.Length];
            weaponFloats = new float[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                if (!EquipmentParameter(parameters[i].name)) continue;
                if (parameters[i].type == AnimatorControllerParameterType.Int)
                    weaponValues[i] = sourceAnimator.GetInteger(parameters[i].nameHash);
                else if (parameters[i].type == AnimatorControllerParameterType.Float)
                    weaponFloats[i] = sourceAnimator.GetFloat(parameters[i].nameHash);
            }

            var weapon = owner.GetCurrentWeapon();
            string drawName = weapon != null && weapon.m_shared != null && weapon.m_shared.m_attack != null
                ? weapon.m_shared.m_attack.m_drawAnimationState
                : null;
            if (string.IsNullOrEmpty(drawName)) drawName = "bow_draw";
            drawStateName = drawName;
            fireTrigger = weapon?.m_shared?.m_attack?.m_attackAnimation;
            if (string.IsNullOrEmpty(fireTrigger)) fireTrigger = "bow_fire";
            fireHash = Animator.StringToHash(fireTrigger);
            foreach (var clip in sourceAnimator.runtimeAnimatorController.animationClips)
            {
                if (clip == null) continue;
                string name = clip.name.ToLowerInvariant();
                if (name.Contains("bow") &&
                    (name.Contains("fire") || name.Contains("shoot") || name.Contains("release")))
                    ReleaseDuration = Mathf.Max(ReleaseDuration, clip.length);
            }

            foreach (var p in parameters)
                if (p.nameHash == fireHash && p.type == AnimatorControllerParameterType.Trigger)
                    hasFireTrigger = true;
            drawHash = Animator.StringToHash(drawName);
            foreach (var p in parameters)
                if (p.nameHash == drawHash && p.type == AnimatorControllerParameterType.Bool)
                    hasDraw = true;

            equipment = owner.GetComponent<VisEquipment>();
            var held = equipment != null ? LeftItemField?.GetValue(equipment) as GameObject : null;
            boundHeldItem = held;
            heldItemName = held != null ? held.name : null;
            if (held != null)
                foreach (Transform source in held.GetComponentsInChildren<Transform>(true))
                {
                    Transform target;
                    if (bones.TryGetValue(source, out target))
                    {
                        bowBones.Add(new KeyValuePair<Transform, Transform>(source, target));
                        string path = "";
                        for (Transform part = source; part != held.transform; part = part.parent)
                            path = path.Length == 0 ? part.name : part.name + "/" + path;
                        bowPaths.Add(path);
                    }
                }
        }

        internal void Start()
        {
            clock = Time.time;
            animator.Rebind();
            // Rebind enters the player's wake-up graph. Evaluate it invisibly with
            // wakeup/dead/sitting/block disabled BEFORE recording a neutral state.
            foreach (var p in parameters)
            {
                if (p.type == AnimatorControllerParameterType.Bool) animator.SetBool(p.nameHash, p.name == "onGround");
                else if (p.type == AnimatorControllerParameterType.Float)
                    animator.SetFloat(p.nameHash,
                        EquipmentParameter(p.name) ? sourceAnimator.GetFloat(p.nameHash) : 0f);
                else if (p.type == AnimatorControllerParameterType.Trigger) animator.ResetTrigger(p.nameHash);
                else if (p.type == AnimatorControllerParameterType.Int && EquipmentParameter(p.name))
                    animator.SetInteger(p.nameHash, sourceAnimator.GetInteger(p.nameHash));
            }

            for (int warmup = 0; warmup < 24; warmup++) animator.Update(0.25f);
            resting = new Snapshot
            {
                Aim = owner.GetLookDir(), Floats = new float[parameters.Length], Ints = new int[parameters.Length],
                Bools = new bool[parameters.Length], States = new AnimatorStateInfo[animator.layerCount],
                DrawStates = new bool[animator.layerCount],
                Weights = new float[animator.layerCount], Positions = new Vector3[bowBones.Count],
                Rotations = new Quaternion[bowBones.Count], Scales = new Vector3[bowBones.Count]
            };
            for (int i = 0; i < bowBones.Count; i++)
            {
                resting.Positions[i] = bowBones[i].Value.localPosition;
                resting.Rotations[i] = bowBones[i].Value.localRotation;
                resting.Scales[i] = bowBones[i].Value.localScale;
            }

            ReactionDelay = UnityEngine.Random.Range(HelOathGhostRules.ReactionMin, HelOathGhostRules.ReactionMax);
            Capture();
            Apply(snapshots[0], Vector3.zero, 0f);
        }

        private Transform CopyHierarchy(Transform source, Transform parent)
        {
            var go = new GameObject(source.name) { layer = source.gameObject.layer };
            Transform target = go.transform;
            target.SetParent(parent, false);
            target.localPosition = source.localPosition;
            target.localRotation = source.localRotation;
            target.localScale = source.localScale;
            bones.Add(source, target);
            for (int i = 0; i < source.childCount; i++) CopyHierarchy(source.GetChild(i), target);
            go.SetActive(source.gameObject.activeSelf);
            return target;
        }

        private Transform Remap(Transform original)
        {
            if (original == null) return null;
            Transform result;
            if (!bones.TryGetValue(original, out result))
                throw new InvalidOperationException("A skinned mesh references a bone outside Player.Visual: " +
                                                    original.name);
            return result;
        }

        internal void Detach()
        {
            // Resting.Aim started at spawn; keep the last delayed shot direction
            // when leaving owner playback so the final release does not turn away.
            if (mirroring && resting != null) resting.Aim = AimDirection;
            mirroring = false;
        }

        internal void CancelActions()
        {
            firePending = false;
            bowCycle.Cancel();
            queuedDraws.Clear();
            snapshots.Clear();
            sourceDrawing = false;
            Detach();
        }

        internal void Animate(Vector3 localVelocity, bool mirrorOwner, float dt)
        {
            if (animator == null) return;
            if (!mirrorOwner || sourceAnimator == null)
            {
                // Keep our own release/locomotion moving, but never sample another
                // owner draw, trigger or bow-bone pose after departure begins.
                Detach();
                Apply(resting, localVelocity, Mathf.Max(0f, Time.deltaTime));
                return;
            }

            if (!mirroring)
            {
                snapshots.Clear();
                playbackTime = -1f;
                lastCapture = -1f;
                resting.Time = Time.time - ReactionDelay;
                snapshots.Add(resting);
                mirroring = true;
            }

            // Shot scheduling also uses Time.time. Movement dt is capped by the
            // follower, so it cannot be used as an animation-history clock.
            clock = Time.time;
            bool drawing = hasDraw && sourceAnimator.GetBool(drawHash) && !SourceBlocking();
            bool changed = drawing != sourceDrawing;
            if (changed || clock - lastCapture >= 1f / 60f) Capture();
            // One fixed delay per body keeps short successive draws in chronological order.
            playbackTime = Mathf.Max(playbackTime, clock - ReactionDelay);
            Snapshot display = snapshots[0];
            for (int i = 1; i < snapshots.Count; i++)
            {
                if (snapshots[i].Time > playbackTime) break;
                display = snapshots[i];
            }

            Apply(display, localVelocity, Mathf.Max(0f, Time.deltaTime));
        }

        // Humanoid drives its equipped stance through statef/statei in this game
        // version. Preserve the bow's values even when the owner swaps to a hammer.
        private static bool EquipmentParameter(string name) => name == "statef" || name == "statei"
            || name.ToLowerInvariant().Contains("weapon");

        private static bool BlockParameter(string name)
        {
            string lower = name.ToLowerInvariant();
            return lower.Contains("block") || lower.Contains("defend");
        }

        private bool SourceBlocking()
        {
            foreach (var p in parameters)
            {
                if (!BlockParameter(p.name)) continue;
                if (p.type == AnimatorControllerParameterType.Bool && sourceAnimator.GetBool(p.nameHash)) return true;
                if (p.type == AnimatorControllerParameterType.Float &&
                    sourceAnimator.GetFloat(p.nameHash) > 0.01f) return true;
            }

            return false;
        }

        private bool BowState(AnimatorStateInfo state)
        {
            // Positive allow-list: never import arbitrary block, melee, emote, death or swim layers.
            return state.IsName(drawStateName) || state.IsName("bow_draw") || state.IsName("bow_fire")
                   || state.IsName("bow_release") || state.IsName("bow_shoot") || state.IsName(fireTrigger) ||
                   state.IsTag("bow");
        }

        private bool BowClips(int layer, bool next, bool drawOnly = false, Animator target = null)
        {
            // State names and trigger names need not match. Clip names let us retain
            // the actual release state instead of losing it at the draw -> fire transition.
            if (target == null) target = sourceAnimator;
            var clips = next ? target.GetNextAnimatorClipInfo(layer) : target.GetCurrentAnimatorClipInfo(layer);
            foreach (var info in clips)
            {
                if (info.clip == null || info.weight <= 0.001f) continue;
                string name = info.clip.name.ToLowerInvariant();
                if (name.Contains("bow") && (name.Contains("draw") || (!drawOnly && (name.Contains("fire")
                        || name.Contains("release") || name.Contains("shoot") ||
                        name.Contains("attack"))))) return true;
            }

            return false;
        }

        private bool DrawVisible()
        {
            for (int i = 0; i < animator.layerCount; i++)
            {
                if (i > 0 && animator.GetLayerWeight(i) <= 0.01f) continue;
                var state = animator.GetCurrentAnimatorStateInfo(i);
                if (state.IsName(drawStateName) || state.IsName("bow_draw") || BowClips(i, false, true, animator))
                    return true;
                if (animator.IsInTransition(i) && BowClips(i, true, true, animator)) return true;
            }

            return false;
        }

        private void RefreshBowSources()
        {
            var held = equipment != null ? LeftItemField?.GetValue(equipment) as GameObject : null;
            // Retain the spectral bow while the owner holds a hammer/shield. On
            // re-equipping, VisEquipment creates a NEW hierarchy for the same bow.
            if (held == null || held == boundHeldItem || held.name != heldItemName) return;
            boundHeldItem = held;
            for (int i = 0; i < bowBones.Count; i++)
            {
                Transform source = bowPaths[i].Length == 0 ? held.transform : held.transform.Find(bowPaths[i]);
                bowBones[i] = new KeyValuePair<Transform, Transform>(source, bowBones[i].Value);
            }
        }

        private void Capture()
        {
            RefreshBowSources();
            bool blocked = SourceBlocking();
            var frame = new Snapshot
            {
                Time = clock, Drawing = !blocked && hasDraw && sourceAnimator.GetBool(drawHash),
                Aim = owner.GetLookDir(), Floats = new float[parameters.Length], Ints = new int[parameters.Length],
                Bools = new bool[parameters.Length], States = new AnimatorStateInfo[animator.layerCount],
                DrawStates = new bool[animator.layerCount],
                Weights = new float[animator.layerCount], Positions = new Vector3[bowBones.Count],
                Rotations = new Quaternion[bowBones.Count], Scales = new Vector3[bowBones.Count]
            };
            for (int i = 0; i < parameters.Length; i++)
            {
                var p = parameters[i];
                if (BlockParameter(p.name)) continue;
                if (p.type == AnimatorControllerParameterType.Float)
                    frame.Floats[i] = sourceAnimator.GetFloat(p.nameHash);
                else if (p.type == AnimatorControllerParameterType.Int)
                    frame.Ints[i] = sourceAnimator.GetInteger(p.nameHash);
                else if (p.type == AnimatorControllerParameterType.Bool)
                    frame.Bools[i] = sourceAnimator.GetBool(p.nameHash);
            }

            for (int i = 0; i < animator.layerCount; i++)
            {
                var current = sourceAnimator.GetCurrentAnimatorStateInfo(i);
                bool currentBow = BowState(current) || BowClips(i, false);
                bool transitioning = sourceAnimator.IsInTransition(i);
                var next = transitioning ? sourceAnimator.GetNextAnimatorStateInfo(i) : default(AnimatorStateInfo);
                bool nextBow = transitioning && (BowState(next) || BowClips(i, true));
                bool currentDraw = current.IsName(drawStateName) || current.IsName("bow_draw") ||
                                   BowClips(i, false, true);
                bool nextDraw = transitioning &&
                                (next.IsName(drawStateName) || next.IsName("bow_draw") || BowClips(i, true, true));
                // A draw bool may change before the animator leaves its old fire state.
                // Never record that outgoing release as the next draw animation.
                if (!blocked && frame.Drawing && nextDraw)
                {
                    frame.States[i] = next;
                    frame.DrawStates[i] = true;
                }
                else if (!blocked && frame.Drawing && currentDraw)
                {
                    frame.States[i] = current;
                    frame.DrawStates[i] = true;
                }
                else if (!blocked && nextBow) frame.States[i] = next;
                else if (!blocked && currentBow) frame.States[i] = current;

                frame.Weights[i] = sourceAnimator.GetLayerWeight(i);
            }

            for (int i = 0; i < bowBones.Count; i++)
            {
                Transform source = bowBones[i].Key;
                // A rapid weapon swap can destroy the owner's old held-item hierarchy
                // while our fading render copy is still alive. Keep its last pose.
                if (source == null) source = bowBones[i].Value;
                if (source == null) continue;
                frame.Positions[i] = source.localPosition;
                frame.Rotations[i] = source.localRotation;
                frame.Scales[i] = source.localScale;
            }

            if (frame.Drawing && !sourceDrawing) sourceCycle++;
            sourceDrawing = frame.Drawing;
            frame.Cycle = sourceCycle;
            if (frame.Drawing) lastDrawing = frame;
            snapshots.Add(frame);
            if (snapshots.Count > 40) snapshots.RemoveAt(0);
            lastCapture = clock;
        }

        private void Apply(Snapshot frame, Vector3 velocity, float dt)
        {
            Snapshot pose = frame;
            int cycle = frame.Cycle;
            bool drawing = frame.Drawing;
            if (queuedDraws.Count > 0)
            {
                var next = queuedDraws[0];
                if (frame.Cycle != next.Cycle || !frame.Drawing)
                {
                    drawing = Time.time >= next.Due - HelOathGhostBowCycle.MinimumDraw;
                    if (drawing) pose = next.Pose;
                }

                cycle = next.Cycle;
            }

            drawing = drawing && bowCycle.Draw(cycle, Time.time, 0f);
            if (!drawing) pose = frame;
            bool ownRelease = bowCycle.Releasing(Time.time);
            IsAiming = drawing || ownRelease;
            AimDirection = ownRelease ? releaseAim : pose.Aim;
            for (int i = 0; i < parameters.Length; i++)
            {
                var p = parameters[i];
                bool block = BlockParameter(p.name);
                if (p.type == AnimatorControllerParameterType.Float)
                {
                    float value = EquipmentParameter(p.name) ? weaponFloats[i]
                        : p.name == "forward_speed" ? velocity.z
                        : p.name == "sideway_speed" ? velocity.x
                        : !block && p.name.ToLowerInvariant().Contains("draw") ? pose.Floats[i] : 0f;
                    if (p.name == "forward_speed" || p.name == "sideway_speed")
                        animator.SetFloat(p.nameHash, value, 0.12f, dt);
                    else animator.SetFloat(p.nameHash, value);
                }
                else if (p.type == AnimatorControllerParameterType.Int)
                    animator.SetInteger(p.nameHash, !block && EquipmentParameter(p.name) ? weaponValues[i] : 0);
                else if (p.type == AnimatorControllerParameterType.Bool)
                    animator.SetBool(p.nameHash,
                        !block && (p.name == "onGround" || (p.nameHash == drawHash && drawing)));
                else if (p.type == AnimatorControllerParameterType.Trigger) animator.ResetTrigger(p.nameHash);
            }

            if (firePending)
            {
                if (hasFireTrigger) animator.SetTrigger(fireHash);
                firePending = false;
            }

            // Advance locomotion every frame. Sampling an upper bow layer must not
            // freeze feet by evaluating the entire controller at delta zero only.
            animator.Update(Mathf.Max(0f, dt));
            bool copiedBowState = false;
            for (int i = 0; i < animator.layerCount; i++)
            {
                var state = pose.States[i];
                bool bow = state.fullPathHash != 0 && animator.HasState(i, state.fullPathHash);
                if (bow && ((drawing && pose.Drawing && pose.DrawStates[i])
                            || (ownRelease && !hasFireTrigger && !pose.Drawing &&
                                pose.Cycle == bowCycle.ReleasedCycle)))
                {
                    IsAiming = true;
                    copiedBowState = true;
                    animator.SetLayerWeight(i, pose.Weights[i]);
                    animator.Play(state.fullPathHash, i, state.normalizedTime);
                }
                else
                {
                    // Keep the current action visible; a new cycle may interrupt
                    // recovery, while a stale source draw may not restart it.
                    float weight = i == 0
                        ? 1f
                        : drawing || ownRelease
                            ? Mathf.Max(pose.Weights[i], animator.GetLayerWeight(i))
                            : Mathf.MoveTowards(animator.GetLayerWeight(i), 0f, dt * 5f);
                    animator.SetLayerWeight(i, weight);
                }
            }

            if (copiedBowState) animator.Update(0f);
            // Count only evaluated draw poses. A bool alone does not mean the
            // controller has left its previous release/transition yet.
            if (drawing && DrawVisible()) bowCycle.Draw(cycle, Time.time, dt);
            // Only weapon transforms use sampled poses. Character bones remain under our safe animator.
            for (int i = 0; i < bowBones.Count; i++)
            {
                Transform target = bowBones[i].Value;
                if (target == null) continue;
                target.localPosition = pose.Positions[i];
                target.localRotation = pose.Rotations[i];
                target.localScale = pose.Scales[i];
            }
        }
    }
}