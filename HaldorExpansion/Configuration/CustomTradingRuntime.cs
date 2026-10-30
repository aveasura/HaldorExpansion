using System;
using System.Collections;
using System.IO;
using System.IO.Compression;
using System.Text;
using HaldorExpansion.Trading;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;
using CompressionLevel = System.IO.Compression.CompressionLevel;

namespace HaldorExpansion.Configuration
{
    // Independent from the built-in trading schema: an optional bad user file must not
    // prevent the plugin, its migrations or the original shop from starting.
    internal sealed class CustomTradingRuntime
    {
        private string path, observedText, pendingText, lastWarning;
        private CustomTradingDocument local = new CustomTradingDocument();
        private CustomRPC rpc;
        private bool initialized, wasClient, haveStamp;
        private DateTime lastWrite;
        private long length;
        private float nextPoll, nextFullRead;
        private const int MaximumFileBytes = CustomTradingDocument.MaximumBytes;
        private const string GzipPrefix = "HEGZ1:";
        private static bool IsClient => ZNet.instance != null && !ZNet.instance.IsServer();

        internal void Initialize(string folder)
        {
            path = Path.Combine(folder, "custom-trading.yml");
            try
            {
                Directory.CreateDirectory(folder);
                if (!File.Exists(path)) ConfigText.WriteAtomic(path, CustomTradingDocument.DefaultText);
                observedText = ReadSnapshot();
                local = CustomTradingDocument.Parse(observedText);
            }
            catch (Exception error)
            {
                Warn(error.Message + " Custom stock is empty until a valid file is loaded.");
            }

            wasClient = IsClient;
            Apply(wasClient ? new CustomTradingDocument() : local);
            rpc = NetworkManager.Instance.AddRPC("CustomTradingConfigV1", RejectClientUpdate, Receive);
            SynchronizationManager.Instance.AddInitialSynchronization(rpc, MakePackage);
            nextFullRead = Time.unscaledTime + 60f;
            initialized = true;
        }

        internal void Update(float pollInterval)
        {
            if (!initialized) return;
            bool client = IsClient;
            if (client != wasClient)
            {
                // Do not expose local-only stock while waiting for the server's first packet.
                Apply(client ? new CustomTradingDocument() : local);
                wasClient = client;
            }

            if (Time.unscaledTime < nextPoll) return;
            float interval = float.IsNaN(pollInterval) || float.IsInfinity(pollInterval)
                ? 3f
                : Math.Max(0.5f, pollInterval);
            nextPoll = Time.unscaledTime + interval;
            bool fullRead = Time.unscaledTime >= nextFullRead;
            if (fullRead) nextFullRead = Time.unscaledTime + 60f;
            try
            {
                var info = new FileInfo(path);
                if (!info.Exists)
                {
                    // Editors often replace files via rename. Keep last valid stock during the gap.
                    haveStamp = false;
                    pendingText = null;
                    return;
                }

                if (!fullRead && pendingText == null && haveStamp &&
                    info.LastWriteTimeUtc == lastWrite && info.Length == length) return;
                string text = ReadSnapshot();
                if (text == observedText)
                {
                    pendingText = null;
                    return;
                }

                if (pendingText != text)
                {
                    pendingText = text;
                    return;
                }

                pendingText = null;
                // Remember invalid text too: warn once until the file changes, not every frame.
                observedText = text;
                var parsed = CustomTradingDocument.Parse(text);
                local = parsed;
                lastWarning = null;
                if (!client)
                {
                    Apply(parsed);
                    if (ZNet.instance != null && ZNet.instance.IsServer())
                        rpc.SendPackage(ZNet.instance.GetPeers(), MakePackage());
                }

                HaldorExpansionPlugin.Log?.LogInfo("[Custom trading] custom-trading.yml reloaded." +
                                                   (client
                                                       ? " Local changes will apply after disconnect; server settings remain active."
                                                       : ""));
            }
            catch (Exception error)
            {
                Warn(error.Message + " Last valid custom stock retained.");
            }
        }

        private string ReadSnapshot()
        {
            var before = new FileInfo(path);
            if (!before.Exists) throw new IOException("custom-trading.yml is temporarily unavailable.");
            long size = before.Length;
            DateTime write = before.LastWriteTimeUtc;
            if (size > MaximumFileBytes) throw new FormatException("File exceeds 1 MiB.");
            string text = File.ReadAllText(path);
            var after = new FileInfo(path);
            if (!after.Exists || after.Length != size || after.LastWriteTimeUtc != write)
                throw new IOException("File changed during read; retrying next check.");
            lastWrite = write;
            length = size;
            haveStamp = true;
            return text;
        }

        private static void Apply(CustomTradingDocument document)
        {
            CustomShopItems.Apply(document);
            CustomBuyItems.Apply(document);
            ConfigurationRuntime.RequestTradeRefresh();
        }

        private void Warn(string message)
        {
            if (lastWarning == message) return;
            lastWarning = message;
            HaldorExpansionPlugin.Log?.LogWarning("[Custom trading] " + message);
        }

        private ZPackage MakePackage()
        {
            var package = new ZPackage();
            package.Write(EncodeForNetwork(local.Serialize()));
            return package;
        }

        // Keep small wire documents readable. Compress large repetitive catalog rows.
        private static string EncodeForNetwork(string text)
        {
            if (text.Length < 32768) return text;
            byte[] data = Encoding.UTF8.GetBytes(text);
            using (var stream = new MemoryStream())
            {
                using (var gzip = new GZipStream(stream, CompressionLevel.Fastest, true))
                    gzip.Write(data, 0, data.Length);
                string packed = GzipPrefix + Convert.ToBase64String(stream.ToArray());
                return packed.Length < text.Length ? packed : text;
            }
        }

        private static string DecodeFromNetwork(string text)
        {
            if (!text.StartsWith(GzipPrefix, StringComparison.Ordinal)) return text;
            byte[] payload = Convert.FromBase64String(text.Substring(GzipPrefix.Length));
            using (var input = new MemoryStream(payload))
            using (var gzip = new GZipStream(input, CompressionMode.Decompress))
            using (var output = new MemoryStream())
            {
                var buffer = new byte[8192];
                int count;
                while ((count = gzip.Read(buffer, 0, buffer.Length)) > 0)
                {
                    if (output.Length + count > MaximumFileBytes)
                        throw new FormatException("Server custom trading data exceeds 1 MiB after decompression.");
                    output.Write(buffer, 0, count);
                }

                return new UTF8Encoding(false, true).GetString(output.ToArray());
            }
        }

        private IEnumerator RejectClientUpdate(long sender, ZPackage package)
        {
            yield break;
        }

        private IEnumerator Receive(long sender, ZPackage package)
        {
            if (!IsClient || ZNet.instance.GetServerPeer() == null ||
                sender != ZNet.instance.GetServerPeer().m_uid) yield break;
            try
            {
                Apply(CustomTradingDocument.Parse(DecodeFromNetwork(package.ReadString())));
                wasClient = true;
            }
            catch (Exception error)
            {
                Warn("Invalid server custom stock: " + error.Message + " Last valid settings retained.");
            }

            yield break;
        }
    }
}