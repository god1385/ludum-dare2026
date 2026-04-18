using UnityEngine;

namespace LudumDare2026.Core.GameFlow
{
    [CreateAssetMenu(fileName = "GameContent", menuName = "Game/Content Config", order = 1)]
    public class GameContentConfig : ScriptableObject
    {
        [SerializeField] private CipherMessageData[] _messages;

        public CipherMessageData[] Messages => _messages;

        public int MessageCount => _messages?.Length ?? 0;
    }
}
