using UnityEngine;

namespace GorillaEscape.Core
{
    public sealed class Bootstrap : MonoBehaviour
    {
        [SerializeField] private ApplicationSettings settings;
        private int previousFrameRate;
        private bool applied;

        private void Awake()
        {
            if (settings == null || settings.TargetFrameRate < 1)
            {
                Debug.LogError("Gorilla Escape bootstrap requires valid ApplicationSettings.", this);
                enabled = false;
                return;
            }
            previousFrameRate = Application.targetFrameRate;
            Application.targetFrameRate = settings.TargetFrameRate;
            applied = true;
            Debug.Log("Gorilla Escape foundation ready; gameplay and hardware adapters pending.", this);
        }

        private void OnDestroy()
        {
            if (applied) Application.targetFrameRate = previousFrameRate;
        }
    }
}
