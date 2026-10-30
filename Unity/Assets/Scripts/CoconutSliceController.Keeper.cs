using System;
using UnityEngine;
using GorillaEscape.Gameplay.Input;

public sealed partial class CoconutSliceController
{
    private static readonly string[] KeeperCornerLabels =
    {
        "ARRIBA IZQ", "ARRIBA DER", "MEDIO IZQ", "MEDIO DER", "ABAJO IZQ", "ABAJO DER"
    };

    private GameObject leftGoalPost, rightGoalPost, topCrossbar;

    private void BuildKeeperGoalFrame()
    {
        Color bamboo = new Color(.56f, .42f, .16f);
        Color rope = new Color(.82f, .74f, .48f);
        leftGoalPost = JungleStageBuilder.CreatePrimitive(PrimitiveType.Cylinder, "Poste izquierdo Portería", new Vector3(-6.35f, .15f, .4f), new Vector3(.34f, 3.85f, .34f), bamboo, transform);
        rightGoalPost = JungleStageBuilder.CreatePrimitive(PrimitiveType.Cylinder, "Poste derecho Portería", new Vector3(6.35f, .15f, .4f), new Vector3(.34f, 3.85f, .34f), bamboo, transform);
        topCrossbar = JungleStageBuilder.CreatePrimitive(PrimitiveType.Cylinder, "Travesaño superior Portería", new Vector3(0f, 3.85f, .4f), new Vector3(.34f, 6.55f, .34f), bamboo, transform);
        topCrossbar.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
        JungleStageBuilder.CreatePrimitive(PrimitiveType.Sphere, "Unión izquierda Portería", new Vector3(-6.35f, 3.85f, .35f), new Vector3(.52f, .52f, .52f), rope, transform);
        JungleStageBuilder.CreatePrimitive(PrimitiveType.Sphere, "Unión derecha Portería", new Vector3(6.35f, 3.85f, .35f), new Vector3(.52f, .52f, .52f), rope, transform);
    }

    private void SpawnKeeperWave()
    {
        float progress = 1f - Mathf.Clamp01((roundEndsAt - Time.time) / RoundDuration);
        int amount = progress > .38f && UnityEngine.Random.value < .28f ? 2 : 1;
        int firstCorner = UnityEngine.Random.Range(0, 6);
        for (int i = 0; i < amount; i++)
        {
            int corner = i == 0 ? firstCorner : (firstCorner + UnityEngine.Random.Range(1, 6)) % 6;
            bool isHazard = amount == 1 && UnityEngine.Random.value < .16f;
            ShotMechanics.GoalieCornerTarget(corner, out float tx, out float ty);
            if (isHazard && torsoTracked)
            {
                tx = Mathf.Lerp(-5.5f, 5.5f, torsoPos.x) + UnityEngine.Random.Range(-.4f, .4f);
                ty = Mathf.Lerp(-2.5f, 2.5f, torsoPos.y) + UnityEngine.Random.Range(-.3f, .3f);
            }
            else
            {
                tx += UnityEngine.Random.Range(-.45f, .45f);
                ty += UnityEngine.Random.Range(-.35f, .35f);
            }
            Vector3 start = new Vector3(UnityEngine.Random.Range(-.45f, .45f), -.65f, 3.1f);
            Vector3 target = new Vector3(tx, ty, -1.6f);
            GameObject visual = AcquireCoconutPhysicsObject(
                isHazard ? 2 : 1,
                isHazard ? "Cactus disparo" : "Coco a portería",
                isHazard ? cactusPrefab : wholeCoconutPrefab,
                isHazard ? cactusModelMaterial : wholeCoconutModelMaterial,
                start,
                isHazard ? .92f : .84f,
                false,
                0,
                isHazard ? .78f : .65f);
            visual.transform.localScale = Vector3.one * .42f;
            Rigidbody body = visual.GetComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            float duration = Mathf.Lerp(1.18f, .74f, progress) + (i * .16f);
            coconuts.Add(new Coconut
            {
                gameObject = visual,
                body = body,
                bornAt = Time.time,
                hazard = isHazard,
                isKeeperShot = true,
                cornerIndex = corner,
                keeperStart = start,
                keeperTarget = target,
                keeperDuration = duration
            });
        }
        nextSpawn = Time.time + Mathf.Lerp(1.22f, .68f, progress) + UnityEngine.Random.Range(-.06f, .12f);
    }

    private bool AuthorizeKeeperAction()
    {
        if (latestRevision < 0) return false;
        if (authorizedSaveRevision == latestRevision) return true;
        if (session.TryAuthorize(JungleAction.Save, latestRevision))
        {
            authorizedSaveRevision = latestRevision;
            return true;
        }
        return false;
    }

    private void UpdateKeeperShots()
    {
        Vector2 curLeft = VisualHand(leftVisual, leftHand);
        Vector2 curRight = VisualHand(rightVisual, rightHand);
        for (int i = coconuts.Count - 1; i >= 0; i--)
        {
            Coconut coconut = coconuts[i];
            if (coconut.gameObject == null) { coconuts.RemoveAt(i); continue; }
            float t = (Time.time - coconut.bornAt) / Mathf.Max(.2f, coconut.keeperDuration);
            float clamped = Mathf.Clamp01(t);
            Vector3 pos = Vector3.Lerp(coconut.keeperStart, coconut.keeperTarget, clamped);
            pos.y += Mathf.Sin(clamped * Mathf.PI) * 1.35f;
            coconut.gameObject.transform.position = pos;
            coconut.gameObject.transform.Rotate(new Vector3(180f, 260f, 140f) * Time.deltaTime, Space.Self);
            float scale = Mathf.Lerp(.42f, 1.08f, clamped);
            coconut.gameObject.transform.localScale = Vector3.one * scale;

            if (state == GameState.Playing && t >= .80f && t <= 1.04f && session.IsInputReady)
            {
                Vector3 vp = Camera.main.WorldToViewportPoint(pos);
                GoalieSaveResult save = ShotMechanics.EvaluateGoalieSave(
                    vp.x, vp.y,
                    leftTracked, curLeft.x, curLeft.y, leftSpeed,
                    rightTracked, curRight.x, curRight.y, rightSpeed,
                    torsoTracked, torsoPos.x, torsoPos.y);
                if (save.Saved && AuthorizeKeeperAction())
                {
                    DeflectKeeperShot(coconut, save);
                    coconuts.RemoveAt(i);
                    continue;
                }
            }

            if (t >= 1.04f)
            {
                if (state == GameState.Playing)
                {
                    if (coconut.hazard)
                    {
                        if (AuthorizeKeeperAction())
                        {
                            score += 120;
                            callout = "¡CACTUS ESQUIVADO! +120";
                            calloutUntil = Time.time + .65f;
                        }
                    }
                    else
                    {
                        missed++;
                        combo = 0;
                        lives = Mathf.Max(0, lives - 1);
                        PlaySliceSound(true);
                        callout = lives > 0 ? "¡GOL DEL GORILA! VIDA -1" : "¡SIN VIDAS!";
                        calloutUntil = Time.time + .95f;
                        if (lives == 0)
                        {
                            state = GameState.Finale;
                            session.SetGameplayActive(false);
                            stateStartedAt = Time.time;
                            lostByLives = true;
                        }
                    }
                }
                ReleaseCoconutPhysicsObject(coconut.gameObject);
                coconuts.RemoveAt(i);
            }
        }
    }

    private void DeflectKeeperShot(Coconut coconut, GoalieSaveResult save)
    {
        Vector3 position = coconut.gameObject.transform.position;
        ReleaseCoconutPhysicsObject(coconut.gameObject);

        if (coconut.hazard)
        {
            PlaySliceSound(true);
            JungleStageBuilder.ShakeCamera(.16f, .22f);
            lives = Mathf.Max(0, lives - 1);
            combo = 0;
            callout = lives > 0 ? "¡OUCH! ESQUIVA EL CACTUS · VIDA -1" : "¡SIN VIDAS!";
            calloutUntil = Time.time + 1.05f;
            if (lives == 0)
            {
                state = GameState.Finale;
                session.SetGameplayActive(false);
                stateStartedAt = Time.time;
                lostByLives = true;
            }
            return;
        }

        PlaySliceSound(false);
        JungleStageBuilder.ShakeCamera(.065f, .11f);
        JungleStageBuilder.PulseSquash(position.x < 0f ? leftGoalPost : rightGoalPost, .96f, 1.14f, 26f, 10f);
        GameObject rebound = AcquireCoconutPhysicsObject(1, "Coco atajado", wholeCoconutPrefab, wholeCoconutModelMaterial, position, .85f, false, 0, .55f);
        Rigidbody body = rebound.GetComponent<Rigidbody>();
        float pushX = Mathf.Clamp(position.x * .95f, -4.5f, 4.5f);
        body.linearVelocity = new Vector3(pushX, UnityEngine.Random.Range(4.8f, 7.2f), 5.5f);
        body.angularVelocity = UnityEngine.Random.insideUnitSphere * 14f;
        temporaryPieces.Add(rebound);

        sliced++;
        combo++;
        bestCombo = Mathf.Max(bestCombo, combo);
        int gained = Mathf.RoundToInt((save.BasePoints + Mathf.Clamp(combo, 0, 20) * 15) * save.PrecisionMultiplier);
        score += gained;
        string sourceLabel = save.Source == GoalieSaveSource.LeftHand ? "MANO IZQ"
            : save.Source == GoalieSaveSource.RightHand ? "MANO DER" : "PECHO / TORSO";
        callout = $"¡ATAJADA {sourceLabel}! +{gained}" + (combo >= 2 ? $" · RACHA x{combo}" : "");
        calloutUntil = Time.time + .75f;
    }

    private void DrawKeeperCornerReticles(float w, float h)
    {
        if (Camera.main == null) return;
        float rSize = Mathf.Max(42f, h * .072f);
        for (int c = 0; c < 6; c++)
        {
            bool incoming = false;
            bool hazardIncoming = false;
            for (int i = 0; i < coconuts.Count; i++)
            {
                if (coconuts[i].isKeeperShot && coconuts[i].cornerIndex == c)
                {
                    incoming = true;
                    if (coconuts[i].hazard) hazardIncoming = true;
                }
            }
            if (!incoming) continue;

            ShotMechanics.GoalieCornerTarget(c, out float wx, out float wy);
            Vector3 vp = Camera.main.WorldToViewportPoint(new Vector3(wx, wy, -1.6f));
            float sx = vp.x * w, sy = (1f - vp.y) * h;
            Rect box = new Rect(sx - rSize * .5f, sy - rSize * .5f, rSize, rSize);
            Texture2D border = hazardIncoming ? JungleUITheme.Red : JungleUITheme.Gold;
            JungleRetainedUI.DrawTexture(new Rect(box.x, box.y, box.width, 3f), border);
            JungleRetainedUI.DrawTexture(new Rect(box.x, box.yMax - 3f, box.width, 3f), border);
            JungleRetainedUI.DrawTexture(new Rect(box.x, box.y, 3f, box.height), border);
            JungleRetainedUI.DrawTexture(new Rect(box.xMax - 3f, box.y, 3f, box.height), border);
            JungleRetainedUI.Label(new Rect(sx - rSize, box.y - 22f, rSize * 2f, 20f), hazardIncoming ? "¡ESQUIVA!" : KeeperCornerLabels[c], JungleUITheme.HudSmallTag);
        }
    }
}
