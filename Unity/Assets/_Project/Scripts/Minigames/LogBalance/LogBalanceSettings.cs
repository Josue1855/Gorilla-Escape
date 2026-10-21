using UnityEngine;
namespace GorillaEscape.Minigames.LogBalance
{
    [CreateAssetMenu(menuName="Gorilla Escape/Log Balance Settings")]
    public sealed class LogBalanceSettings : ScriptableObject
    {
        public LogBalanceRules rules = new LogBalanceRules();
    }
}
