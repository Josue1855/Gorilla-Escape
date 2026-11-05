using System;
using System.Collections.Generic;
using GorillaEscape.Contracts;
namespace GorillaEscape.Gameplay.Input
{
    // Preparation belongs to the hosted room, not to a tournament. Live gameplay
    // still gates every action on fresh input after a mode switch or interruption.
    public sealed class SessionPreparation
    {
        string sessionId;
        bool cameraPrepared;
        readonly Dictionary<int,string> devices = new Dictionary<int,string>();
        public void Record(bool cameraReady, LobbySnapshotWire lobby)
        {
            if (string.IsNullOrEmpty(lobby?.sessionId)) return;
            if (sessionId != lobby.sessionId) { Clear(); sessionId = lobby.sessionId; }
            cameraPrepared |= cameraReady;
            foreach (var player in lobby.players ?? Array.Empty<LobbyPlayerWire>())
                if (player != null && player.networkReady && player.inputReady && !string.IsNullOrWhiteSpace(player.displayName)
                    && !string.IsNullOrEmpty(player.deviceSessionId) && (player.connectionState == "ACTIVE" || player.connectionState == "CONNECTED"))
                    devices[player.playerId] = player.deviceSessionId;
        }
        public bool CanReuse(LobbySnapshotWire lobby, bool cameraActive, bool bothDevices, int players)
        {
            if (string.IsNullOrEmpty(sessionId) || lobby?.sessionId != sessionId || players < 1 || players > 4) return false;
            bool camera = cameraPrepared && cameraActive;
            if (bothDevices && !camera) return false;
            if (!bothDevices && camera) return true;
            foreach (int id in System.Linq.Enumerable.Range(1, players))
            {
                var current = Array.Find(lobby.players ?? Array.Empty<LobbyPlayerWire>(), p => p != null && p.playerId == id);
                if (current == null || current.connectionState == "FORFEITED" || !devices.TryGetValue(id, out var device) || current.deviceSessionId != device) return false;
            }
            return true;
        }
        public void Clear() { sessionId = null; cameraPrepared = false; devices.Clear(); }
    }
}
