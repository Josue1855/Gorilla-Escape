using UnityEngine;

namespace GorillaEscape.Core
{
    [CreateAssetMenu(menuName = "Gorilla Escape/Application Settings")]
    public sealed class ApplicationSettings : ScriptableObject
    {
        [Min(1)] public int TargetFrameRate = 60;
    }
}
