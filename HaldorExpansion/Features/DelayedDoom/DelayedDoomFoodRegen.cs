using System.Collections.Generic;
using UnityEngine;

namespace HaldorExpansion.Features.DelayedDoom
{
    internal static class DelayedDoomFoodRegen
    {

        internal static float GetPlayerBaseFoodRegen(Player player)
        {
            if (player == null)
                return 0f;

            List<Player.Food> foods = player.GetFoods();
            if (foods == null)
                return 0f;

            float total = 0f;

            foreach (Player.Food food in foods)
            {
                if (food?.m_item?.m_shared == null)
                    continue;

                total += Mathf.Max(0f, food.m_item.m_shared.m_foodRegen);
            }

            return total;
        }
    }
}
