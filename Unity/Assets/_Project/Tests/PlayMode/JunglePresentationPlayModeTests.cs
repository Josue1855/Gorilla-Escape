using System.Collections;
using System.Reflection;
using System.Linq;
using GorillaEscape.Contracts;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace GorillaEscape.Tests
{
    public sealed class JunglePresentationPlayModeTests
    {
        static System.Type TournamentType => System.AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("TournamentSession")).First(t => t != null);
        [UnityTest]
        public IEnumerator ModeExitKeepsTheSameRoomAndItsPreparation()
        {
            loadedGameplayForPauseTest = true;
            yield return UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("Connection");
            yield return null;
            var host = System.Array.Find(Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None), b => b.GetType().Name == "PortableServiceHost");
            Assert.That(host, Is.Not.Null);
            string room = (string)host.GetType().GetProperty("RoomCode").GetValue(host);
            var preparation = (GorillaEscape.Gameplay.Input.SessionPreparation)host.GetType().GetField("preparation", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(host);
            var lobby = new LobbySnapshotWire {sessionId="prepared-room",players=new[]{new LobbyPlayerWire{playerId=1,deviceSessionId="same-phone",displayName="Josué",connectionState="ACTIVE",networkReady=true,inputReady=true}}};
            preparation.Record(true,lobby);
            TournamentType.GetMethod("StartStoryMode").Invoke(null,new object[]{1,null});
            TournamentType.GetMethod("BeginRequestedMode").Invoke(null,new object[]{true,lobby});
            yield return null;
            var navigation = System.AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("SceneNavigator")).First(t => t != null);
            navigation.GetMethod("Load").Invoke(null,new object[]{"MainMenu"});
            yield return null; yield return null;
            Assert.That(TournamentType.GetProperty("IsActive").GetValue(null),Is.EqualTo(false));
            Assert.That(host,Is.SameAs(System.Array.Find(Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None), b => b.GetType().Name == "PortableServiceHost")));
            Assert.That(host.GetType().GetProperty("RoomCode").GetValue(host),Is.EqualTo(room));
            Assert.That(preparation.CanReuse(lobby,true,true,1),Is.True,"changing modes must not discard prepared devices");
        }
        [UnityTest]
        public IEnumerator StoryAndPartyWaitAtConnectionUntilRequiredDevicesAreReady()
        {
            loadedGameplayForPauseTest = true;
            var type = TournamentType;
            foreach (string method in new[] { "StartStoryMode", "StartPartyMode" }) {
                type.GetMethod(method).Invoke(null, new object[] { 2, null });
                yield return null; yield return null;
                Assert.That(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name, Is.EqualTo("Connection"));
                Assert.That(type.GetProperty("IsActive").GetValue(null), Is.EqualTo(false));
                Assert.That(type.GetMethod("BeginRequestedMode").Invoke(null, new object[] { false, null }), Is.EqualTo(false));
                var lobby = new LobbySnapshotWire { players = new[] {
                    new LobbyPlayerWire { playerId=1, displayName="Uno", connectionState="ACTIVE", networkReady=true, inputReady=true },
                    new LobbyPlayerWire { playerId=2, displayName="Dos", connectionState="ACTIVE", networkReady=true, inputReady=false }
                }};
                Assert.That(type.GetMethod("ConnectionRequirement").Invoke(null, new object[] { true, lobby }), Is.Not.Null);
                lobby.players[1].inputReady = true;
                Assert.That(type.GetMethod("ConnectionRequirement").Invoke(null, new object[] { false, lobby }), Is.Not.Null);
                Assert.That(type.GetMethod("BeginRequestedMode").Invoke(null, new object[] { true, lobby }), Is.EqualTo(true));
                yield return null;
                Assert.That(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name, Is.EqualTo("GorillaSmash"));
            }
        }
        [UnityTest]
        public IEnumerator SplashRunnerUsesMatchingSkinAndAnimatedStride()
        {
            loadedGameplayForPauseTest = true;
            yield return UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("Splash");
            yield return null;
            var director = System.Array.Find(Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None), b => b.GetType().Name == "SplashDirector");
            Assert.That(director, Is.Not.Null);
            director.enabled = false;
            var rig = director.GetType().GetField("gorillaRig", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(director);
            var type = rig.GetType();
            Assert.That(type.GetProperty("HasAnimatedStride").GetValue(rig), Is.EqualTo(true));
            var model = (GameObject)type.GetProperty("MascotObject").GetValue(rig);
            var skin = model.GetComponentInChildren<SkinnedMeshRenderer>();
            Assert.That(skin, Is.Not.Null);
            Assert.That(skin.bones.Length, Is.GreaterThan(12));
            var stage = System.Type.GetType("JungleStageBuilder, Assembly-CSharp", true);
            var canonical = (GameObject)stage.GetMethod("LoadBowlingGorillaPrefab").Invoke(null, null);
            Assert.That(skin.sharedMesh, Is.SameAs(canonical.GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh), "splash must use the exact gameplay Meshy mesh");
            Assert.That(skin.sharedMaterial.mainTexture.name, Does.Contain("Low_Poly_Gorilla_Embr"));
            type.GetMethod("AnimateRun").Invoke(rig, new object[] { .1f, 1.25f });
            var before = skin.bones.Select(b => b.localRotation).ToArray();
            type.GetMethod("AnimateRun").Invoke(rig, new object[] { .35f, 1.25f });
            Assert.That(skin.bones.Where((b, i) => Quaternion.Angle(before[i], b.localRotation) > 5f).Count(), Is.GreaterThan(3), "the run must move the matching skeleton, not just translate the model");
            var feet = new[] { "mixamorig:LeftFoot", "mixamorig:RightFoot" }.Select(n => skin.bones.First(b => b.name == n)).ToArray();
            float clearance = (float)type.GetField("runnerFootClearance", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(rig);
            float ground = (float)type.GetField("runnerGroundY", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(rig);
            for (int i = 0; i < 32; i++) {
                type.GetMethod("AnimateRun").Invoke(rig, new object[] { i / 32f, 1.25f });
                float sole = feet.Min(f => f.position.y) - clearance;
                Assert.That(sole, Is.InRange(ground - .001f, ground + .04f), "the stance foot must stay on the path throughout the stride");
            }
            type.GetMethod("PlayIdleAnimation").Invoke(rig, null);
            Assert.That(model.transform.localScale.magnitude, Is.GreaterThan(.01f));
            string folder = System.Environment.GetEnvironmentVariable("GORILLA_SPLASH_VERIFICATION_DIR");
            if (!string.IsNullOrEmpty(folder)) {
                System.IO.Directory.CreateDirectory(folder);
                foreach (float time in new[] { 1.8f, 2.05f, 4.2f }) {
                    director.GetType().GetMethod("UpdateCinematic", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(director, new object[] { time });
                    yield return new WaitForEndOfFrame();
                    Assert.That(skin.bounds.size.y, Is.GreaterThan(1.5f), "the imported clip must not shrink the fitted runner");
                    Assert.That(GeometryUtility.TestPlanesAABB(GeometryUtility.CalculateFrustumPlanes(Camera.main), skin.bounds), Is.True, "the runner must remain in the cinematic frame");
                    System.IO.File.AppendAllText(System.IO.Path.Combine(folder, "model-metrics.txt"), "t="+time+" root="+model.transform.position+" scale="+model.transform.lossyScale+" bounds="+skin.bounds+" viewport="+Camera.main.WorldToViewportPoint(skin.bounds.center)+"\n");
                    var image = ScreenCapture.CaptureScreenshotAsTexture();
                    System.IO.File.WriteAllBytes(System.IO.Path.Combine(folder, "run-" + time.ToString("F2", System.Globalization.CultureInfo.InvariantCulture) + ".png"), image.EncodeToPNG()); Object.Destroy(image);
                }
            }
        }
        bool loadedGameplayForPauseTest;
        [UnityTearDown]
        public IEnumerator LeaveGameplayAfterPauseVerification()
        {
            if (!loadedGameplayForPauseTest) yield break;
            loadedGameplayForPauseTest = false;
            TournamentType.GetMethod("ResetTournament").Invoke(null, null);
            yield return UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("Connection");
            for (int i = 0; i < 3; i++) yield return null;
        }
        [UnityTest]
        public IEnumerator GameplayPausePersistsAfterClickAndHeaderLeavesRoomForHud()
        {
            loadedGameplayForPauseTest = true;
            yield return UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("JungleDrums");
            for (int i = 0; i < 16; i++) yield return null;
            var session = System.Array.Find(Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None), x => x.GetType().Name == "MotionSession");
            Assert.That(session, Is.Not.Null);
            var document = session.GetComponent<UIDocument>();
            var button = document.rootVisualElement.Query<Button>().ToList().Find(x => x.text == "PAUSA · ESC");
            Assert.That(button, Is.Not.Null);
            using (var click = NavigationSubmitEvent.GetPooled()) { click.target = button; button.SendEvent(click); }
            for (int i = 0; i < 5; i++) yield return null;
            var paused = session.GetType().GetProperty("InGamePauseActive", BindingFlags.Public | BindingFlags.Static);
            Assert.That(paused.GetValue(null), Is.EqualTo(true));
            Assert.That(Time.timeScale, Is.Zero);
            var sceneDocument = System.Array.Find(Object.FindObjectsByType<UIDocument>(FindObjectsSortMode.None), x => x.gameObject.scene == UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Assert.That(sceneDocument.rootVisualElement.worldBound.yMin, Is.GreaterThanOrEqualTo(JunglePanel.GameplayHeaderHeight - 1));
            string screenshot = System.Environment.GetEnvironmentVariable("GORILLA_PAUSE_VERIFICATION_SCREENSHOT");
            if (!string.IsNullOrEmpty(screenshot)) {
                yield return new WaitForEndOfFrame();
                var shot = ScreenCapture.CaptureScreenshotAsTexture();
                System.IO.File.WriteAllBytes(screenshot, shot.EncodeToPNG()); Object.Destroy(shot);
            }
            button = document.rootVisualElement.Query<Button>().ToList().Find(x => x.text == "SEGUIR · ESC");
            Assert.That(button, Is.Not.Null);
            using (var click = NavigationSubmitEvent.GetPooled()) { click.target = button; button.SendEvent(click); }
            for (int i = 0; i < 3; i++) yield return null;
            Assert.That(paused.GetValue(null), Is.EqualTo(false));
        }
        [UnityTest]
        public IEnumerator PointerReleaseActivatesToggleOnceAndLeavesItActive()
        {
            var host = new GameObject("Retained pause click regression");
            try {
                var screen = host.AddComponent<RetainedPanelProbe>();
                screen.ToggleLabel = true;
                yield return null; yield return null;
                var button = host.GetComponent<UIDocument>().rootVisualElement.Q<Button>();
                var position = button.worldBound.center;
                using (var down = PointerDownEvent.GetPooled(new Event { type = EventType.MouseDown, button = 0, mousePosition = position }))
                { down.target = button; button.SendEvent(down); }
                yield return null; yield return null;
                Assert.That(screen.Activations, Is.Zero, "holding the pointer must not activate a toggle");
                using (var up = PointerUpEvent.GetPooled(new Event { type = EventType.MouseUp, button = 0, mousePosition = position }))
                { up.target = button; button.SendEvent(up); }
                yield return null; yield return null;
                Assert.That(screen.Activations, Is.EqualTo(1));
                Assert.That(button.text, Is.EqualTo("Resume"));
                yield return null; yield return null;
                Assert.That(screen.Activations, Is.EqualTo(1), "release must not activate the new label again");
            }
            finally { Object.Destroy(host); }
        }
        [UnityTest]
        public IEnumerator PanelCanReenableInSameFrameWithoutDuplicatingDocument()
        {
            var host = new GameObject("Retained panel lifecycle test");
            try {
                var screen = host.AddComponent<RetainedPanelProbe>();
                yield return null; yield return null;
                var panel = host.GetComponent<JunglePanel>();
                var document = host.GetComponent<UIDocument>();
                for (int i = 0; i < 3; i++) { panel.enabled = false; panel.enabled = true; }
                yield return null; yield return null;
                Assert.That(host.GetComponents<UIDocument>().Length, Is.EqualTo(1));
                Assert.That(host.GetComponent<UIDocument>(), Is.SameAs(document));
                Assert.That(document.enabled, Is.True);
                Assert.That(document.rootVisualElement.Q<Button>(), Is.Not.Null);
                Assert.That(screen.DuplicateFrame, Is.False);
            }
            finally { Object.Destroy(host); }
        }
        [UnityTest]
        public IEnumerator DynamicScreenRendersOncePerFrameAndUsesToolkitNavigation()
        {
            var host = new GameObject("Retained panel test");
            try {
                var screen = host.AddComponent<RetainedPanelProbe>();
                yield return null; yield return null; yield return null;
                Assert.That(screen.Renders, Is.GreaterThan(0));
                Assert.That(screen.DuplicateFrame, Is.False);
                var document = host.GetComponent<UIDocument>();
                Assert.That(document, Is.Not.Null);
                var button = document.rootVisualElement.Q<Button>();
                Assert.That(button, Is.Not.Null);
                using (var submit = NavigationSubmitEvent.GetPooled()) { submit.target = button; button.SendEvent(submit); }
                yield return null; yield return null;
                Assert.That(screen.Activations, Is.EqualTo(1));
                yield return null;
                Assert.That(screen.Activations, Is.EqualTo(1));
                screen.ButtonEnabled = false;
                yield return null; yield return null;
                Assert.That(button.enabledSelf, Is.False);
                screen.enabled = false;
                yield return null; yield return null;
                Assert.That(button.style.display.value, Is.EqualTo(DisplayStyle.None));
            }
            finally { Object.Destroy(host); }
        }
        [UnityTest]
        public IEnumerator PointerPickingReachesSceneButtonUnderSessionOverlay()
        {
            var host = new GameObject("Mouse scene screen");
            var overlay = new GameObject("Mouse session overlay");
            try {
                host.AddComponent<RetainedPanelProbe>();
                var overlayScreen = overlay.AddComponent<RetainedOverlayProbe>();
                for (int i = 0; i < 5; i++) yield return null;
                var sceneDocument = host.GetComponent<UIDocument>();
                var overlayDocument = overlay.GetComponent<UIDocument>();
                var button = sceneDocument.rootVisualElement.Q<Button>();
                Assert.That(overlayDocument.rootVisualElement.panel, Is.SameAs(sceneDocument.rootVisualElement.panel));
                var point = button.worldBound.center;
                Assert.That(sceneDocument.rootVisualElement.panel.Pick(point), Is.SameAs(button), "mouse must reach the button through the empty overlay");
                overlayScreen.Modal = true;
                for (int i = 0; i < 3; i++) yield return null;
                Assert.That(sceneDocument.rootVisualElement.panel.Pick(point), Is.SameAs(overlayDocument.rootVisualElement), "a real pause modal must block scene input");
            }
            finally { Object.Destroy(host); Object.Destroy(overlay); }
        }
        [UnityTest]
        public IEnumerator SavedProductScreensHaveVisibleToolkitTrees()
        {
            string output = System.Environment.GetEnvironmentVariable("GORILLA_PRESENTATION_SCREENSHOTS");
            foreach (string name in new[] { "Connection", "MainMenu", "BananaBowling", "GorillaBoxing", "JungleBaseball", "JungleDrums", "CoconutSlice", "WhackAMole", "LogBalance", "GorillaSmash", "CoconutThrow", "JungleKeeper" })
            {
                var navigation = System.AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("SceneNavigator")).First(t => t != null);
                navigation.GetMethod("Load").Invoke(null, new object[] { name });
                yield return null;
                var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                // Hide the input-loss notice to inspect the actual screen without hardware.
                foreach (var behaviour in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
                    if (behaviour.GetType().Name == "MotionSession") behaviour.enabled = false;
                for (int i = 0; i < 12; i++) yield return null;
                var document = System.Array.Find(Object.FindObjectsByType<UIDocument>(FindObjectsSortMode.None), x => x.gameObject.scene == scene);
                Assert.That(document, Is.Not.Null, name);
                Assert.That(document.rootVisualElement.childCount, Is.GreaterThan(0), name);
                Assert.That(document.rootVisualElement.panel, Is.Not.Null, name);
                if (!string.IsNullOrEmpty(output) && SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null)
                {
                    System.IO.Directory.CreateDirectory(output);
                    // Panel targets render normally in batchmode with a graphics device.
                    yield return null;
                    var shot = ScreenCapture.CaptureScreenshotAsTexture();
                    System.IO.File.AppendAllText(System.IO.Path.Combine(output, "layout.txt"), name + " screen=" + Screen.width + "x" + Screen.height + " capture=" + shot.width + "x" + shot.height + " root=" + document.rootVisualElement.worldBound + " parent=" + document.rootVisualElement.parent.worldBound + "\n");
                    System.IO.File.WriteAllBytes(System.IO.Path.Combine(output, name + ".png"), shot.EncodeToPNG());
                    Object.Destroy(shot);
                }
            }
        }
        [UnityTest]
        public IEnumerator PrewarmBuildsAllClipsAndSubsequentCallsReuseThem()
        {
            var bank = System.Type.GetType("JungleAudioBank, Assembly-CSharp", true);
            var warm = bank.GetMethod("Prewarm", BindingFlags.Public | BindingFlags.Static);
            // Simulate the owner changing scenes after preparation has started.
            bank.GetField("<IsPrewarmed>k__BackingField", BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, false);
            var interrupted = (IEnumerator)warm.Invoke(null, null);
            Assert.That(interrupted.MoveNext(), Is.True);
            (interrupted as System.IDisposable)?.Dispose();
            yield return (IEnumerator)warm.Invoke(null, null);
            Assert.That(bank.GetProperty("IsPrewarmed").GetValue(null), Is.EqualTo(true));
            var clips = new System.Collections.Generic.Dictionary<string, AudioClip>();
            foreach (var property in bank.GetProperties(BindingFlags.Public | BindingFlags.Static)) {
                if (property.PropertyType != typeof(AudioClip)) continue;
                var clip = (AudioClip)property.GetValue(null);
                Assert.That(clip, Is.Not.Null, property.Name);
                Assert.That(clip.samples, Is.GreaterThan(0), property.Name);
                clips.Add(property.Name, clip);
            }
            yield return (IEnumerator)warm.Invoke(null, null);
            foreach (var item in clips) Assert.That(bank.GetProperty(item.Key).GetValue(null), Is.SameAs(item.Value), item.Key);
        }
    }
}
