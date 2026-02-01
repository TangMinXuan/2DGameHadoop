using System.Collections.Generic;

namespace HadoopCore.Scripts.Shared {
    public class CharacterRank {
        private static readonly Dictionary<string, int> RankMap = new() {
            { "Player", 0 },
            { "Monster", 1 },
            { "SeniorMonster", 2 },
            { "Boss", 3 }
        };

        public static int GetRank(string tag) {
            return RankMap.GetValueOrDefault(tag, -1);
        }

        public static bool ContainsTag(string tag) {
            return RankMap.ContainsKey(tag);
        }
    }
}