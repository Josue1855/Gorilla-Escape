using System;
using UnityEngine;

namespace GorillaEscape.Minigames.GorillaSmash
{
    [CreateAssetMenu(fileName = "GorillaSmashSettings", menuName = "Gorilla Escape/Gorilla Smash Settings")]
    public sealed class GorillaSmashSettings : ScriptableObject
    {
        public GorillaSmashRules rules = new GorillaSmashRules();

        [Header("Feedback Colors")]
        public Color colorMissWeak = new Color(0.75f, 0.35f, 0.35f);
        public Color colorGood = new Color(0.85f, 0.75f, 0.25f);
        public Color colorGreat = new Color(0.25f, 0.82f, 0.35f);
        public Color colorPerfect = new Color(0.15f, 0.75f, 0.95f);
        public Color colorLegendary = new Color(1.0f, 0.85f, 0.15f);

        public Color GetTierColor(GorillaSmashTier tier)
        {
            switch (tier)
            {
                case GorillaSmashTier.MissWeak: return colorMissWeak;
                case GorillaSmashTier.Good: return colorGood;
                case GorillaSmashTier.Great: return colorGreat;
                case GorillaSmashTier.Perfect: return colorPerfect;
                case GorillaSmashTier.Legendary: return colorLegendary;
                default: return Color.white;
            }
        }
    }
}
