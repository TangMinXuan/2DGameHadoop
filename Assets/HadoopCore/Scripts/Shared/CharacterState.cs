using System.Collections.Generic;

namespace HadoopCore.Scripts.Shared {
    public class CharacterState {
        public static readonly CharacterState Dead = new(0, "Dead");                // 死亡状态
        public static readonly CharacterState Idle = new(1, "Idle");                // 空闲状态
        public static readonly CharacterState Walk = new(2, "Walk");                // 行走状态
        public static readonly CharacterState Patrol = new(3, "Patrol");            // 巡逻状态
        public static readonly CharacterState Chase = new(4, "Chase");              // 追击状态
        public static readonly CharacterState Attack = new(5, "Attack");            // 攻击状态
        public static readonly CharacterState Static = new(6, "Static");            // 攻击后短暂的静止状态
        public static readonly CharacterState UnderAttack = new(7, "UnderAttack");  // 受击状态

        private static readonly Dictionary<int, CharacterState> StateMap = new() {
            { 0, Dead },
            { 1, Idle },
            { 2, Walk },
            { 3, Patrol },
            { 4, Chase },
            { 5, Attack },
            { 6, Static },
            { 7, UnderAttack }
        };

        private readonly int _value;
        private readonly string _name;

        private CharacterState(int value, string name) {
            _value = value;
            _name = name;
        }

        public int GetValue() => _value;

        public string GetName() => _name;

        public static CharacterState FromValue(int value) {
            return StateMap.GetValueOrDefault(value, null);
        }

        public static implicit operator int(CharacterState state) => state._value;

        public override string ToString() => _name;
    }
}