using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace HaldorExpansion.Features.Berserker
{
    // Temporary one-shot inspection: no VFX modifications or combat changes.
    internal static class BerserkerThunderbloodVfxDiagnostics
    {
        private const string PrefabName = "BattleaxeGold_BloodLightning";
        private static bool finished;
        private static int attempts;

        internal static void TryDump(Player player)
        {
            if (finished || player == null || player != Player.m_localPlayer) return;
            var scene = ZNetScene.instance;
            if (scene == null) return;
            // Avoid logging every frame; give the scene time to finish registering prefabs.
            if (++attempts % 120 != 0) return;
            var prefab = scene.GetPrefab(PrefabName);
            if (prefab == null)
            {
                if (attempts >= 1200)
                {
                    finished = true;
                    HaldorExpansionPlugin.Log?.LogWarning("[ThunderbloodVFX] Prefab not found: " + PrefabName + ". Check internal prefab name/game version.");
                }
                return;
            }
            finished = true;
            try
            {
                var sb = new StringBuilder(16384);
                sb.AppendLine("[ThunderbloodVFX] ===== BEGIN prefab inspection: " + PrefabName + " =====");
                sb.AppendLine("[ThunderbloodVFX] Transform hierarchy, ParticleSystems, Renderers, Lights, ZSFX:");
                DumpNode(prefab.transform, prefab.transform, sb, 0);
                sb.AppendLine("[ThunderbloodVFX] ===== END prefab inspection =====");
                HaldorExpansionPlugin.Log?.LogInfo(sb.ToString());
            }
            catch (Exception ex)
            {
                HaldorExpansionPlugin.Log?.LogWarning("[ThunderbloodVFX] Dump failed: " + ex);
            }
        }

        private static void DumpNode(Transform root, Transform node, StringBuilder sb, int depth)
        {
            if (node == null || depth > 32) return;
            string prefix = new string(' ', depth * 2);
            sb.Append("[ThunderbloodVFX] ").Append(prefix).Append(node.name)
                .Append(" active=").Append(node.gameObject.activeSelf)
                .Append(" pos=").Append(node.localPosition)
                .Append(" scale=").Append(node.localScale).AppendLine();

            var particle = node.GetComponent<ParticleSystem>();
            if (particle != null)
            {
                var main = particle.main;
                var emission = particle.emission;
                var shape = particle.shape;
                var noise = particle.noise;
                sb.Append("[ThunderbloodVFX] ").Append(prefix).Append("  PARTICLES")
                    .Append(" duration=").Append(main.duration)
                    .Append(" loop=").Append(main.loop)
                    .Append(" lifetime=").Append(main.startLifetime.constant)
                    .Append(" speed=").Append(main.startSpeed.constant)
                    .Append(" size=").Append(main.startSize.constant)
                    .Append(" color=").Append(main.startColor.color)
                    .Append(" space=").Append(main.simulationSpace)
                    .Append(" emission=").Append(emission.enabled ? emission.rateOverTime.constant.ToString() : "off")
                    .Append(" shape=").Append(shape.enabled ? shape.shapeType.ToString() : "off")
                    .Append(" noise=").Append(noise.enabled)
                    .AppendLine();
            }
            foreach (var renderer in node.GetComponents<Renderer>())
            {
                sb.Append("[ThunderbloodVFX] ").Append(prefix).Append("  RENDERER ")
                    .Append(renderer.GetType().Name).Append(" enabled=").Append(renderer.enabled).AppendLine();
                foreach (var material in renderer.sharedMaterials)
                {
                    if (material == null) continue;
                    sb.Append("[ThunderbloodVFX] ").Append(prefix).Append("    material=").Append(material.name)
                        .Append(" shader=").Append(material.shader != null ? material.shader.name : "null")
                        .Append(" tint=").Append(material.HasProperty("_Color") ? material.GetColor("_Color").ToString() : "n/a")
                        .Append(" mainTex=").Append(material.mainTexture != null ? material.mainTexture.name : "null")
                        .AppendLine();
                }
            }
            foreach (var light in node.GetComponents<Light>())
                sb.Append("[ThunderbloodVFX] ").Append(prefix).Append("  LIGHT color=").Append(light.color)
                    .Append(" range=").Append(light.range).Append(" intensity=").Append(light.intensity).AppendLine();
            foreach (var sfx in node.GetComponents<ZSFX>())
                sb.Append("[ThunderbloodVFX] ").Append(prefix).Append("  ZSFX ").Append(sfx.name).AppendLine();
            for (int i = 0; i < node.childCount; ++i) DumpNode(root, node.GetChild(i), sb, depth + 1);
        }
    }
}
