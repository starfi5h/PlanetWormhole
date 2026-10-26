using BepInEx.Configuration;

namespace PlanetWormhole
{
    internal class Constants
    {
        public const int PROLIFERATOR_MK1 = 1141;
        public const int PROLIFERATOR_MK2 = 1142;
        public const int PROLIFERATOR_MK3 = 1143;
        public const int WARPER = 1210;
        public const int IONOSPHERIC_TECH = 1505;
        public const int SHIP_ENGINE_4 = 3404;
        public const int MAX_ITEM_COUNT = 12000;
        public const int BUFFER_SIZE = 1000;
        public const int PERIOD = 9;

        /// <summary>MK1 每件可喷涂的物品数（HpMax）</summary>
        public static int INC_SPRAY_TIMES_MK1;

        /// <summary>MK1 能力值（每件物品被喷涂所需的点数）</summary>
        public static int INC_ABILITY_MK1;

        /// <summary>MK1 科技加成的额外喷涂次数</summary>
        public static int EXTRA_INC_SPRAY_TIMES_MK1;

        /// <summary>MK2 每件可喷涂的物品数（HpMax）</summary>
        public static int INC_SPRAY_TIMES_MK2;

        /// <summary>MK2 能力值（每件物品被喷涂所需的点数）</summary>
        public static int INC_ABILITY_MK2;

        /// <summary>MK2 科技加成的额外喷涂次数</summary>
        public static int EXTRA_INC_SPRAY_TIMES_MK2;

        /// <summary>MK3 每件可喷涂的物品数（HpMax）</summary>
        public static int INC_SPRAY_TIMES_MK3;

        /// <summary>MK3 能力值（每件物品被喷涂所需的点数）</summary>
        public static int INC_ABILITY_MK3;

        /// <summary>MK3 科技加成的额外喷涂次数</summary>
        public static int EXTRA_INC_SPRAY_TIMES_MK3;

        static Constants()
        {
            InitProliferatorTier(PROLIFERATOR_MK1, ref INC_SPRAY_TIMES_MK1, ref INC_ABILITY_MK1, ref EXTRA_INC_SPRAY_TIMES_MK1);
            InitProliferatorTier(PROLIFERATOR_MK2, ref INC_SPRAY_TIMES_MK2, ref INC_ABILITY_MK2, ref EXTRA_INC_SPRAY_TIMES_MK2);
            InitProliferatorTier(PROLIFERATOR_MK3, ref INC_SPRAY_TIMES_MK3, ref INC_ABILITY_MK3, ref EXTRA_INC_SPRAY_TIMES_MK3);
        }

        /// <summary>
        /// 从游戏数据读取指定档位增产剂的参数并缓存：
        /// 喷涂次数取 HpMax，能力值取 Ability，额外喷涂次数按 incTable 科技加成换算（与原版 MK3 读取逻辑一致）。
        /// </summary>
        private static void InitProliferatorTier(int itemId, ref int sprayTimes, ref int ability, ref int extraSprayTimes)
        {
            ItemProto proto = LDB.items.Select(itemId);
            sprayTimes = proto.HpMax;
            ability = proto.Ability;
            extraSprayTimes = (int)(sprayTimes * (Cargo.incTable[ability] * 0.001) + 0.1);
        }
    }
}
