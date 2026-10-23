using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering;

namespace HaldorExpansion.Features.HelOath
{
    // Immutable owner-shot payload. No ammunition, skill gain, weapon wear or copied scripts.
    internal sealed class HelOathGhostShot
    {
        private static readonly FieldInfo VelocityField = AccessTools.Field(typeof(Projectile), "m_vel");
        internal ItemDrop.ItemData Weapon;
        internal HitData.DamageTypes Damage;
        internal Vector3 AimPoint;
        internal Vector3 Velocity;
        internal float Gravity;
        internal float Push;
        internal float Backstab;
        internal bool Special;
        internal bool TouchV;
        internal float Splash;
        internal float Radius;
        internal string Epoch;

        internal static HelOathGhostShot Capture(Projectile projectile, HelOathShot source)
        {
            Vector3 velocity = (Vector3)VelocityField.GetValue(projectile);
            Vector3 aim = HelOathGhostBolt.CaptureAim(projectile.transform.position,
                velocity.normalized, source.Owner);

            float multiplier = source.Special
                ? HelOathGhostRules.ActiveDamageMultiplier
                : HelOathGhostRules.NormalDamageMultiplier;
            var damage = projectile.m_damage;
            damage.Modify(multiplier);
            return new HelOathGhostShot
            {
                Weapon = source.Owner.GetCurrentWeapon(), Damage = damage,
                AimPoint = aim, Velocity = velocity, Gravity = projectile.m_gravity,
                Push = projectile.m_attackForce * multiplier, Backstab = projectile.m_backstabBonus,
                Special = source.Special, TouchV = source.TouchV, Splash = source.Splash * multiplier,
                Radius = source.Radius, Epoch = source.Epoch
            };
        }
    }

    // This is a local visual bolt, NOT a Player, Character, AI or ZNetView. Only the
    // shooting client's copy owns a payload and can send authoritative damage RPCs.
    internal sealed class HelOathGhostBolt : MonoBehaviour
    {
        private const string VisualRpc = "HE_HelGhostArrowFx_v4";

        internal static readonly int CollisionMask = LayerMask.GetMask("Default", "static_solid", "Default_small",
            "piece", "piece_nonsolid", "terrain", "vehicle", "character", "character_net", "character_ghost", "hitbox");

        private Player owner;
        private HelOathGhostShot shot;
        private Vector3 velocity;
        private float gravity;
        private float age;
        private bool empowered;
        private Vector3 terminalPoint;
        private float flightRemaining;

        // Queries run synchronously on Unity's main thread. Grow only when a
        // crowded scene fills a buffer; never silently lose a later character.
        private const int MaxCachedHits = 512;
        private static RaycastHit[] castHits = new RaycastHit[32];
        private static Collider[] overlapHits = new Collider[16];
        private LineRenderer shaft;
        private TrailRenderer trail;
        private Material material;
        private bool finished;
        private bool originChecked;

        internal static void Register(Player player, ZNetView view)
        {
            view.Register<ZPackage>(VisualRpc, (sender, packet) =>
            {
                if (!view.IsValid() || sender != view.GetZDO().GetOwner() || view.IsOwner()) return;
                if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) return;
                Vector3 position = packet.ReadVector3();
                Vector3 speed = packet.ReadVector3();
                float fall = packet.ReadSingle();
                bool empowered = packet.ReadBool();
                int ghostIndex = packet.ReadInt();
                string bodyToken = packet.ReadString();
                Vector3 aimPoint = packet.ReadVector3();
                float flightDuration = packet.ReadSingle();
                Guid parsedToken;
                if (bodyToken == null || bodyToken.Length != 32 ||
                    !Guid.TryParseExact(bodyToken, "N", out parsedToken)) return;
                if (ghostIndex < 0 || ghostIndex >= HelOathGhostRules.Limit) return;
                if (!Finite(position) || !Finite(speed) || !Finite(fall) || speed.sqrMagnitude > 1000000f
                    || fall < 0f || fall > 100f || (position - player.transform.position).sqrMagnitude > 900f) return;
                if (!Finite(aimPoint) || !Finite(flightDuration) ||
                    (aimPoint - position).sqrMagnitude > 200f * 200f ||
                    (empowered ? flightDuration < 0.025f || flightDuration > 8f : flightDuration != 0f)) return;
                var companions = player.GetComponent<HelOathGhostCompanions>();
                if (companions == null || !companions.RemoteShot(ghostIndex, bodyToken, empowered, speed.normalized,
                        () => Create(player, position, speed, fall, empowered, null, aimPoint, flightDuration)))
                    Create(player, position, speed, fall, empowered, null, aimPoint, flightDuration);
            });
        }

        internal static void Launch(Player player, Vector3 origin, HelOathGhostShot shot, int ghostIndex,
            string bodyToken)
        {
            if (player != Player.m_localPlayer || !HelOathGhostPreview.Eligible(player)) return;
            float speed = shot.Velocity.magnitude;
            if (!Finite(speed) || speed < 0.1f) return;
            Vector3 displacement = shot.AimPoint - origin;
            Vector3 velocity = displacement.normalized * speed;
            float flightDuration = 0f;
            if (shot.Special)
            {
                // Solve the arc for this exact endpoint. Scenery on the way is
                // intangible, but the selected ground/target point ends the shot.
                flightDuration = Mathf.Clamp(displacement.magnitude / speed, 0.025f, 8f);
                velocity = displacement / flightDuration;
                velocity.y += shot.Gravity * flightDuration * 0.5f;
            }
            else
            {
                float travel = Mathf.Clamp(displacement.magnitude / speed, 0f, 3f);
                velocity.y += shot.Gravity * travel * 0.5f;
            }

            Create(player, origin, velocity, shot.Gravity, shot.Special, shot, shot.AimPoint, flightDuration);
            var view = HelOathNetwork.View(player);
            if (view == null || !view.IsValid() || !view.IsOwner()) return;
            var packet = new ZPackage();
            HelOathNetwork.Write(packet, origin);
            HelOathNetwork.Write(packet, velocity);
            HelOathNetwork.Write(packet, shot.Gravity);
            HelOathNetwork.Write(packet, shot.Special);
            HelOathNetwork.Write(packet, ghostIndex);
            HelOathNetwork.Write(packet, bodyToken);
            HelOathNetwork.Write(packet, shot.AimPoint);
            HelOathNetwork.Write(packet, flightDuration);
            view.InvokeRPC(ZNetView.Everybody, VisualRpc, packet);
        }

        private static void Create(Player owner, Vector3 position, Vector3 velocity, float gravity,
            bool empowered, HelOathGhostShot payload, Vector3 aimPoint, float flightDuration)
        {
            var root = new GameObject("HE_HelSpectralArrow");
            root.transform.position = position;
            var bolt = root.AddComponent<HelOathGhostBolt>();
            bolt.owner = owner;
            bolt.velocity = velocity;
            bolt.gravity = gravity;
            bolt.shot = payload;
            bolt.empowered = empowered;
            bolt.terminalPoint = aimPoint;
            bolt.flightRemaining = flightDuration;
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null) return; // Collision simulation still works without a renderer.
            bolt.material = new Material(shader) { color = new Color(0.18f, 0.80f, 0.68f, 0.7f) };
            bolt.shaft = root.AddComponent<LineRenderer>();
            bolt.shaft.sharedMaterial = bolt.material;
            bolt.shaft.positionCount = 2;
            bolt.shaft.useWorldSpace = true;
            bolt.shaft.startWidth = empowered ? 0.065f : 0.035f;
            bolt.shaft.endWidth = 0.008f;
            bolt.shaft.shadowCastingMode = ShadowCastingMode.Off;
            bolt.shaft.receiveShadows = false;
            bolt.trail = root.AddComponent<TrailRenderer>();
            bolt.trail.sharedMaterial = bolt.material;
            bolt.trail.time = 0.18f;
            bolt.trail.minVertexDistance = 0.08f;
            bolt.trail.startWidth = empowered ? 0.13f : 0.07f;
            bolt.trail.endWidth = 0f;
            bolt.trail.startColor = new Color(0.2f, 0.9f, 0.7f, 0.35f);
            bolt.trail.endColor = new Color(0.1f, 0.6f, 0.5f, 0f);
            bolt.trail.shadowCastingMode = ShadowCastingMode.Off;
            bolt.trail.receiveShadows = false;
            bolt.Draw();
        }

        private void Update()
        {
            if (finished) return;
            if (owner == null || !owner.gameObject.activeInHierarchy || owner.IsDead() || owner.IsTeleporting())
            {
                Finish();
                return;
            }

            age += Time.deltaTime;
            if (!empowered && age > 8f)
            {
                Finish();
                return;
            }

            if (!originChecked)
            {
                originChecked = true;
                Collider collider = OriginCharacter(transform.position);
                if (collider != null)
                {
                    Impact(collider.GetComponentInParent<Character>(), transform.position, collider);
                    return;
                }
            }

            // Cover the whole empowered frame, including its endpoint. Limit
            // subdivisions on very slow frames without discarding flight time.
            float remaining = empowered
                ? Mathf.Min(Time.deltaTime, flightRemaining)
                : Mathf.Min(Time.deltaTime, 0.25f);
            float stepSize = Mathf.Max(0.025f, remaining / 16f);
            while (remaining > 0f && !finished)
            {
                float dt = Mathf.Min(remaining, stepSize);
                bool reachesEnd = empowered && dt >= flightRemaining - 0.000001f;
                remaining -= dt;
                Vector3 from = transform.position;
                Vector3 move = velocity * dt + Vector3.down * (gravity * dt * dt * 0.5f);
                velocity += Vector3.down * gravity * dt;
                if (empowered) flightRemaining = Mathf.Max(0f, flightRemaining - dt);
                // Clip BEFORE the collision query: a character beyond the endpoint
                // cannot steal the impact, and the bolt cannot travel below it.
                if (reachesEnd) move = terminalPoint - from;
                RaycastHit hit;
                if (move.sqrMagnitude > 0.000001f && FirstCharacter(from, move, out hit))
                {
                    transform.position = hit.point;
                    Impact(hit.collider.GetComponentInParent<Character>(), hit.point, hit.collider);
                    return;
                }

                transform.position = reachesEnd ? terminalPoint : from + move;
                if (reachesEnd)
                {
                    Impact(null, terminalPoint, null);
                    return;
                }
            }

            Draw();
        }

        internal static Vector3 CaptureAim(Vector3 origin, Vector3 direction, Player owner)
        {
            int count;
            RaycastHit[] hits;
            while (true)
            {
                count = Physics.RaycastNonAlloc(origin, direction, castHits, 120f,
                    CollisionMask, QueryTriggerInteraction.Ignore);
                if (count < castHits.Length)
                {
                    hits = castHits;
                    break;
                }

                if (castHits.Length >= MaxCachedHits)
                {
                    hits = Physics.RaycastAll(origin, direction, 120f, CollisionMask,
                        QueryTriggerInteraction.Ignore);
                    count = hits.Length;
                    break;
                }

                Array.Resize(ref castHits, castHits.Length * 2);
            }

            float nearest = 120f;
            Vector3 aim = origin + direction * nearest;
            for (int i = 0; i < count; i++)
            {
                var hit = hits[i];
                if (hit.collider == null || hit.collider.GetComponentInParent<Character>() == owner ||
                    hit.distance > nearest) continue;
                nearest = hit.distance;
                aim = hit.point;
            }

            return aim;
        }

        private Collider OriginCharacter(Vector3 origin)
        {
            int count;
            Collider[] hits;
            while (true)
            {
                count = Physics.OverlapSphereNonAlloc(origin, 0.035f, overlapHits,
                    CollisionMask, QueryTriggerInteraction.Ignore);
                if (count < overlapHits.Length)
                {
                    hits = overlapHits;
                    break;
                }

                if (overlapHits.Length >= MaxCachedHits)
                {
                    hits = Physics.OverlapSphere(origin, 0.035f, CollisionMask, QueryTriggerInteraction.Ignore);
                    count = hits.Length;
                    break;
                }

                Array.Resize(ref overlapHits, overlapHits.Length * 2);
            }

            Collider closest = null;
            float nearest = float.PositiveInfinity;
            for (int i = 0; i < count; i++)
            {
                var collider = hits[i];
                if (collider == null) continue;
                var target = collider.GetComponentInParent<Character>();
                if (target == null || target == owner) continue;
                float distance = (collider.ClosestPoint(origin) - origin).sqrMagnitude;
                if (distance >= nearest) continue;
                nearest = distance;
                closest = collider;
            }

            return closest;
        }

        private bool FirstCharacter(Vector3 origin, Vector3 move, out RaycastHit closest)
        {
            int count;
            RaycastHit[] hits;
            while (true)
            {
                count = Physics.SphereCastNonAlloc(origin, 0.035f, move.normalized, castHits,
                    move.magnitude, CollisionMask, QueryTriggerInteraction.Ignore);
                if (count < castHits.Length)
                {
                    hits = castHits;
                    break;
                }

                if (castHits.Length >= MaxCachedHits)
                {
                    hits = Physics.SphereCastAll(origin, 0.035f, move.normalized, move.magnitude,
                        CollisionMask, QueryTriggerInteraction.Ignore);
                    count = hits.Length;
                    break;
                }

                Array.Resize(ref castHits, castHits.Length * 2);
            }

            closest = default(RaycastHit);
            float nearest = float.PositiveInfinity;
            for (int i = 0; i < count; i++)
            {
                var hit = hits[i];
                if (hit.collider == null || hit.distance >= nearest) continue;
                var target = hit.collider.GetComponentInParent<Character>();
                if (target == null || target == owner) continue;
                nearest = hit.distance;
                closest = hit;
            }

            return closest.collider != null;
        }

        private void Draw()
        {
            if (shaft == null) return;
            shaft.SetPosition(0, transform.position - velocity.normalized * 0.65f);
            shaft.SetPosition(1, transform.position);
        }

        private void Impact(Character target, Vector3 point, Collider collider)
        {
            if (finished) return;
            // Close the bolt before damage/effect callbacks can re-enter it.
            Finish();
            if (shot != null && owner == Player.m_localPlayer)
            {
                var echo = new HelOathShot
                {
                    Owner = owner, Echo = true, Epoch = shot.Epoch,
                    Special = shot.Special, TouchV = shot.TouchV, Splash = shot.Splash, Radius = shot.Radius
                };
                if (Enemy(target))
                {
                    var hit = new HitData
                    {
                        m_damage = shot.Damage, m_point = point, m_dir = velocity.normalized,
                        m_pushForce = shot.Push, m_backstabBonus = shot.Backstab, m_ranged = true,
                        m_dodgeable = true, m_blockable = true, m_skill = Skills.SkillType.Bows,
                        m_skillRaiseAmount = 0f, m_hitType = HitData.HitType.PlayerHit
                    };
                    hit.SetAttacker(owner);
                    if (shot.Special)
                    {
                        echo.DirectTarget = target;
                        echo.DirectHit = hit;
                    }
                    else HelOathNetwork.Send(target, echo, hit, 0f, point);
                }

                // Same explosion path and owner-authorized RPC as the bow, with Echo
                // suppressing all charge/Touch credit at the receiving end.
                if (shot.Special) HelOathProjectile.Resolve(echo, point);
            }
        }

        private bool Enemy(Character target) => target != null && target != owner && !target.IsPlayer()
                                                && !target.IsDead() && BaseAI.IsEnemy(owner, target);

        private static bool Finite(float f) => !float.IsNaN(f) && !float.IsInfinity(f);
        private static bool Finite(Vector3 v) => Finite(v.x) && Finite(v.y) && Finite(v.z);

        private void Finish()
        {
            if (finished) return;
            finished = true;
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (material != null) Destroy(material);
        }
    }
}