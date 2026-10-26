using System;
using UnityEngine;

namespace GorillaEscape.Minigames.GorillaSmash
{
    /// <summary>
    /// Procedural visual stage for Gorilla Smash (High Striker / Ring-the-Bell machine).
    /// Constructs tower, indicator puck, anvil base, mallet, bell, and camera framing
    /// strictly according to Specification v4 Section 17 & Section 27.
    /// </summary>
    public sealed class GorillaSmashStage
    {
        readonly Transform root;
        readonly GorillaSmashRules rules;

        GameObject stageHost;
        GameObject towerColumn;
        GameObject anvilBase;
        GameObject puck;
        GameObject bell;
        GameObject malletPivot;
        Camera stageCamera;
        Vector3 defaultCameraPos;
        Quaternion defaultCameraRot;

        float hammerAngle = -65f;
        float targetHammerAngle = -65f;
        float cameraShakeTimer = 0f;
        float cameraShakeMagnitude = 0f;
        float bellFlashTimer = 0f;
        Renderer bellRenderer;
        Material bellMat;

        public Camera Camera => stageCamera;

        public GorillaSmashStage(Transform parent, GorillaSmashRules rules)
        {
            this.rules = rules ?? new GorillaSmashRules();
            root = parent;
            BuildStage();
            SetupCamera();
        }

        void BuildStage()
        {
            stageHost = new GameObject("GorillaSmashStage");
            stageHost.transform.SetParent(root, false);
            stageHost.transform.localPosition = Vector3.zero;

            // 1. Ground platform
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Ground";
            ground.transform.SetParent(stageHost.transform, false);
            ground.transform.localPosition = new Vector3(0, -0.25f, 0);
            ground.transform.localScale = new Vector3(10f, 0.5f, 10f);
            SetColor(ground, new Color(0.18f, 0.22f, 0.16f)); // Deep jungle grass green

            // 2. Anvil / striking base
            anvilBase = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            anvilBase.name = "AnvilBase";
            anvilBase.transform.SetParent(stageHost.transform, false);
            anvilBase.transform.localPosition = new Vector3(0, 0.35f, 0);
            anvilBase.transform.localScale = new Vector3(1.6f, 0.35f, 1.6f);
            SetColor(anvilBase, new Color(0.35f, 0.35f, 0.38f)); // Cast iron metal

            // Strike pad on anvil
            var pad = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pad.name = "StrikePad";
            pad.transform.SetParent(anvilBase.transform, false);
            pad.transform.localPosition = new Vector3(0, 1.05f, 0);
            pad.transform.localScale = new Vector3(0.85f, 0.15f, 0.85f);
            SetColor(pad, new Color(0.85f, 0.32f, 0.15f)); // Vibrant rubber/leather pad

            // 3. Vertical Tower Column
            float towerH = rules.towerHeight;
            towerColumn = GameObject.CreatePrimitive(PrimitiveType.Cube);
            towerColumn.name = "TowerColumn";
            towerColumn.transform.SetParent(stageHost.transform, false);
            towerColumn.transform.localPosition = new Vector3(0, towerH * 0.5f + 0.7f, 0.65f);
            towerColumn.transform.localScale = new Vector3(0.55f, towerH, 0.35f);
            SetColor(towerColumn, new Color(0.25f, 0.18f, 0.12f)); // Jungle timber wood

            // Guide rail in front of tower
            var rail = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rail.name = "GuideRail";
            rail.transform.SetParent(towerColumn.transform, false);
            rail.transform.localPosition = new Vector3(0, 0, -0.6f);
            rail.transform.localScale = new Vector3(0.15f, 1.0f, 0.15f);
            SetColor(rail, new Color(0.7f, 0.7f, 0.75f));

            // Height tier markers on tower
            CreateTierMarker(towerH * 0.25f + 0.7f, new Color(0.6f, 0.6f, 0.6f), "2500_GOOD");
            CreateTierMarker(towerH * 0.50f + 0.7f, new Color(0.2f, 0.8f, 0.2f), "5000_GREAT");
            CreateTierMarker(towerH * 0.75f + 0.7f, new Color(0.1f, 0.7f, 0.9f), "7500_PERFECT");
            CreateTierMarker(towerH * 0.95f + 0.7f, new Color(1.0f, 0.85f, 0.1f), "9500_LEGENDARY");

            // 4. Indicator Puck (slides up and down guide rail)
            puck = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            puck.name = "IndicatorPuck";
            puck.transform.SetParent(stageHost.transform, false);
            puck.transform.localPosition = new Vector3(0, 0.8f, 0.44f);
            puck.transform.localScale = new Vector3(0.7f, 0.2f, 0.7f);
            SetColor(puck, new Color(0.95f, 0.75f, 0.15f)); // Shining brass

            // 5. Bell at top
            bell = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bell.name = "TowerBell";
            bell.transform.SetParent(stageHost.transform, false);
            bell.transform.localPosition = new Vector3(0, towerH + 1.15f, 0.45f);
            bell.transform.localScale = new Vector3(1.1f, 1.0f, 1.1f);
            bellRenderer = bell.GetComponent<Renderer>();
            bellMat = bellRenderer.material;
            bellMat.color = new Color(1.0f, 0.82f, 0.18f); // Golden bell

            // 6. Mallet / Hammer with swing pivot
            malletPivot = new GameObject("MalletPivot");
            malletPivot.transform.SetParent(stageHost.transform, false);
            malletPivot.transform.localPosition = new Vector3(1.3f, 0.5f, -0.6f);

            var hammerHandle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            hammerHandle.name = "HammerHandle";
            hammerHandle.transform.SetParent(malletPivot.transform, false);
            hammerHandle.transform.localPosition = new Vector3(0, 0.85f, 0);
            hammerHandle.transform.localScale = new Vector3(0.08f, 0.85f, 0.08f);
            SetColor(hammerHandle, new Color(0.45f, 0.28f, 0.15f));

            var hammerHead = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            hammerHead.name = "HammerHead";
            hammerHead.transform.SetParent(malletPivot.transform, false);
            hammerHead.transform.localPosition = new Vector3(0, 1.7f, 0);
            hammerHead.transform.localRotation = Quaternion.Euler(0, 0, 90f);
            hammerHead.transform.localScale = new Vector3(0.45f, 0.45f, 0.45f);
            SetColor(hammerHead, new Color(0.2f, 0.2f, 0.22f));

            malletPivot.transform.localRotation = Quaternion.Euler(0, 0, hammerAngle);
        }

        void CreateTierMarker(float heightY, Color color, string name)
        {
            var marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            marker.name = "TierMarker_" + name;
            marker.transform.SetParent(stageHost.transform, false);
            marker.transform.localPosition = new Vector3(0, heightY, 0.45f);
            marker.transform.localScale = new Vector3(0.75f, 0.08f, 0.1f);
            SetColor(marker, color);
        }

        void SetupCamera()
        {
            // Specification v4 Section 27:
            // Gorilla Smash camera: Lateral / 3/4 framing; captures base anvil & top bell.
            var camObj = new GameObject("GorillaSmashCamera");
            camObj.transform.SetParent(stageHost.transform, false);
            stageCamera = camObj.AddComponent<Camera>();
            stageCamera.clearFlags = CameraClearFlags.Color;
            stageCamera.backgroundColor = new Color(0.12f, 0.14f, 0.18f); // Soft dusk background
            stageCamera.fieldOfView = 50f;

            defaultCameraPos = new Vector3(5.5f, 5.8f, -9.5f);
            defaultCameraRot = Quaternion.Euler(18f, -28f, 0f);

            camObj.transform.localPosition = defaultCameraPos;
            camObj.transform.localRotation = defaultCameraRot;
        }

        void SetColor(GameObject go, Color color)
        {
            var r = go.GetComponent<Renderer>();
            if (r != null)
            {
                var mat = new Material(Shader.Find("Standard") ?? Shader.Find("Diffuse"));
                mat.color = color;
                mat.SetFloat("_Glossiness", 0.35f);
                r.material = mat;
            }
        }

        public void TriggerHammerSwing()
        {
            targetHammerAngle = 15f; // Slam down onto the anvil
        }

        public void ResetHammer()
        {
            targetHammerAngle = -65f; // Return to raised ready angle
        }

        public void ApplyImpactShake(float intensity)
        {
            cameraShakeMagnitude = Mathf.Clamp(intensity * 0.35f, 0.05f, 0.45f);
            cameraShakeTimer = 0.22f;
        }

        public void RingBell()
        {
            bellFlashTimer = 0.8f;
        }

        public void Update(float deltaTime, float currentPuckHeight, GorillaSmashState state)
        {
            // 1. Update puck position smoothly
            if (puck != null)
            {
                float basePuckY = 0.8f;
                puck.transform.localPosition = new Vector3(0, basePuckY + currentPuckHeight, 0.44f);
            }

            // 2. Animate hammer swing
            hammerAngle = Mathf.Lerp(hammerAngle, targetHammerAngle, deltaTime * 16f);
            if (malletPivot != null)
            {
                malletPivot.transform.localRotation = Quaternion.Euler(0, 0, hammerAngle);
            }

            if (state == GorillaSmashState.Elevating || state == GorillaSmashState.AttemptFinished)
            {
                ResetHammer();
            }

            // 3. Bell flash animation when hit
            if (bellFlashTimer > 0f)
            {
                bellFlashTimer -= deltaTime;
                if (bellMat != null)
                {
                    float pulse = (Mathf.Sin(Time.time * 25f) + 1f) * 0.5f;
                    bellMat.color = Color.Lerp(new Color(1.0f, 0.82f, 0.18f), Color.white, pulse);
                }
            }
            else if (bellMat != null)
            {
                bellMat.color = new Color(1.0f, 0.82f, 0.18f);
            }

            // 4. Camera shake update
            if (cameraShakeTimer > 0f && stageCamera != null)
            {
                cameraShakeTimer -= deltaTime;
                Vector3 shakeOffset = UnityEngine.Random.insideUnitSphere * cameraShakeMagnitude;
                stageCamera.transform.localPosition = defaultCameraPos + shakeOffset;
            }
            else if (stageCamera != null)
            {
                stageCamera.transform.localPosition = defaultCameraPos;
            }
        }

        public void Destroy()
        {
            if (stageHost != null)
            {
                UnityEngine.Object.Destroy(stageHost);
                stageHost = null;
            }
        }
    }
}
