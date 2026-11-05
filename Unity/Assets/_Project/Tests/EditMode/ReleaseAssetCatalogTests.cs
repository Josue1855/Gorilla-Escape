using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
namespace GorillaEscape.Tests {
 public sealed class ReleaseAssetCatalogTests {
  [Test] public void ShippedAssetRecordsContainNoMissingReferences() {
   var failures=new System.Collections.Generic.List<string>();
   foreach(var guid in AssetDatabase.FindAssets("t:JungleAssetReference",new[]{"Assets/Resources/JungleAssetReferences"})) {
    var path=AssetDatabase.GUIDToAssetPath(guid);var record=AssetDatabase.LoadAssetAtPath<JungleAssetReference>(path);
    if(record.Assets.Length==0||record.Assets.Any(asset=>asset==null))failures.Add(path);
   }
   Assert.That(failures,Is.Empty,string.Join("\n",failures));
  }
 }
}
