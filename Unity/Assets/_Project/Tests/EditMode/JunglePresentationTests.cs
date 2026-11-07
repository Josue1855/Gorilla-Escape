using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace GorillaEscape.Tests
{
    public sealed class JunglePresentationTests
    {
        [Test]
        public void RetainedTreeReusesElementsAndHidesRemovedState()
        {
            var root = new VisualElement();
            var nodes = new List<JungleRetainedUI.Node>();
            JungleRetainedUI.Begin(root, nodes);
            JungleRetainedUI.Label(new Rect(0, 0, 200, 40), "Connecting");
            JungleRetainedUI.Button(new Rect(0, 50, 200, 40), "Continue");
            JungleRetainedUI.End();
            var label = root[0]; var button = root[1];
            JungleRetainedUI.Begin(root, nodes);
            JungleRetainedUI.Label(new Rect(0, 0, 200, 40), "Ready");
            JungleRetainedUI.End();
            Assert.That(root.childCount, Is.EqualTo(2));
            Assert.That(root[0], Is.SameAs(label));
            Assert.That(((Label)label).text, Is.EqualTo("Ready"));
            Assert.That(button.style.display.value, Is.EqualTo(DisplayStyle.None));
            JungleRetainedUI.Begin(root, nodes);
            JungleRetainedUI.Label(new Rect(0, 0, 200, 40), "Ready");
            JungleRetainedUI.Button(new Rect(0, 50, 200, 40), "Continue");
            JungleRetainedUI.End();
            Assert.That(root[1], Is.SameAs(button));
            Assert.That(button.style.display.value, Is.EqualTo(DisplayStyle.Flex));
        }
        [Test]
        public void CameraPreviewPreservesMirroredCoordinatesWithoutMirroringSkeletonLayout()
        {
            var root = new VisualElement(); var nodes = new List<JungleRetainedUI.Node>();
            var uv = new Rect(1, 0, -1, 1);
            JungleRetainedUI.Begin(root, nodes);
            JungleRetainedUI.DrawTextureWithTexCoords(new Rect(10, 20, 100, 80), Texture2D.whiteTexture, uv);
            JungleRetainedUI.Label(new Rect(10, 20, 20, 20), "Hand");
            JungleRetainedUI.End();
            Assert.That(((Image)root[0]).uv, Is.EqualTo(uv));
            Assert.That(root[1].style.left.value.value, Is.EqualTo(10));
        }
        [Test]
        public void SceneReferenceSurvivesRenameAndInactiveObject()
        {
            var host = new GameObject("bindings"); var target = new GameObject("Original");
            try {
                var bindings = host.AddComponent<JungleSceneBindings>();
                bindings.SetEntries(new[] { new JungleSceneBindings.Entry { key = "stable-key", target = target } });
                target.name = "Renamed"; target.SetActive(false);
                Assert.That(bindings.Get("stable-key"), Is.SameAs(target));
            }
            finally { Object.DestroyImmediate(host); Object.DestroyImmediate(target); }
        }
        [Test]
        public void PrefabReferenceResolvesTheFullCastawayHierarchy()
        {
            var prefab = JungleAssets.Load<GameObject>("LogBalance/Prefabs/Vagabundo");
            Assert.That(prefab, Is.Not.Null);
            Assert.That(prefab.GetComponent<GorillaEscape.Minigames.LogBalance.LogBalanceCharacter>(), Is.Not.Null);
            Assert.That(prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length, Is.GreaterThan(0));
        }
        [TestCase("Brand/LilitaOne", typeof(Font))]
        [TestCase("Brand/NunitoSans", typeof(Font))]
        [TestCase("Jungle/gorila_menu_hd", typeof(Texture2D))]
        [TestCase("LogBalance/CREDITS", typeof(TextAsset))]
        public void SerializedAssetRecordsResolveRequiredContent(string key, System.Type type)
        {
            var record = Resources.Load<JungleAssetReference>("JungleAssetReferences/" + key);
            Assert.That(record, Is.Not.Null, key);
            Assert.That(System.Array.Exists(record.Assets, x => x != null && type.IsInstanceOfType(x)), Is.True, key);
        }
    }
}
