using GorillaEscape.QA;
using UnityEngine;
namespace GorillaEscape.UI
{
    public sealed class TestsMenuController
    {
        readonly MainMenuController navigation;
        readonly QaRuntimeService runtime;
        readonly TestsMenuView view;
        Vector2 scroll;
        public TestsMenuController(MainMenuController navigation, QaRuntimeService runtime)
        {
            this.navigation = navigation;
            this.runtime = runtime;
            view = new TestsMenuView(runtime, this);
        }
        public void Enter() => runtime.EnterTests();
        public void Leave() => runtime.LeaveTests();
        public void Back() => navigation.Back();
        public void SetDeviceState(string state) => runtime.SetDeviceState(state);
        public void RegenerateQr() => runtime.RegenerateQr();
        public void SelectPlayer(int player) => runtime.SelectPlayer(player);
        public void StartTest() => runtime.StartTest();
        public void StopTest() => runtime.StopTest();
        public void ResetMetrics() => runtime.ResetMetrics();
        public void ExportSummary() => runtime.ExportSummary();
        public void Draw(float width, float height)
        {
            scroll = GUI.BeginScrollView(new Rect(0,0,width,height),scroll,new Rect(0,0,1280,940));
            view.Draw();
            GUI.EndScrollView();
        }
    }
}
