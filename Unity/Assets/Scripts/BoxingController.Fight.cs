using UnityEngine;
using UnityEngine.SceneManagement;
using GorillaEscape.Gameplay.Input;

public sealed partial class BoxingController
{
    private void StartBoxingGame()
    {
        sharedSession.ResetRound();
        playerHealth = 100f; gorillaHealth = 100f; boxingRoundTime = 90f; getUpEnergy = 0f;
        boxingGorillaAction = 0; gorillaActionAt = Time.time + 1.8f; boxingMessageCode = 0; boxingMessageUntil = 0;
        gorillaRig.ResetToRest(Vector3.zero);
        state = GameState.Boxing;
        if (sfxSource != null) sfxSource.PlayOneShot(JungleAudioBank.BoxingBell, .75f);
    }

    private void UpdateBoxing()
    {
        boxingRoundTime -= Time.deltaTime;
        if (boxingRoundTime <= 0f) { state = GameState.BoxingOver; if (sfxSource != null) sfxSource.PlayOneShot(JungleAudioBank.BoxingBell, .80f); return; }
        if (Time.time >= gorillaActionAt && boxingGorillaAction == 0)
        {
            float roll = UnityEngine.Random.value;
            boxingGorillaAction = roll < .58f ? 1 : roll < .82f ? 2 : 3; // golpe, guardia, gesto territorial
            boxingAttackResolved = false;
            gorillaAttackResolveAt = Time.time + (boxingGorillaAction == 1 ? .62f : 1.05f);
            gorillaActionAt = Time.time + UnityEngine.Random.Range(1.7f, 2.6f);
        }
        if (boxingGorillaAction == 1 && !boxingAttackResolved && Time.time >= gorillaAttackResolveAt)
        {
            boxingAttackResolved = true;
            bool covering = boxingLeftHand.y < .49f && boxingRightHand.y < .49f;
            bool dodging = Mathf.Abs(poseBodyX - .5f) > .13f;
            float damage = dodging ? 0f : covering ? 5f : UnityEngine.Random.Range(14f, 22f);
            playerHealth = Mathf.Max(0f, playerHealth - damage);
            boxingMessageCode = dodging ? 4 : covering ? 3 : 2; boxingMessageUntil = Time.time + .8f;
            if (sfxSource != null && damage > 0f) sfxSource.PlayOneShot(covering ? JungleAudioBank.BoxingBlock : JungleAudioBank.BoxingPunch, covering ? .55f : .85f);
            if (!dodging) JungleStageBuilder.ShakeCamera(covering ? .06f : .16f, covering ? .10f : .20f);
            if (playerHealth <= 0f) { state = GameState.BoxingKnockdown; getUpEnergy = 0f; resultAt = Time.time + 10f; if (sfxSource != null) sfxSource.PlayOneShot(JungleAudioBank.BoxingBell, .70f); }
        }
        if (Time.time >= gorillaActionAt) boxingGorillaAction = 0;
    }

    private void TryBoxingPunch(bool right, float cameraSpeed, float phonePower)
    {
        if (state != GameState.Boxing || Time.time < lastPunchAt + .34f) return;
        if (inputFrame == null || !sharedSession.TryAuthorize(right ? JungleAction.PunchRight : JungleAction.PunchLeft, inputFrame.Revision)) return;
        lastPunchAt = Time.time;
        bool gorillaBlocking = boxingGorillaAction == 2;
        float baseDamage = right ? Mathf.Clamp((phonePower - 180f) / 28f, 5f, 20f) : Mathf.Clamp(cameraSpeed * 4.2f, 3f, 10f);
        float damage = gorillaBlocking ? baseDamage * .18f : baseDamage;
        gorillaHealth = Mathf.Max(0f, gorillaHealth - damage);
        boxingMessageCode = gorillaBlocking ? 5 : 1; boxingMessageUntil = Time.time + .55f;
        if (sfxSource != null) sfxSource.PlayOneShot(gorillaBlocking ? JungleAudioBank.BoxingBlock : JungleAudioBank.BoxingPunch, gorillaBlocking ? .55f : .85f);
        JungleStageBuilder.ShakeCamera(gorillaBlocking ? .045f : (right ? .12f : .075f), .14f);
        gorillaRig.TriggerBoxingHitReaction(.38f);
        if (gorillaRig.MascotObject != null)
        {
            JungleSquashStretch.Apply(gorillaRig.MascotObject, 1.12f, 0.88f, 0.28f);
        }
        if (gorillaHealth <= 0f) { state = GameState.BoxingOver; if (sfxSource != null) sfxSource.PlayOneShot(JungleAudioBank.BoxingBell, .85f); }
    }

    private int cachedRoundSec = -1, cachedKnockdownSec = -1, cachedPlayerHp = -1, cachedGorillaHp = -1;
    private string cachedRoundSecText = "", cachedKnockdownText = "", cachedPlayerHpText = "", cachedGorillaHpText = "";

    private void DrawBoxing()
    {
        float w = Screen.width, h = Screen.height;
        int roundSec = Mathf.CeilToInt(boxingRoundTime);
        if (roundSec != cachedRoundSec) { cachedRoundSec = roundSec; cachedRoundSecText = roundSec.ToString(); }

        JungleUITheme.DrawPanel(new Rect(w * .16f, h * .025f, w * .68f, h * .15f));
        JungleUITheme.DrawShadowedLabel(new Rect(w * .35f, h * .035f, w * .30f, h * .055f), "BOXEO CON GORILA", fightTitleStyle);
        DrawHealthBar(new Rect(w * .19f, h * .105f, w * .26f, h * .035f), playerHealth, new Color(.1f, .75f, 1f), true);
        DrawHealthBar(new Rect(w * .55f, h * .105f, w * .26f, h * .035f), gorillaHealth, new Color(1f, .25f, .1f), false);
        JungleUITheme.DrawShadowedLabel(new Rect(w * .46f, h * .095f, w * .08f, h * .05f), cachedRoundSecText, fightTitleStyle);

        if (state == GameState.BoxingKnockdown)
        {
            int kdSec = Mathf.Max(0, Mathf.CeilToInt(resultAt - Time.time));
            if (kdSec != cachedKnockdownSec) { cachedKnockdownSec = kdSec; cachedKnockdownText = $"¡LEVÁNTATE!  {kdSec}"; }
            JungleUITheme.DrawPanel(new Rect(w * .25f, h * .28f, w * .50f, h * .32f), true);
            JungleUITheme.DrawShadowedLabel(new Rect(w * .28f, h * .31f, w * .44f, h * .10f), cachedKnockdownText, fightTitleStyle);
            JungleUITheme.DrawShadowedLabel(new Rect(w * .29f, h * .41f, w * .42f, h * .07f), "AGITA EL CELULAR REPETIDAMENTE", fightTextStyle);
            JungleRetainedUI.DrawTexture(new Rect(w * .31f, h * .51f, w * .38f, h * .035f), darkTexture);
            JungleRetainedUI.DrawTexture(new Rect(w * .31f, h * .51f, w * .38f * getUpEnergy / 100f, h * .035f), accentTexture);
        }
        else if (state == GameState.BoxingOver)
        {
            JungleUITheme.DrawPanel(new Rect(w * .26f, h * .30f, w * .48f, h * .25f), true);
            string result = gorillaHealth <= 0f ? "¡GANASTE!" : playerHealth <= 0f ? "K.O. DEL GORILA" : playerHealth >= gorillaHealth ? "¡GANASTE POR PUNTOS!" : "EL GORILA GANA";
            JungleUITheme.DrawShadowedLabel(new Rect(w * .29f, h * .34f, w * .42f, h * .09f), result, fightTitleStyle);
            if (JungleRetainedUI.Button(new Rect(w * .38f, h * .45f, w * .24f, h * .065f), "REVANCHA", buttonStyle)) { JungleAudioBank.PlayUiClick(); StartBoxingGame(); }
        }
        else
        {
            string action = boxingGorillaAction == 1 ? "¡ESQUIVA O CÚBRETE!" : boxingGorillaAction == 2 ? "EL GORILA SE CUBRE" : boxingGorillaAction == 3 ? "¡GOLPE TERRITORIAL!" : "MANOS ARRIBA";
            if (Time.time < boxingMessageUntil) action = boxingMessageCode == 1 ? "¡BUEN GOLPE!" : boxingMessageCode == 2 ? "¡TE GOLPEÓ!" : boxingMessageCode == 3 ? "BLOQUEO" : boxingMessageCode == 4 ? "¡ESQUIVA!" : "GUARDIA DEL GORILA";
            JungleUITheme.DrawShadowedLabel(new Rect(w * .30f, h * .18f, w * .40f, h * .065f), action, fightTextStyle);
        }

        Vector2 visualLeft = CameraActive && leftVisual.TryPosition(Time.unscaledTime, out float lx, out float ly) ? new Vector2(lx, ly) : boxingLeftHand;
        Vector2 visualRight = CameraActive && rightVisual.TryPosition(Time.unscaledTime, out float rx, out float ry) ? new Vector2(rx, ry) : boxingRightHand;
        DrawBoxingGlove(visualLeft, new Color(.12f, .55f, 1f), "L");
        DrawBoxingGlove(visualRight, new Color(1f, .25f, .12f), "R");
        JungleUITheme.DrawShadowedLabel(new Rect(w * .19f, h * .83f, w * .62f, h * .065f), "AMBAS MANOS ARRIBA = CUBRIRSE  ·  MUEVE EL CUERPO = ESQUIVAR  ·  CELULAR = PUÑO DERECHO", fightTextStyle);
        if (JungleRetainedUI.Button(new Rect(w * .84f, h * .04f, w * .13f, h * .065f), "MENÚ", buttonStyle))
        {
            JungleAudioBank.PlayUiClick();
            gorillaRig.ResetToRest(Vector3.zero);
            SceneNavigator.Load(SceneNavigator.MainMenu);
        }
    }

    private void DrawHealthBar(Rect rect, float health, Color color, bool isPlayer)
    {
        int hpInt = Mathf.CeilToInt(health);
        string text;
        if (isPlayer)
        {
            if (hpInt != cachedPlayerHp) { cachedPlayerHp = hpInt; cachedPlayerHpText = "TÚ  " + hpInt; }
            text = cachedPlayerHpText;
        }
        else
        {
            if (hpInt != cachedGorillaHp) { cachedGorillaHp = hpInt; cachedGorillaHpText = "GORILA  " + hpInt; }
            text = cachedGorillaHpText;
        }
        JungleRetainedUI.DrawTexture(rect, darkTexture);
        JungleRetainedUI.DrawTexture(new Rect(rect.x, rect.y, rect.width * Mathf.Clamp01(health / 100f), rect.height), color.b > color.r ? boxingBlueTexture : boxingRedTexture);
        JungleRetainedUI.Label(new Rect(rect.x, rect.y - rect.height * 1.1f, rect.width, rect.height), text, bodyStyle);
    }

    private void DrawBoxingGlove(Vector2 hand, Color color, string label)
    {
        float w = Screen.width, h = Screen.height;
        float x = Mathf.Lerp(w * .13f, w * .87f, hand.x), y = Mathf.Lerp(h * .24f, h * .78f, hand.y);
        Rect wrist = new Rect(x - w * .018f, y + h * .022f, w * .036f, h * .075f);
        Rect glove = new Rect(x - w * .035f, y - h * .025f, w * .070f, h * .070f);
        Texture2D texture = color.b > color.r ? boxingBlueTexture : boxingRedTexture;
        JungleRetainedUI.DrawTexture(wrist, texture); JungleRetainedUI.DrawTexture(glove, texture);
        JungleRetainedUI.Label(glove, label, bigStyle);
    }
}
