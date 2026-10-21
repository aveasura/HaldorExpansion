using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace HaldorExpansion.Features.HelOath
{
    // Quality-10 awakening: gameplay exists only on the real owner; companions are render-only.
    internal static class HelOathGhostPreview
    {
        private const string CountKey = "he_hel_ghost_count_v2";
        private const string VisibleKey = "he_hel_ghost_visible_v3";
        private static readonly HelOathGhostState State = new HelOathGhostState();
        private static readonly HelOathGhostRoster Roster = new HelOathGhostRoster(HelOathGhostRules.Limit);
        private static readonly string[] BodyKeys = { "he_hel_body_0_v1", "he_hel_body_1_v1", "he_hel_body_2_v1" };

        internal static string BodyToken(Player player, int index)
        {
            if (player == Player.m_localPlayer) return Roster.Token(index);
            var view = HelOathNetwork.View(player);
            return view != null && view.IsValid() ? view.GetZDO().GetString(BodyKeys[index], "") : "";
        }

        private static ItemDrop.ItemData weapon;
        private static string soulEpoch = Guid.NewGuid().ToString("N");

        internal static string SoulEpoch(Player player)
        {
            Adopt(player);
            return soulEpoch;
        }

        private static Player owner;
        private static bool renderFailureLogged;

        internal static bool Eligible(Player player) => player != null && !player.IsDead()
                                                                       && !player.IsTeleporting() &&
                                                                       !player.InCutscene() &&
                                                                       HelOathRuntime.Equipped(player)
                                                                       && player.GetCurrentWeapon().m_quality >=
                                                                       HelOathGhostRules.RequiredQuality;

        internal static void Register(Player player, ZNetView view)
        {
            // Session-only test state must not survive reconnects or loading a saved player ZDO.
            if (view.IsOwner())
            {
                view.GetZDO().Set(CountKey, 0);
                view.GetZDO().Set(VisibleKey, false);
            }

            if (player.GetComponent<HelOathOwnerCleanup>() == null)
                player.gameObject.AddComponent<HelOathOwnerCleanup>().Owner = player;
        }

        internal static void ResetOwner(Player player)
        {
            if (!ReferenceEquals(owner, player)) return;
            State.Reset();
            Roster.Update(0);
            owner = null;
            weapon = null;
            soulEpoch = Guid.NewGuid().ToString("N");
        }

        internal static void OnConfirmedHit(Player player, string epoch, float damage)
        {
            if (player == null || player != Player.m_localPlayer) return;
            Adopt(player);
            if (epoch != soulEpoch) return;
            State.Hit(Time.time, Eligible(player), damage, HelOathConfiguration.RequiredDamage);
            Publish(player);
        }

        private static void Adopt(Player player)
        {
            bool alive = player != null && !player.IsDead() && !player.IsTeleporting() && !player.InCutscene();
            var current = Eligible(player) ? player.GetCurrentWeapon() : null;
            bool changedOwner = !ReferenceEquals(owner, player);
            if (changedOwner || !alive)
            {
                State.Reset();
                Roster.Update(0);
            }

            if (changedOwner || !ReferenceEquals(weapon, current))
            {
                soulEpoch = Guid.NewGuid().ToString("N");
                player?.GetComponent<HelOathGhostCompanions>()?.CancelShots();
            }

            owner = player;
            weapon = current;
        }

        private static void Publish(Player player)
        {
            Roster.Update(State.Count);
            var view = HelOathNetwork.View(player);
            if (view == null || !view.IsValid() || !view.IsOwner()) return;
            if (view.GetZDO().GetInt(CountKey) != State.Count) view.GetZDO().Set(CountKey, State.Count);
            bool visible = Eligible(player);
            if (view.GetZDO().GetBool(VisibleKey) != visible) view.GetZDO().Set(VisibleKey, visible);
            for (int i = 0; i < BodyKeys.Length; i++)
                if (view.GetZDO().GetString(BodyKeys[i], "") != Roster.Token(i))
                    view.GetZDO().Set(BodyKeys[i], Roster.Token(i));
        }

        internal static void UpdatePlayer(Player player)
        {
            if (player == null) return;
            if (player == Player.m_localPlayer)
            {
                Adopt(player);
                State.Update(Time.time, Eligible(player));
                Publish(player);
            }

            // A dedicated server maintains the normal damage credit path, but never builds meshes.
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) return;
            var view = HelOathNetwork.View(player);
            // Remote inventories are not authoritative/synchronized; the owner's ZDO
            // already encodes quality/equipment eligibility. Do not inspect their bow.
            int count = view != null && view.IsValid() && !player.IsDead() && !player.IsTeleporting()
                ? Mathf.Clamp(view.GetZDO().GetInt(CountKey), 0, HelOathGhostRules.Limit)
                : 0;
            var companions = player.GetComponent<HelOathGhostCompanions>();
            bool visible = view != null && view.IsValid() && view.GetZDO().GetBool(VisibleKey);
            if ((count == 0 || !visible) && companions == null) return;
            if (companions == null) companions = player.gameObject.AddComponent<HelOathGhostCompanions>();
            companions.SetCount(player, count, visible);
        }

        internal static void OnShot(Projectile projectile, HelOathShot shot)
        {
            Player player = shot.Owner;
            if (player != Player.m_localPlayer || !Eligible(player) || shot.Echo) return;
            Adopt(player);
            State.Update(Time.time, true);
            int count = State.Count;
            if (count == 0) return;
            Publish(player);
            var companions = player.GetComponent<HelOathGhostCompanions>();
            if (companions == null) companions = player.gameObject.AddComponent<HelOathGhostCompanions>();
            companions.SetCount(player, count, true);
            bool release = shot.Special;
            if (release)
            {
                State.Consume();
                companions.InterruptForUltimate();
            }

            // Queued awakened arrows belong to the new generation; earlier ordinary
            // echoes are invalidated immediately, so souls cannot be spent twice.
            companions.Schedule(HelOathGhostShot.Capture(projectile, shot), count);
            // Schedule against the old body identities before publishing their
            // removal. The pending shots retain those identities through departure.
            if (release) Publish(player);
        }

        internal static void LogRenderFailure(Exception error)
        {
            if (renderFailureLogged) return;
            renderFailureLogged = true;
            HaldorExpansionPlugin.Log?.LogWarning("[Hel Ghost Preview] Could not create spectral visual: " + error);
        }
    }

    internal sealed class HelOathOwnerCleanup : MonoBehaviour
    {
        internal Player Owner;
        private void OnDisable() => HelOathGhostPreview.ResetOwner(Owner);
        private void OnDestroy() => HelOathGhostPreview.ResetOwner(Owner);
    }

    // Attached to the real player's object solely for lifecycle cleanup; ghosts themselves are visual-only.
    internal sealed class HelOathGhostCompanions : MonoBehaviour
    {
        // ZDO count can arrive before the delayed arrow RPC. This bounded wait
        // happens before fading; it never restarts an already dissolving body.
        private const float RemoteReleaseGrace = HelOathGhostRules.ReactionMax + 0.5f;
        private readonly HelOathGhostVisual[] ghosts = new HelOathGhostVisual[HelOathGhostRules.Limit];
        private readonly List<HelOathGhostVisual> retiring = new List<HelOathGhostVisual>();
        private Player owner;
        private int count;
        private bool visible;
        private Vector3 previousOwnerPosition;
        private Vector3 ownerVelocity;
        private float retryAt;

        private Quaternion formationRotation;

        // Shared combat target for slot 0 (the Guardian), even when there is only one soul.
        private Character nearestThreat;
        private float nextThreatScan;
        private bool initialized;
        internal bool HasThreat => nearestThreat != null && !nearestThreat.IsDead();
        internal Vector3 ThreatPosition => HasThreat ? nearestThreat.GetCenterPoint() : Vector3.zero;

        private void UpdateThreat(Player player)
        {
            if (Time.time < nextThreatScan) return;
            nextThreatScan = Time.time + 0.35f;
            nearestThreat = null;
            if (count <= 0 || !visible) return;
            float nearest = 22f * 22f;
            foreach (Character character in Character.GetAllCharacters())
            {
                if (character == null || character.IsDead() || character.IsPlayer()) continue;
                Vector3 offset = character.transform.position - player.transform.position;
                if (Mathf.Abs(offset.y) > 7f) continue;
                offset.y = 0f;
                if (offset.sqrMagnitude >= nearest || !BaseAI.IsEnemy(player, character)) continue;
                nearest = offset.sqrMagnitude;
                nearestThreat = character;
            }
        }

        // Soul Swap is an intentional tactical relocation, not an automatic
        // invulnerability/safety selector. Threat proximity never vetoes a swap.
        // Formation uses terrain and line-of-fire checks independently.
        // One press prioritizes the Guardian, then any materialized soul.
        internal bool TrySelectSwap(out int selected, out string token, out Vector3 landing)
        {
            selected = -1;
            token = null;
            landing = Vector3.zero;
            if (owner == null || owner != Player.m_localPlayer || !visible
                || !HelOathGhostPreview.Eligible(owner)) return false;
            int[] preference = { HelOathGhostMotion.GuardianIndex, 1, 2 };
            foreach (int i in preference)
            {
                if (i >= count) continue;
                var ghost = ghosts[i];
                Vector3 at;
                // No distance, height, enemy, ground or animation-state veto.
                // The spectral body itself is the destination; no idealized
                // "safe zone" must be found before accepting the button press.
                if (ghost == null || !ghost.TrySwapLanding(out at)) continue;
                selected = i;
                token = ghost.Token;
                landing = at;
                return true;
            }

            return false;
        }

        internal void CompleteSwap(int selected, string token, Vector3 oldOwnerPosition)
        {
            if (selected < 0 || selected >= ghosts.Length) return;
            var ghost = ghosts[selected];
            if (ghost == null || ghost.Token != token) return;
            ghost.RelocateForSwap(oldOwnerPosition);
            // Ignore instantaneous owner displacement when computing pursuit velocity.
            previousOwnerPosition = owner.transform.position;
            ownerVelocity = Vector3.zero;
            nextThreatScan = 0f;
        }

        internal void RemoteSwap(int selected, string token, Vector3 oldOwnerPosition)
        {
            if (selected < 0 || selected >= ghosts.Length) return;
            var ghost = ghosts[selected];
            if (ghost != null && ghost.Token == token && !ghost.Retiring)
            {
                ghost.RelocateForSwap(oldOwnerPosition);
                previousOwnerPosition = owner.transform.position;
                ownerVelocity = Vector3.zero;
            }
        }

        private readonly List<PendingShot> pending = new List<PendingShot>();

        private sealed class PendingShot
        {
            internal HelOathGhostVisual Ghost;
            internal HelOathGhostShot Shot;
            internal float At;
            internal float Expires; // Only the terminal ultimate has a recall deadline.
            internal int Index;
            internal int Cycle;
        }

        private readonly List<RemotePending> remotePending = new List<RemotePending>();

        private sealed class RemotePending
        {
            internal HelOathGhostVisual Ghost;
            internal int Cycle;
            internal Vector3 Direction;
            internal bool Special;
            internal Action ShowArrow;
            internal float Expires;
        }

        private int PeerCount => ghosts.Length + retiring.Count;
        private HelOathGhostVisual Peer(int i) => i < ghosts.Length ? ghosts[i] : retiring[i - ghosts.Length];

        internal bool HasRoom(HelOathGhostVisual self, Vector3 point, bool reservations,
            float clearance = HelOathGhostSpacing.Preferred)
        {
            for (int i = 0; i < PeerCount; i++)
            {
                var peer = Peer(i);
                if (peer == null || peer == self || !peer.ReservesSpace) continue;
                Vector3 delta = point - peer.Position;
                if (Mathf.Abs(delta.y) < HelOathGhostSpacing.HeightTolerance
                    && delta.x * delta.x + delta.z * delta.z < clearance * clearance) return false;
                if (!reservations || !peer.HasReservedTarget) continue;
                delta = point - peer.ReservedTarget;
                if (Mathf.Abs(delta.y) < HelOathGhostSpacing.HeightTolerance
                    && delta.x * delta.x + delta.z * delta.z < clearance * clearance) return false;
            }

            return true;
        }

        internal bool CanStep(HelOathGhostVisual self, Vector3 from, Vector3 to)
        {
            for (int i = 0; i < PeerCount; i++)
            {
                var peer = Peer(i);
                if (peer == null || peer == self || !peer.ReservesSpace) continue;
                Vector3 other = peer.Position;
                if (Mathf.Min(Mathf.Abs(from.y - other.y), Mathf.Abs(to.y - other.y)) >=
                    HelOathGhostSpacing.HeightTolerance) continue;
                if (!HelOathGhostSpacing.CanMove(from.x, from.z, to.x, to.z, other.x, other.z)) return false;
            }

            return true;
        }

        internal Vector3 Separation(HelOathGhostVisual self)
        {
            Vector3 result = Vector3.zero;
            for (int i = 0; i < PeerCount; i++)
            {
                var peer = Peer(i);
                if (peer == null || peer == self || !peer.ReservesSpace) continue;
                Vector3 delta = self.Position - peer.Position;
                if (Mathf.Abs(delta.y) >= HelOathGhostSpacing.HeightTolerance) continue;
                delta.y = 0f;
                float distance = delta.magnitude;
                if (distance >= HelOathGhostSpacing.Minimum + 0.08f) continue;
                if (distance < 0.001f)
                    delta = Quaternion.Euler(0f, self.Index * 120f, 0f) * Vector3.forward;
                result += delta.normalized * (HelOathGhostSpacing.Preferred - distance);
            }

            return result;
        }

        internal void CancelShots()
        {
            foreach (var shot in pending) shot.Ghost.CancelShot(shot.Cycle);
            pending.Clear();
        }

        internal void InterruptForUltimate()
        {
            CancelShots();
            // Only the ultimate preempts a body's current draw or release.
            // It must not wait behind its previous owner's normal arrows.
            foreach (var ghost in ghosts) ghost?.InterruptForUltimate();
            foreach (var ghost in retiring) ghost.InterruptForUltimate();
        }

        private void DropShot(int index)
        {
            pending[index].Ghost.CancelShot(pending[index].Cycle);
            pending.RemoveAt(index);
        }

        internal void ReleaseAll()
        {
            CancelShots();
            count = 0;
            foreach (var ghost in ghosts) ghost?.Retire();
        }

        internal bool RemoteShot(int index, string token, bool special, Vector3 direction, Action showArrow)
        {
            if (index < 0 || index >= ghosts.Length || string.IsNullOrEmpty(token)) return false;
            HelOathGhostVisual selected = null;
            foreach (var ghost in retiring)
                if (ghost.Index == index && ghost.Token == token)
                {
                    if (special) selected = ghost;
                    break;
                }

            if (selected == null)
            {
                var active = ghosts[index];
                if (active != null && active.Token == token && (special || !active.Retiring)) selected = active;
            }

            if (selected == null || !selected.CanKeepShot) return false;
            if (special)
            {
                for (int i = remotePending.Count - 1; i >= 0; i--)
                    if (remotePending[i].Ghost == selected)
                    {
                        selected.CancelShot(remotePending[i].Cycle);
                        remotePending.RemoveAt(i);
                    }

                selected.InterruptForUltimate();
                selected.Retire(RemoteReleaseGrace);
                // The count/ZDO update may already have marked this body retiring.
                // Preserve a fresh bounded interval for the actual final-shot RPC.
                selected.HoldFinalShot(RemoteReleaseGrace);
            }

            if (!selected.HasQueuedShots) selected.PrepareShot(selected.ShotOrigin + direction * 100f);
            int cycle = selected.QueueShot(Time.time, direction);
            remotePending.Add(new RemotePending
            {
                Ghost = selected, Cycle = cycle, Direction = direction,
                Special = special, ShowArrow = showArrow, Expires = special ? Time.time + 2.5f : float.PositiveInfinity
            });
            return true;
        }

        internal void Schedule(HelOathGhostShot shot, int desired)
        {
            for (int i = 0; i < desired; i++)
            {
                // No finite burst cap: every owner shot enters each body's FIFO.
                EnsureGhost(i);
                if (ghosts[i] == null) continue;
                if (shot.Special) ghosts[i].FinishRecall(owner, formationRotation, true);
                if (!ghosts[i].HasQueuedShots) ghosts[i].PrepareShot(shot.AimPoint);
                if (shot.Special) ghosts[i].Retire();
                float due = Time.time + ghosts[i].ReactionDelay;
                int cycle = ghosts[i].QueueShot(due, shot.AimPoint - ghosts[i].ShotOrigin);
                pending.Add(new PendingShot
                {
                    Ghost = ghosts[i], Shot = shot, Cycle = cycle,
                    At = due, Expires = shot.Special ? due + 3f : float.PositiveInfinity,
                    Index = i
                });
            }

            if (shot.Special) count = 0;
        }

        private void EnsureGhost(int i)
        {
            string token = HelOathGhostPreview.BodyToken(owner, i);
            // A final-shot RPC may beat the ZDO removal. The old synchronized
            // token is not a newly earned soul and must not create a replacement.
            if (ghosts[i] != null && ghosts[i].Retiring && ghosts[i].Token == token) return;
            if (ghosts[i] != null && (ghosts[i].Retiring || ghosts[i].Token != token))
            {
                ghosts[i].Retire(owner == Player.m_localPlayer ? 0f : RemoteReleaseGrace);
                if (retiring.Count >= 6)
                {
                    var oldest = retiring[0];
                    pending.RemoveAll(p => p.Ghost == oldest);
                    oldest.Dispose();
                    retiring.RemoveAt(0);
                }

                retiring.Add(ghosts[i]);
                ghosts[i] = null;
            }

            // A quick reequip may schedule a shot before LateUpdate restores visibility.
            // Unspent bodies may return; a spent body was moved out above.
            if (ghosts[i] != null)
            {
                ghosts[i].Reappear();
                return;
            }

            if (string.IsNullOrEmpty(token) || Time.time < retryAt) return;
            try
            {
                ghosts[i] = HelOathGhostVisual.Create(owner, i, formationRotation, token, this);
                if (ghosts[i] == null) retryAt = Time.time + 0.5f;
            }
            catch (Exception error)
            {
                HelOathGhostPreview.LogRenderFailure(error);
                retryAt = Time.time + 10f;
            }
        }

        internal void SetCount(Player player, int desired, bool show)
        {
            owner = player;
            for (int i = desired; i < ghosts.Length; i++)
                ghosts[i]?.Retire(player == Player.m_localPlayer ? 0f : RemoteReleaseGrace);
            count = desired;
            visible = show;
            if (!initialized)
            {
                formationRotation = player.transform.rotation;
                previousOwnerPosition = player.transform.position;
                initialized = true;
            }
        }

        private void LateUpdate()
        {
            if (owner == null)
            {
                Destroy(this);
                return;
            }

            var view = HelOathNetwork.View(owner);
            if (view == null || !view.IsValid() || owner.IsDead() || owner.IsTeleporting()) count = 0;
            float dt = Mathf.Min(Time.deltaTime, 0.1f);
            // Camera/aim rotation never moves the formation. Only actual travel changes its heading.
            Vector3 travel = owner.transform.position - previousOwnerPosition;
            previousOwnerPosition = owner.transform.position;
            travel.y = 0f;
            Vector3 measured = Time.deltaTime > 0.0001f ? travel / Time.deltaTime : Vector3.zero;
            ownerVelocity = Vector3.Lerp(ownerVelocity, Vector3.ClampMagnitude(measured, 16f),
                1f - Mathf.Exp(-10f * dt));
            if (ownerVelocity.sqrMagnitude > 0.36f)
                formationRotation = Quaternion.Slerp(formationRotation, Quaternion.LookRotation(ownerVelocity),
                    1f - Mathf.Exp(-3f * dt));
            UpdateThreat(owner);
            // FIFO per body: two due arrows must never collapse into one fire trigger.
            for (int i = 0; i < pending.Count;)
            {
                PendingShot p = pending[i];
                // A soul naturally decaying must not erase an arrow that was
                // already accepted into this body's FIFO. Owner/equipment loss
                // still cancels unsafe work; the ultimate explicitly preempts it.
                if (!HelOathGhostPreview.Eligible(owner)
                    || !ReferenceEquals(p.Shot.Weapon, owner.GetCurrentWeapon()))
                {
                    DropShot(i);
                    continue;
                }

                if (p.Shot.Special && p.Ghost.Recalling && Time.time < p.Expires)
                {
                    p.Ghost.FinishRecall(owner, formationRotation, true);
                    if (p.Ghost.Recalling)
                    {
                        i++;
                        continue;
                    }
                }

                if (!p.Ghost.CanKeepShot)
                {
                    DropShot(i);
                    continue;
                }

                if (Time.time < p.At)
                {
                    i++;
                    continue;
                }

                // Ordinary actions never expire while waiting for their own draw,
                // release or a clear firing lane; only the spent soul has a bound.
                if (p.Shot.Special && Time.time > p.Expires)
                {
                    DropShot(i);
                    continue;
                }

                // The line of fire is a positioning preference, never a prerequisite
                // for finishing an accepted arrow. When the owner sprints away, the
                // old aim point can remain behind terrain while this body switches
                // to follow mode (where it cannot reposition for that old point).
                // Waiting for ClearShot here permanently locks its bow in the draw
                // pose until the owner happens to shoot again. Fire on the body's
                // independent clock instead: the projectile's normal collision
                // handles any wall in its path, just as it does for owner shots.
                // Do not shoot into an intermediate dungeon floor. Keep the
                // action in FIFO until the visual rejoins the owner's level.
                // Ultimate remains the one action permitted to preempt this.
                if (!p.Shot.Special && !p.Ghost.OnOwnerLevel(owner.transform.position))
                {
                    i++;
                    continue;
                }

                Vector3 origin = p.Ghost.ShotOrigin;
                if (!p.Ghost.Fired(p.Shot.AimPoint - origin, p.Cycle))
                {
                    i++;
                    continue;
                }

                pending.RemoveAt(i);
                // Promote the next FIFO aim only after the previous arrow releases.
                // Later owner shots must not redirect the head while it draws.
                for (int next = i; next < pending.Count; next++)
                    if (pending[next].Ghost == p.Ghost)
                    {
                        p.Ghost.PrepareShot(pending[next].Shot.AimPoint);
                        break;
                    }

                // Fired() can finish turning the render body this frame. Use
                // its post-turn bow origin rather than the pre-turn offset.
                HelOathGhostBolt.Launch(owner, p.Ghost.ShotOrigin, p.Shot, p.Index, p.Ghost.Token);
            }

            for (int i = 0; i < remotePending.Count;)
            {
                var p = remotePending[i];
                if (!p.Ghost.CanKeepShot || Time.time > p.Expires)
                {
                    p.Ghost.CancelShot(p.Cycle);
                    remotePending.RemoveAt(i);
                    continue;
                }

                if (!p.Special && !p.Ghost.OnOwnerLevel(owner.transform.position))
                {
                    i++;
                    continue;
                }

                if (!p.Ghost.Fired(p.Direction, p.Cycle))
                {
                    i++;
                    continue;
                }

                remotePending.RemoveAt(i);
                for (int next = i; next < remotePending.Count; next++)
                    if (remotePending[next].Ghost == p.Ghost)
                    {
                        p.Ghost.PrepareShot(p.Ghost.ShotOrigin + remotePending[next].Direction * 100f);
                        break;
                    }

                p.ShowArrow();
            }

            bool hasVisual = false;
            for (int i = 0; i < ghosts.Length; i++)
            {
                if (i < count && visible) EnsureGhost(i);
                if (ghosts[i] == null) continue;
                if (!ghosts[i].Tick(owner, formationRotation,
                        (i < count && visible) || pending.Exists(p => p.Ghost == ghosts[i]) ||
                        remotePending.Exists(p => p.Ghost == ghosts[i]), i >= count, ownerVelocity, dt))
                {
                    ghosts[i].Dispose();
                    ghosts[i] = null;
                }
                else hasVisual = true;
            }

            for (int i = retiring.Count - 1; i >= 0; i--)
            {
                var ghost = retiring[i];
                if (!ghost.Tick(owner, formationRotation,
                        pending.Exists(p => p.Ghost == ghost) || remotePending.Exists(p => p.Ghost == ghost), true,
                        ownerVelocity, dt))
                {
                    ghost.Dispose();
                    retiring.RemoveAt(i);
                }
                else hasVisual = true;
            }

            if ((count == 0 || !visible) && !hasVisual) Destroy(this);
        }

        private void ClearVisuals()
        {
            CancelShots();
            remotePending.Clear();
            foreach (var ghost in retiring) ghost.Dispose();
            retiring.Clear();
            for (int i = 0; i < ghosts.Length; i++)
            {
                ghosts[i]?.Dispose();
                ghosts[i] = null;
            }
        }

        // Visual roots live in world space: disabling their owner must also hide/destroy them.
        private void OnDisable() => ClearVisuals();
        private void OnDestroy() => ClearVisuals();
    }
}