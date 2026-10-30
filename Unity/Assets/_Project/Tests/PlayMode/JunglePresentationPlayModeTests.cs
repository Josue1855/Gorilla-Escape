using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace GorillaEscape.Tests
{
    public sealed class JunglePresentationPlayModeTests
    {
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
            foreach (string name in new[] { "Connection", "MainMenu", "BananaBowling", "GorillaBoxing", "JungleBaseball", "JungleDrums", "CoconutSlice", "WhackAMole", "LogBalance" })
            {
                yield return UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(name);
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
