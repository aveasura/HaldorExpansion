using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace HaldorExpansion.Features.Berserker
{
    // TEMPORARY DEVELOPMENT ONLY.
    internal static class BerserkerTestCommands
    {
        internal const bool Enabled = true;
        internal const int TestDebtStacks = 5;

        internal static void Register()
        {
            if (!Enabled) return;
            CommandManager.Instance.AddConsoleCommand(new WrathFive());
            CommandManager.Instance.AddConsoleCommand(new DebtFive());
        }

        private static Player LocalPlayer(Terminal terminal)
        {
            Player player = Player.m_localPlayer;
            if (player == null || player.IsDead())
            {
                terminal?.AddString("[HE Test] Enter a world with a living character first.");
                return null;
            }
            return player;
        }

        private sealed class WrathFive : ConsoleCommand
        {
            public override string Name => "he_wrath5";
            public override string Help => "[HE TEST] Set local Berserker Wrath to stage V.";

            public override void Run(string[] args, Terminal terminal)
            {
                Player player = LocalPlayer(terminal);
                if (player == null) return;
                BerserkerRuntime.GrantTestWrathV(player);
                terminal?.AddString("[HE Test] Wrath V granted. Equip the Berserker axe for its effects.");
            }
        }

        private sealed class DebtFive : ConsoleCommand
        {
            public override string Name => "he_debt5";
            public override string Help => "[HE TEST] Add x5 (500 points) to local Berserker Blood Debt.";

            public override void Run(string[] args, Terminal terminal)
            {
                Player player = LocalPlayer(terminal);
                if (player == null) return;
                BerserkerRuntime.GrantTestDebt(player, TestDebtStacks * BerserkerRagnarokMath.DebtPerVisualStack);
                terminal?.AddString("[HE Test] Added x5 Blood Debt. Current: x" +
                    (BerserkerRuntime.Debt.Amount / BerserkerRagnarokMath.DebtPerVisualStack).ToString("0.##"));
            }
        }
    }
}
