using System;
using System.Collections.Generic;
using UnityEngine;
using GorillaEscape.Minigames.LogBalance;

namespace GorillaEscape.UI
{
    /// <summary>
    /// 3D Tropical Jungle backdrop for the Main Menu (matching reference art direction):
    /// Low-poly Gorilla character standing on a sandy beach/wooden pier in the center-left,
    /// flanked by glowing tiki torches, lush tropical palms, warm sunlight, and ocean horizon.
    /// </summary>
    public sealed class MainMenuBackdrop : MonoBehaviour
    {
        Camera menuCamera;
        GameObject stageRoot;
        GameObject gorilla;
        LogBalanceSpectator gorillaSpectator;
        LogBalanceCharacter castaway;
        readonly List<Light> torchLights = new List<Light>();

        void Awake()
        {
            BuildStage();
        }

        void BuildStage()
        {
            stageRoot = new GameObject("3D Menu Stage");
            stageRoot.transform.SetParent(transform, false);

            // 1. Perspective Camera - positioned to view the full gorilla from head to toe + island panorama
            var camObj = new GameObject("Menu Perspective Camera");
            camObj.transform.SetParent(stageRoot.transform, false);
            menuCamera = camObj.AddComponent<Camera>();
            menuCamera.clearFlags = CameraClearFlags.SolidColor;
            menuCamera.backgroundColor = new Color(0.36f, 0.72f, 0.94f); // Vibrant Caribbean sky
            menuCamera.fieldOfView = 42f;
            menuCamera.nearClipPlane = 0.3f;
            menuCamera.farClipPlane = 150f;
            menuCamera.transform.position = new Vector3(-0.35f, 1.6f, -4.8f);
            menuCamera.transform.LookAt(new Vector3(-0.85f, 1.25f, 1.2f));

            // 2. Warm Tropical Sunlight
            var sunObj = new GameObject("Tropical Sunlight");
            sunObj.transform.SetParent(stageRoot.transform, false);
            var light = sunObj.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1.0f, 0.96f, 0.86f);
            light.intensity = 1.35f;
            sunObj.transform.rotation = Quaternion.Euler(38f, -28f, 0f);

            // 3. Turquoise Lagoon Ocean (extends to the horizon)
            var ocean = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ocean.name = "Lagoon Ocean";
            ocean.transform.SetParent(stageRoot.transform, false);
            ocean.transform.position = new Vector3(0f, -0.1f, 25f);
            ocean.transform.localScale = new Vector3(120f, 0.1f, 60f);
            var oceanMat = new Material(Shader.Find("Standard"));
            oceanMat.color = new Color(0.18f, 0.65f, 0.68f); // Tropical turquoise lagoon
            oceanMat.SetFloat("_Glossiness", 0.75f);
            oceanMat.SetFloat("_Metallic", 0.1f);
            ocean.GetComponent<MeshRenderer>().sharedMaterial = oceanMat;

            // 4. Sandy Beach Island Ground
            var beach = GameObject.CreatePrimitive(PrimitiveType.Cube);
            beach.name = "Sandy Beach Shore";
            beach.transform.SetParent(stageRoot.transform, false);
            beach.transform.position = new Vector3(0f, -0.4f, 4f);
            beach.transform.localScale = new Vector3(45f, 0.8f, 20f);
            var sandMat = new Material(Shader.Find("Standard"));
            sandMat.color = new Color(0.89f, 0.78f, 0.58f); // Warm sunlit sand
            sandMat.SetFloat("_Glossiness", 0.05f);
            beach.GetComponent<MeshRenderer>().sharedMaterial = sandMat;

            // 5. Foreground Wooden Deck Pier where Gorilla stands
            var deck = GameObject.CreatePrimitive(PrimitiveType.Cube);
            deck.name = "Wooden Deck Pier";
            deck.transform.SetParent(stageRoot.transform, false);
            deck.transform.position = new Vector3(-1.6f, 0.04f, 1.2f);
            deck.transform.localScale = new Vector3(4.8f, 0.1f, 3.6f);
            var deckMat = new Material(Shader.Find("Standard"));
            deckMat.color = new Color(0.46f, 0.28f, 0.14f); // Warm teak plank
            deckMat.SetFloat("_Glossiness", 0.12f);
            deck.GetComponent<MeshRenderer>().sharedMaterial = deckMat;

            // 6. The 3D Gorilla Character (Left-Center, full body visible from head to toe)
            var gorillaPrefab = Resources.Load<GameObject>("LogBalance/Prefabs/gorilla_low_ploy");
            if (gorillaPrefab != null)
            {
                gorilla = Instantiate(gorillaPrefab, stageRoot.transform, false);
                gorilla.name = "Menu Gorilla Host";
                gorilla.transform.position = new Vector3(-1.6f, 0.09f, 1.2f);
                gorilla.transform.rotation = Quaternion.Euler(0f, 165f, 0f);
                gorilla.transform.localScale = Vector3.one * 0.72f;

                gorillaSpectator = gorilla.GetComponent<LogBalanceSpectator>();
                if (gorillaSpectator != null)
                {
                    gorillaSpectator.BindRig();
                }
            }

            var humanPrefab=Resources.Load<GameObject>("LogBalance/Prefabs/Vagabundo");
            if(humanPrefab!=null){
                var human=Instantiate(humanPrefab,stageRoot.transform,false);human.name="Menu human castaway";
                human.transform.position=new Vector3(-.35f,.09f,.8f);human.transform.rotation=Quaternion.Euler(0,180,0);
                castaway=human.GetComponent<LogBalanceCharacter>();
                var renderers=human.GetComponentsInChildren<Renderer>();
                if(renderers.Length>0){var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
                    if(bounds.size.y>.01f)human.transform.localScale*=1.65f/bounds.size.y;}
            }
            // A broken mast and a torn sail tell the shipwreck story behind the castaway.
            var mast=GameObject.CreatePrimitive(PrimitiveType.Cube);mast.name="Shipwreck mast";mast.transform.SetParent(stageRoot.transform,false);
            mast.transform.position=new Vector3(-3,.8f,4);mast.transform.localScale=new Vector3(.09f,1.6f,.09f);mast.transform.rotation=Quaternion.Euler(0,0,-12);
            var mastMaterial=new Material(Shader.Find("Standard"));mastMaterial.color=new Color(.42f,.25f,.16f);mast.GetComponent<Renderer>().sharedMaterial=mastMaterial;
            var sail=new GameObject("Torn castaway sail");sail.transform.SetParent(stageRoot.transform,false);
            var mesh=new Mesh();mesh.vertices=new[]{new Vector3(-2.86f,1.5f,3.95f),new Vector3(-3.65f,.35f,3.95f),new Vector3(-2.98f,.5f,3.95f)};mesh.triangles=new[]{0,1,2,2,1,0};mesh.RecalculateNormals();
            sail.AddComponent<MeshFilter>().sharedMesh=mesh;var cloth=new Material(Shader.Find("Standard"));cloth.color=new Color(.96f,.9f,.77f);sail.AddComponent<MeshRenderer>().sharedMaterial=cloth;

            // 7. Tiki Torches with flickering fire (flanking the wooden deck)
            SpawnTorch(new Vector3(-3.5f, 0.09f, 0.2f));
            SpawnTorch(new Vector3(0.3f, 0.09f, 0.2f));

            // 8. Lush Palm Trees in Background (Towering scales 5.0 to 8.0)
            SpawnTree("Tree_00", new Vector3(-6.5f, 0f, 4.0f), 6.5f, 25f);
            SpawnTree("Tree_04", new Vector3(-8.5f, 0f, 2.0f), 7.0f, 50f);
            SpawnTree("Tree_08", new Vector3(-4.0f, 0f, 8.5f), 8.0f, 85f);
            SpawnTree("Tree_02", new Vector3(2.5f, 0f, 9.0f), 8.5f, 135f);
            SpawnTree("Tree_06", new Vector3(7.5f, 0f, 6.5f), 7.5f, 210f);
            SpawnTree("Tree_10", new Vector3(9.5f, 0f, 3.5f), 6.5f, 280f);

            // 9. Grass Tufts along the deck edge
            SpawnGrass("Grass_00", new Vector3(-3.2f, 0.09f, -0.2f), 1.0f);
            SpawnGrass("Grass_01", new Vector3(-2.2f, 0.09f, -0.4f), 0.9f);
            SpawnGrass("Grass_02", new Vector3(-0.4f, 0.09f, -0.3f), 1.1f);
            SpawnGrass("Grass_03", new Vector3(0.8f, 0.09f, 0.2f), 1.0f);

            // 10. Distant Mountain Canopy Wall
            var mountain = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mountain.name = "Distant Island Mountain";
            mountain.transform.SetParent(stageRoot.transform, false);
            mountain.transform.position = new Vector3(0f, 6.0f, 45f);
            mountain.transform.localScale = new Vector3(90f, 16f, 15f);
            var mtnMat = new Material(Shader.Find("Standard"));
            mtnMat.color = new Color(0.18f, 0.38f, 0.22f); // Deep jungle canopy mountain
            mountain.GetComponent<MeshRenderer>().sharedMaterial = mtnMat;
        }

        void SpawnTorch(Vector3 pos)
        {
            var poleMat = new Material(Shader.Find("Standard")) { color = new Color(0.38f, 0.22f, 0.11f) };

            // Pole
            var pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.name = "Tiki Pole";
            pole.transform.SetParent(stageRoot.transform, false);
            pole.transform.position = pos + new Vector3(0f, 0.85f, 0f);
            pole.transform.localScale = new Vector3(0.1f, 0.85f, 0.1f);
            pole.GetComponent<MeshRenderer>().sharedMaterial = poleMat;

            // Bowl
            var bowl = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            bowl.name = "Tiki Bowl";
            bowl.transform.SetParent(stageRoot.transform, false);
            bowl.transform.position = pos + new Vector3(0f, 1.75f, 0f);
            bowl.transform.localScale = new Vector3(0.26f, 0.09f, 0.26f);
            bowl.GetComponent<MeshRenderer>().sharedMaterial = poleMat;

            // Flame
            var flame = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            flame.name = "Tiki Flame";
            flame.transform.SetParent(stageRoot.transform, false);
            flame.transform.position = pos + new Vector3(0f, 1.92f, 0f);
            flame.transform.localScale = new Vector3(0.22f, 0.30f, 0.22f);
            var flameMat = new Material(Shader.Find("Standard")) { color = new Color(1.0f, 0.6f, 0.1f) };
            flameMat.EnableKeyword("_EMISSION");
            flameMat.SetColor("_EmissionColor", new Color(1.0f, 0.55f, 0.1f) * 2.2f);
            flame.GetComponent<MeshRenderer>().sharedMaterial = flameMat;

            // Light
            var torchLightObj = new GameObject("Tiki Light");
            torchLightObj.transform.SetParent(stageRoot.transform, false);
            torchLightObj.transform.position = pos + new Vector3(0f, 2.0f, 0f);
            var torchLight = torchLightObj.AddComponent<Light>();
            torchLight.type = LightType.Point;
            torchLight.color = new Color(1.0f, 0.65f, 0.2f);
            torchLight.intensity = 1.8f;
            torchLight.range = 6.0f;
            torchLights.Add(torchLight);
        }

        void SpawnTree(string name, Vector3 pos, float scale, float rotY)
        {
            var prefab = Resources.Load<GameObject>("LogBalance/Prefabs/" + name);
            if (prefab == null) return;
            var t = Instantiate(prefab, stageRoot.transform, false);
            t.transform.position = pos;
            t.transform.localScale = Vector3.one * scale;
            t.transform.rotation = Quaternion.Euler(0f, rotY, 0f);
        }

        void SpawnGrass(string name, Vector3 pos, float scale)
        {
            var prefab = Resources.Load<GameObject>("LogBalance/Prefabs/" + name);
            if (prefab == null) return;
            var g = Instantiate(prefab, stageRoot.transform, false);
            g.transform.position = pos;
            g.transform.localScale = Vector3.one * scale;
        }

        void Update()
        {
            if(castaway!=null)castaway.Pose(Time.time,0,false,false,0,false);
            // Gorilla idle breathing & natural animation
            if (gorillaSpectator != null)
            {
                gorillaSpectator.Pose(Time.time, 0f, 0f, false, 0f, false);
            }
            else if (gorilla != null)
            {
                float idleSway = Mathf.Sin(Time.time * 1.5f) * 1.5f;
                gorilla.transform.rotation = Quaternion.Euler(0f, 165f + idleSway, 0f);
            }

            // Tiki torch flicker
            if (torchLights.Count > 0)
            {
                float flicker = Mathf.Sin(Time.time * 9f) * 0.15f + Mathf.Cos(Time.time * 13f) * 0.1f;
                for (int i = 0; i < torchLights.Count; i++)
                {
                    if (torchLights[i] != null)
                        torchLights[i].intensity = 1.8f + flicker * (i % 2 == 0 ? 1f : -0.8f);
                }
            }
        }

        public void SetVisible(bool visible)
        {
            if (stageRoot != null) stageRoot.SetActive(visible);
            if (menuCamera != null)
            {
                menuCamera.enabled = true;
                menuCamera.backgroundColor = visible ? new Color(0.36f, 0.72f, 0.94f) : new Color(0.04f, 0.08f, 0.07f);
            }
        }

        void OnDestroy()
        {
            if (stageRoot != null) Destroy(stageRoot);
        }
    }
}
