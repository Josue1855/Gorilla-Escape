using System;
using System.IO;
using GLTFast;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

public sealed class GorillaMascotRig : IDisposable
{
    private GameObject gorillaMascot;
    private GltfImport gorillaImport;
    private Transform gorillaHead, gorillaNeck, gorillaSpine, gorillaHipLeft, gorillaHipRight, gorillaKneeLeft, gorillaKneeRight;
    private Transform gorillaShoulderLeft, gorillaShoulderRight, gorillaElbowLeft, gorillaElbowRight, gorillaWristLeft, gorillaWristRight;
    private Quaternion gorillaHeadRest, gorillaNeckRest, gorillaSpineRest, gorillaHipLeftRest, gorillaHipRightRest, gorillaKneeLeftRest, gorillaKneeRightRest;
    private Quaternion gorillaLeftRest, gorillaRightRest, gorillaElbowLeftRest, gorillaElbowRightRest, gorillaWristLeftRest, gorillaWristRightRest;
    private Vector3 gorillaRestPosition;
    private Quaternion gorillaRestRotation;
    private float gorillaReactionUntil;
    private int gorillaReaction;
    private int gorillaVariant, previousGorillaVariant = -1;
    private Animator bowlingGorillaAnimator;
    private AnimationClip bowlingGorillaIdleClip, bowlingGorillaAngryClip, bowlingGorillaDanceClip;
    private PlayableGraph bowlingGorillaGraph;
    private AnimationClipPlayable bowlingGorillaPlayable;
    private float bowlingGorillaClipStartedAt, bowlingGorillaClipDuration;
    private bool bowlingGorillaUsingImportedAnimation;

    public GameObject MascotObject => gorillaMascot;
    public Vector3 RestPosition => gorillaRestPosition;
    public Quaternion RestRotation => gorillaRestRotation;
    public int CurrentReaction => gorillaReaction;
    public float ReactionUntil => gorillaReactionUntil;

    public async void LoadAsync(Transform gorillaAnchor, Func<Color, float, float, Material> makeMaterial, BowlingHybridEnvironment hybridBowlingEnvironment)
    {
        bool boxingScene = SceneManager.GetActiveScene().name == SceneNavigator.Boxing;
        GameObject prefab = JungleStageBuilder.LoadBowlingGorillaPrefab();
        if (prefab == null) { Debug.LogWarning("No se encontró el gorila bípedo oficial"); return; }
        gorillaMascot = UnityEngine.Object.Instantiate(prefab);
        gorillaMascot.name = boxingScene ? "Gorila rival de boxeo" : "Gorila bípedo espectador de Bowling";
        foreach (Animation animation in gorillaMascot.GetComponentsInChildren<Animation>(true)) animation.enabled = false;
        ApplyBowlingGorillaMaterials();

        Vector3 intendedPosition = boxingScene ? new Vector3(0f, -.30f, -2.75f) : gorillaAnchor != null ? gorillaAnchor.position : new Vector3(-6.25f, -.48f, 4.2f);
        if (!boxingScene)
        {
            intendedPosition.x = Mathf.Max(intendedPosition.x, -4.35f);
            intendedPosition.z -= 2.35f;
        }
        Vector3 intendedFacing = Camera.main != null ? Camera.main.transform.position - intendedPosition : Vector3.back;
        intendedFacing.y = 0f;
        gorillaMascot.transform.rotation = intendedFacing.sqrMagnitude > .01f ? Quaternion.LookRotation(intendedFacing.normalized) : Quaternion.identity;
        if (!boxingScene && gorillaAnchor != null) gorillaMascot.transform.rotation *= gorillaAnchor.rotation;
        Renderer[] renderers = gorillaMascot.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length > 0)
        {
            Bounds bounds = JungleStageBuilder.CombinedBounds(renderers);
            float scale = (boxingScene ? 4.6f : 5.75f) / Mathf.Max(.01f, bounds.size.y);
            gorillaMascot.transform.localScale = Vector3.one * scale;
            bounds = JungleStageBuilder.CombinedBounds(renderers);
            gorillaMascot.transform.position += new Vector3(intendedPosition.x - bounds.center.x, intendedPosition.y - bounds.min.y, intendedPosition.z - bounds.center.z);
        }
        JungleStageBuilder.DisableChildColliders(gorillaMascot);

        gorillaHead = FindBone("Head", "Head_M_040");
        gorillaNeck = FindBone("Neck", "Neck_M_039");
        gorillaSpine = FindBone("Spine2", "Spine1", "Spine2_M_037");
        gorillaHipLeft = FindBone("LeftUpLeg", "Hip_L_08");
        gorillaHipRight = FindBone("RightUpLeg", "Hip_R_022");
        gorillaKneeLeft = FindBone("LeftLeg", "Knee_L_09");
        gorillaKneeRight = FindBone("RightLeg", "Knee_R_023");
        gorillaShoulderLeft = FindBone("LeftArm", "LeftShoulder", "Shoulder_L_043");
        gorillaShoulderRight = FindBone("RightArm", "RightShoulder", "Shoulder_R_062");
        gorillaElbowLeft = FindBone("LeftForeArm", "Elbow_L_00");
        gorillaElbowRight = FindBone("RightForeArm", "Elbow_R_063");
        gorillaWristLeft = FindBone("LeftHand", "Wrist_L_044");
        gorillaWristRight = FindBone("RightHand", "Wrist_R_064");
        if (gorillaHead != null) gorillaHeadRest = gorillaHead.localRotation;
        if (gorillaNeck != null) gorillaNeckRest = gorillaNeck.localRotation;
        if (gorillaSpine != null) gorillaSpineRest = gorillaSpine.localRotation;
        if (gorillaHipLeft != null) gorillaHipLeftRest = gorillaHipLeft.localRotation;
        if (gorillaHipRight != null) gorillaHipRightRest = gorillaHipRight.localRotation;
        if (gorillaKneeLeft != null) gorillaKneeLeftRest = gorillaKneeLeft.localRotation;
        if (gorillaKneeRight != null) gorillaKneeRightRest = gorillaKneeRight.localRotation;
        if (gorillaShoulderLeft != null) gorillaLeftRest = gorillaShoulderLeft.localRotation;
        if (gorillaShoulderRight != null) gorillaRightRest = gorillaShoulderRight.localRotation;
        if (gorillaElbowLeft != null) gorillaElbowLeftRest = gorillaElbowLeft.localRotation;
        if (gorillaElbowRight != null) gorillaElbowRightRest = gorillaElbowRight.localRotation;
        if (gorillaWristLeft != null) gorillaWristLeftRest = gorillaWristLeft.localRotation;
        if (gorillaWristRight != null) gorillaWristRightRest = gorillaWristRight.localRotation;
        gorillaRestPosition = gorillaMascot.transform.position;
        gorillaRestRotation = gorillaMascot.transform.rotation;
        if (!boxingScene) SetupBowlingGorillaAnimations();
    }

    public void ResetToRest(Vector3 offset)
    {
        if (gorillaMascot == null) return;
        gorillaMascot.transform.position = gorillaRestPosition + offset;
        gorillaMascot.transform.rotation = gorillaRestRotation;
    }

    public void TriggerBoxingHitReaction(float duration = .38f)
    {
        gorillaReaction = 2;
        gorillaReactionUntil = Time.time + duration;
    }

    public void TriggerBowlingReaction(bool strike, BowlingHybridEnvironment hybridBowlingEnvironment)
    {
        if (hybridBowlingEnvironment != null) hybridBowlingEnvironment.ShowThrowReaction(strike);
        if (gorillaMascot == null) return;
        gorillaReaction = strike ? 1 : 2;
        gorillaVariant = UnityEngine.Random.Range(0, 3);
        if (gorillaVariant == previousGorillaVariant) gorillaVariant = (gorillaVariant + UnityEngine.Random.Range(1, 3)) % 3;
        previousGorillaVariant = gorillaVariant;
        if (bowlingGorillaUsingImportedAnimation)
        {
            AnimationClip reactionClip = strike ? bowlingGorillaAngryClip : bowlingGorillaDanceClip;
            float speed = strike ? 1.35f : 2.10f;
            PlayBowlingGorillaClip(reactionClip, speed, false);
            gorillaReactionUntil = Time.time + reactionClip.length / speed;
        }
        else gorillaReactionUntil = Time.time + 2.6f;
    }

    public void Animate(bool isBoxingMode, int boxingGorillaAction, float gorillaAttackResolveAt)
    {
        if (gorillaMascot == null) return;
        if (isBoxingMode) { AnimateBoxingGorilla(boxingGorillaAction, gorillaAttackResolveAt); return; }
        if (bowlingGorillaUsingImportedAnimation)
        {
            gorillaMascot.transform.position = gorillaRestPosition;
            gorillaMascot.transform.rotation = gorillaRestRotation;
            float duration = Mathf.Abs(bowlingGorillaClipDuration);
            if (duration > 0f && Time.time - bowlingGorillaClipStartedAt >= duration)
            {
                if (bowlingGorillaClipDuration < 0f && bowlingGorillaPlayable.IsValid())
                {
                    bowlingGorillaPlayable.SetTime(0d);
                    bowlingGorillaClipStartedAt = Time.time;
                }
                else PlayBowlingGorillaClip(bowlingGorillaIdleClip != null ? bowlingGorillaIdleClip : bowlingGorillaDanceClip, 1.15f, true);
            }
            return;
        }
        bool reacting = Time.time < gorillaReactionUntil;
        float elapsed = Mathf.Max(0f, 2.6f - (gorillaReactionUntil - Time.time));
        float u = Mathf.Clamp01(elapsed / 2.6f);
        float envelope = reacting ? SmoothPulse(u, 0f, .13f, .84f, 1f) : 0f;
        if (reacting && gorillaReaction == 1)
        {
            float crouch = SmoothPulse(u, .02f, .15f, .30f, .48f);
            float beat1 = SmoothPulse(u, .20f, .27f, .33f, .40f);
            float beat2 = SmoothPulse(u, .36f, .43f, .49f, .56f);
            float beat3 = SmoothPulse(u, .52f, .59f, .65f, .73f);
            float leftBeat = gorillaVariant == 1 ? 0f : Mathf.Max(beat1, beat3);
            float rightBeat = gorillaVariant == 1 ? 0f : gorillaVariant == 2 ? Mathf.Max(beat1, beat3) : beat2;
            float impact = Mathf.Max(leftBeat, rightBeat);
            float roar = gorillaVariant == 1 ? SmoothPulse(u, .18f, .31f, .65f, .83f) : 0f;
            gorillaMascot.transform.position = gorillaRestPosition;
            gorillaMascot.transform.rotation = gorillaRestRotation;
            SetBone(gorillaSpine, gorillaSpineRest, new Vector3(0, impact * 2f, -10f * crouch + 5f * impact + 10f * roar), envelope);
            SetBone(gorillaNeck, gorillaNeckRest, new Vector3(0, -impact * 2f, 7f * crouch - 4f * impact - 9f * roar), envelope);
            SetBone(gorillaHead, gorillaHeadRest, new Vector3(0, (rightBeat - leftBeat) * 3f, -5f * impact - 12f * roar), envelope);
            SetBone(gorillaHipLeft, gorillaHipLeftRest, new Vector3(0, 0, -8f * crouch), envelope);
            SetBone(gorillaHipRight, gorillaHipRightRest, new Vector3(0, 0, 8f * crouch), envelope);
            SetBone(gorillaKneeLeft, gorillaKneeLeftRest, new Vector3(0, 0, 15f * crouch), envelope);
            SetBone(gorillaKneeRight, gorillaKneeRightRest, new Vector3(0, 0, -15f * crouch), envelope);
            SetBone(gorillaShoulderLeft, gorillaLeftRest, new Vector3(0, -20f * leftBeat - 24f * roar, 14f * crouch + 24f * leftBeat + 28f * roar), envelope);
            SetBone(gorillaShoulderRight, gorillaRightRest, new Vector3(0, 20f * rightBeat + 24f * roar, -14f * crouch - 24f * rightBeat - 28f * roar), envelope);
            SetBone(gorillaElbowLeft, gorillaElbowLeftRest, new Vector3(0, 0, 48f * leftBeat - 18f * roar), envelope);
            SetBone(gorillaElbowRight, gorillaElbowRightRest, new Vector3(0, 0, -48f * rightBeat + 18f * roar), envelope);
            SetBone(gorillaWristLeft, gorillaWristLeftRest, new Vector3(0, 0, 10f * leftBeat), envelope);
            SetBone(gorillaWristRight, gorillaWristRightRest, new Vector3(0, 0, -10f * rightBeat), envelope);
        }
        else if (reacting)
        {
            float joy = SmoothPulse(u, .04f, .16f, .70f, .90f);
            float bounce = (Mathf.Sin(Mathf.Clamp01((u - .10f) / .68f) * Mathf.PI * 4f) * .5f + .5f) * joy;
            float sway = Mathf.Sin(Mathf.Clamp01((u - .12f) / .65f) * Mathf.PI * 3f) * joy;
            float clap = gorillaVariant == 0 ? Mathf.Max(0f, Mathf.Sin(u * Mathf.PI * 8f)) * joy : 0f;
            float raised = gorillaVariant == 1 ? joy : joy * .45f;
            float proud = gorillaVariant == 2 ? joy : 0f;
            gorillaMascot.transform.position = gorillaRestPosition;
            gorillaMascot.transform.rotation = gorillaRestRotation * Quaternion.Euler(0, sway * 4f, 0);
            SetBone(gorillaSpine, gorillaSpineRest, new Vector3(0, sway * 5f, -5f * bounce + 7f * proud), envelope);
            SetBone(gorillaNeck, gorillaNeckRest, new Vector3(0, -sway * 4f, -4f * proud), envelope);
            SetBone(gorillaHead, gorillaHeadRest, new Vector3(0, -sway * 6f, -7f * bounce - 5f * proud), envelope);
            SetBone(gorillaHipLeft, gorillaHipLeftRest, new Vector3(0, 0, -5f * bounce), envelope);
            SetBone(gorillaHipRight, gorillaHipRightRest, new Vector3(0, 0, 5f * bounce), envelope);
            SetBone(gorillaKneeLeft, gorillaKneeLeftRest, new Vector3(0, 0, 13f * bounce), envelope);
            SetBone(gorillaKneeRight, gorillaKneeRightRest, new Vector3(0, 0, -13f * bounce), envelope);
            SetBone(gorillaShoulderLeft, gorillaLeftRest, new Vector3(0, -34f * raised - 18f * clap, 30f * raised + 22f * clap), envelope);
            SetBone(gorillaShoulderRight, gorillaRightRest, new Vector3(0, 34f * raised + 18f * clap, -30f * raised - 22f * clap), envelope);
            SetBone(gorillaElbowLeft, gorillaElbowLeftRest, new Vector3(0, 0, 42f * clap + 18f * raised), envelope);
            SetBone(gorillaElbowRight, gorillaElbowRightRest, new Vector3(0, 0, -42f * clap - 18f * raised), envelope);
            SetBone(gorillaWristLeft, gorillaWristLeftRest, new Vector3(0, sway * 9f, 0), envelope);
            SetBone(gorillaWristRight, gorillaWristRightRest, new Vector3(0, -sway * 9f, 0), envelope);
        }
        else
        {
            gorillaMascot.transform.position = Vector3.Lerp(gorillaMascot.transform.position, gorillaRestPosition, Time.deltaTime * 8f);
            gorillaMascot.transform.rotation = Quaternion.Slerp(gorillaMascot.transform.rotation, gorillaRestRotation, Time.deltaTime * 8f);
            if (gorillaHead != null) gorillaHead.localRotation = Quaternion.Slerp(gorillaHead.localRotation, gorillaHeadRest, Time.deltaTime * 8f);
            if (gorillaSpine != null) gorillaSpine.localRotation = Quaternion.Slerp(gorillaSpine.localRotation, gorillaSpineRest, Time.deltaTime * 8f);
            if (gorillaShoulderLeft != null) gorillaShoulderLeft.localRotation = Quaternion.Slerp(gorillaShoulderLeft.localRotation, gorillaLeftRest, Time.deltaTime * 8f);
            if (gorillaShoulderRight != null) gorillaShoulderRight.localRotation = Quaternion.Slerp(gorillaShoulderRight.localRotation, gorillaRightRest, Time.deltaTime * 8f);
            RestoreGorillaBones();
        }
    }

    private void AnimateBoxingGorilla(int boxingGorillaAction, float gorillaAttackResolveAt)
    {
        float time = Time.time;
        // 1. Ritmo y respiración orgánica de boxeador pesado
        float bounceCycle = Mathf.Sin(time * 3.6f);
        float breatheCycle = Mathf.Sin(time * 2.2f);
        float boxerBobY = Mathf.Abs(bounceCycle) * 0.05f;
        float boxerSwayX = Mathf.Sin(time * 1.8f) * 0.06f;

        // 2. Transición del ataque (1=Golpe, 2=Guardia alta, 3=Pecho territorial, 0=Stance)
        float attackProgress = boxingGorillaAction == 1
            ? Mathf.Clamp01(1f - (gorillaAttackResolveAt - time) / 0.65f)
            : 0f;
        float punchThrust = 0f;
        float punchWindup = 0f;
        if (boxingGorillaAction == 1)
        {
            if (attackProgress < 0.42f)
                punchWindup = Mathf.SmoothStep(0f, 1f, attackProgress / 0.42f);
            else if (attackProgress < 0.70f)
            {
                punchWindup = 1f;
                punchThrust = Mathf.SmoothStep(0f, 1f, (attackProgress - 0.42f) / 0.28f);
            }
            else
            {
                float recover = (attackProgress - 0.70f) / 0.30f;
                punchThrust = Mathf.SmoothStep(1f, 0f, recover);
                punchWindup = Mathf.SmoothStep(1f, 0f, recover);
            }
        }

        float guard = boxingGorillaAction == 2 ? 1f : 0.32f;
        float territory = boxingGorillaAction == 3 ? 1f : 0f;

        // 3. Impacto y retroceso elástico al recibir golpe del jugador
        float hitEnvelope = Mathf.Clamp01((gorillaReactionUntil - time) / 0.38f);
        float hitShake = hitEnvelope > 0f ? Mathf.Sin((0.38f - (gorillaReactionUntil - time)) * 34f) * hitEnvelope : 0f;
        float hitRecoil = hitEnvelope * 0.22f;

        // Desplazamiento y rotación del cuerpo completo
        Vector3 rootPos = gorillaRestPosition
            + new Vector3(boxerSwayX + hitShake * 0.14f, boxerBobY - hitRecoil * 0.1f, -hitRecoil);
        gorillaMascot.transform.position = rootPos;
        gorillaMascot.transform.rotation = gorillaRestRotation
            * Quaternion.Euler(hitRecoil * 12f, (boxerSwayX * 18f) + (hitShake * 12f), -hitShake * 9f);

        // Columna vertebral: respiración, torsión con el puño y retroceso elástico
        float spineYaw = (punchThrust * 24f) - (punchWindup * 18f);
        float spinePitch = (-4f - breatheCycle * 3f) + (punchThrust * 14f) + (hitEnvelope * 18f) + (guard * 5f);
        SetBone(gorillaSpine, gorillaSpineRest, new Vector3(hitShake * 7f, spineYaw, spinePitch), 1f);

        // Cabeza: mira al jugador, cabeceo de guardia y sacudida al impacto
        float headPitch = (-spinePitch * 0.65f) - (hitEnvelope * 24f) + (attackProgress > 0 ? 6f : 0f);
        float headYaw = -spineYaw * 0.45f - hitShake * 14f;
        SetBone(gorillaHead, gorillaHeadRest, new Vector3(headYaw, hitShake * 9f, headPitch), 1f);

        // Rodillas: flexión de piernas en compás de boxeo
        float kneeFlex = 7f + boxerBobY * 85f;
        SetBone(gorillaKneeLeft, gorillaKneeLeftRest, new Vector3(0, 0, kneeFlex), 1f);
        SetBone(gorillaKneeRight, gorillaKneeRightRest, new Vector3(0, 0, -kneeFlex), 1f);

        // Brazos y guantes
        if (territory > 0f)
        {
            // Tamborileo furioso en el pecho alternando puños
            float thumpLeft = Mathf.Max(0f, Mathf.Sin(time * 16f));
            float thumpRight = Mathf.Max(0f, Mathf.Sin(time * 16f + Mathf.PI));
            SetBone(gorillaShoulderLeft, gorillaLeftRest, new Vector3(-22f * thumpLeft, 14f, 26f + 36f * thumpLeft), 1f);
            SetBone(gorillaShoulderRight, gorillaRightRest, new Vector3(22f * thumpRight, -14f, -26f - 36f * thumpRight), 1f);
            SetBone(gorillaElbowLeft, gorillaElbowLeftRest, new Vector3(0, 0, 56f * thumpLeft + 22f), 1f);
            SetBone(gorillaElbowRight, gorillaElbowRightRest, new Vector3(0, 0, -56f * thumpRight - 22f), 1f);
        }
        else if (boxingGorillaAction == 1) // Golpe pesado derecho con guardia izquierda
        {
            // Brazo izquierdo mantiene guardia firme protegiendo mentón
            SetBone(gorillaShoulderLeft, gorillaLeftRest, new Vector3(-12f, -26f, 34f), 1f);
            SetBone(gorillaElbowLeft, gorillaElbowLeftRest, new Vector3(0, 0, 50f), 1f);
            SetBone(gorillaWristLeft, gorillaWristLeftRest, new Vector3(0, 16f, 0), 1f);

            // Brazo derecho: carga hacia atrás y lanza recto explosivo hacia el jugador
            float rShoulderPitch = -punchWindup * 38f + punchThrust * 68f;
            float rShoulderYaw = punchWindup * 26f - punchThrust * 32f;
            float rElbowBend = punchWindup * 58f - punchThrust * 52f;
            SetBone(gorillaShoulderRight, gorillaRightRest, new Vector3(rShoulderYaw, 24f, -22f - rShoulderPitch), 1f);
            SetBone(gorillaElbowRight, gorillaElbowRightRest, new Vector3(0, 0, -24f - rElbowBend), 1f);
            SetBone(gorillaWristRight, gorillaWristRightRest, new Vector3(0, -punchThrust * 22f, 0), 1f);
        }
        else // Postura de boxeo / Guardia alta (acción 2 o descanso)
        {
            float guardElevate = guard * 30f;
            float guardTuck = guard * 34f;
            SetBone(gorillaShoulderLeft, gorillaLeftRest, new Vector3(0, -24f * guard, 16f + guardElevate), 1f);
            SetBone(gorillaShoulderRight, gorillaRightRest, new Vector3(0, 24f * guard, -16f - guardElevate), 1f);
            SetBone(gorillaElbowLeft, gorillaElbowLeftRest, new Vector3(0, 0, 26f + guardTuck), 1f);
            SetBone(gorillaElbowRight, gorillaElbowRightRest, new Vector3(0, 0, -26f - guardTuck), 1f);
            SetBone(gorillaWristLeft, gorillaWristLeftRest, new Vector3(0, 12f * guard, 0), 1f);
            SetBone(gorillaWristRight, gorillaWristRightRest, new Vector3(0, -12f * guard, 0), 1f);
        }
    }

    private void ApplyBowlingGorillaMaterials()
    {
        Material material = JungleStageBuilder.GetBowlingGorillaSharedMaterial(preferStandard: false);
        if (material == null) return;
        JungleStageBuilder.ApplySharedMaterial(gorillaMascot.GetComponentsInChildren<Renderer>(true), material);
    }

    private void SetupBowlingGorillaAnimations()
    {
        bowlingGorillaAnimator = gorillaMascot.GetComponentInChildren<Animator>(true);
        if (bowlingGorillaAnimator == null) bowlingGorillaAnimator = gorillaMascot.AddComponent<Animator>();
        JungleStageBuilder.LoadBowlingGorillaClips(out bowlingGorillaIdleClip, out bowlingGorillaAngryClip, out bowlingGorillaDanceClip);

        bowlingGorillaUsingImportedAnimation = bowlingGorillaAnimator != null && bowlingGorillaAngryClip != null && bowlingGorillaDanceClip != null;
        if (!bowlingGorillaUsingImportedAnimation)
        {
            Debug.LogWarning("El gorila nuevo no contiene todos los clips esperados; se usarán las reacciones procedurales.");
            return;
        }
        PlayBowlingGorillaClip(bowlingGorillaIdleClip != null ? bowlingGorillaIdleClip : bowlingGorillaDanceClip, 1.15f, true);
    }

    private void PlayBowlingGorillaClip(AnimationClip clip, float speed, bool loop)
    {
        if (clip == null || bowlingGorillaAnimator == null) return;
        if (bowlingGorillaGraph.IsValid()) bowlingGorillaGraph.Destroy();
        bowlingGorillaGraph = PlayableGraph.Create("Animaciones del gorila de Bowling");
        bowlingGorillaGraph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
        AnimationPlayableOutput output = AnimationPlayableOutput.Create(bowlingGorillaGraph, "Gorila", bowlingGorillaAnimator);
        bowlingGorillaPlayable = AnimationClipPlayable.Create(bowlingGorillaGraph, clip);
        bowlingGorillaPlayable.SetApplyFootIK(true);
        bowlingGorillaPlayable.SetSpeed(speed);
        output.SetSourcePlayable(bowlingGorillaPlayable);
        bowlingGorillaGraph.Play();
        bowlingGorillaClipStartedAt = Time.time;
        bowlingGorillaClipDuration = loop ? -clip.length / Mathf.Max(.01f, speed) : clip.length / Mathf.Max(.01f, speed);
    }

    private Transform FindBone(params string[] boneNames) =>
        gorillaMascot != null ? JungleStageBuilder.FindBone(gorillaMascot.transform, boneNames) : null;

    private static void SetBone(Transform bone, Quaternion rest, Vector3 euler, float weight)
    {
        if (bone != null) bone.localRotation = Quaternion.Slerp(rest, rest * Quaternion.Euler(euler), weight);
    }

    private static float SmoothPulse(float value, float start, float peakIn, float peakOut, float end)
    {
        if (value <= start || value >= end) return 0f;
        if (value < peakIn) return Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(start, peakIn, value));
        if (value <= peakOut) return 1f;
        return Mathf.SmoothStep(1f, 0f, Mathf.InverseLerp(peakOut, end, value));
    }

    private void RestoreGorillaBones()
    {
        float t = Time.deltaTime * 8f;
        if (gorillaNeck != null) gorillaNeck.localRotation = Quaternion.Slerp(gorillaNeck.localRotation, gorillaNeckRest, t);
        if (gorillaHipLeft != null) gorillaHipLeft.localRotation = Quaternion.Slerp(gorillaHipLeft.localRotation, gorillaHipLeftRest, t);
        if (gorillaHipRight != null) gorillaHipRight.localRotation = Quaternion.Slerp(gorillaHipRight.localRotation, gorillaHipRightRest, t);
        if (gorillaKneeLeft != null) gorillaKneeLeft.localRotation = Quaternion.Slerp(gorillaKneeLeft.localRotation, gorillaKneeLeftRest, t);
        if (gorillaKneeRight != null) gorillaKneeRight.localRotation = Quaternion.Slerp(gorillaKneeRight.localRotation, gorillaKneeRightRest, t);
        if (gorillaElbowLeft != null) gorillaElbowLeft.localRotation = Quaternion.Slerp(gorillaElbowLeft.localRotation, gorillaElbowLeftRest, t);
        if (gorillaElbowRight != null) gorillaElbowRight.localRotation = Quaternion.Slerp(gorillaElbowRight.localRotation, gorillaElbowRightRest, t);
        if (gorillaWristLeft != null) gorillaWristLeft.localRotation = Quaternion.Slerp(gorillaWristLeft.localRotation, gorillaWristLeftRest, t);
        if (gorillaWristRight != null) gorillaWristRight.localRotation = Quaternion.Slerp(gorillaWristRight.localRotation, gorillaWristRightRest, t);
    }

    public void Dispose()
    {
        if (bowlingGorillaGraph.IsValid()) bowlingGorillaGraph.Destroy();
        gorillaImport?.Dispose();
    }
}
