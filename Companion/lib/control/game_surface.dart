import 'package:flutter/material.dart';

import 'protocol.dart';

class GameControlSurface extends StatelessWidget {
  final Map<String, dynamic>? contextData;
  final int? player;
  final bool sensorsReady, busy;
  final double tilt;
  final void Function(String action, String target) onCommand;
  final Widget? scorePanel, recenter;
  const GameControlSurface({
    super.key,
    required this.contextData,
    required this.player,
    required this.sensorsReady,
    required this.tilt,
    required this.onCommand,
    this.busy = false,
    this.scorePanel,
    this.recenter,
  });
  @override
  Widget build(BuildContext context) {
    final profile = GameControlProfile.fromContext(contextData, player),
        view = GameView.fromContext(contextData, player);
    final balance = view.id == 'log-balance',
        cameraOnly =
            profile.valid && profile.screen == 'game' && !profile.phoneRequired;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Text(
          view.title,
          style: const TextStyle(
            fontSize: 36,
            height: 1.1,
            fontWeight: FontWeight.w900,
          ),
        ),
        const SizedBox(height: 20),
        Semantics(
          liveRegion: true,
          child: Text(
            view.heading,
            style: Theme.of(context).textTheme.headlineSmall,
          ),
        ),
        const SizedBox(height: 10),
        Text(
          view.instruction,
          style: const TextStyle(fontSize: 18, height: 1.5),
        ),
        if (profile.valid && view.id == 'lobby') ...[
          const SizedBox(height: 20),
          if (profile.canNavigate)
            for (final game in controllerGames.entries)
              Padding(
                padding: const EdgeInsets.only(bottom: 10),
                child: OutlinedButton(
                  onPressed: busy
                      ? null
                      : () => onCommand('select-game', game.key),
                  child: Padding(
                    padding: const EdgeInsets.symmetric(vertical: 14),
                    child: Column(
                      children: [
                        Text(game.value),
                        Text(
                          game.key == 'log-balance'
                              ? 'Solo celular'
                              : [
                                  'coconut-slice',
                                  'jungle-keeper',
                                ].contains(game.key)
                              ? 'Solo cámara'
                              : 'Celular + cámara',
                          style: const TextStyle(fontSize: 12),
                        ),
                      ],
                    ),
                  ),
                ),
              ),
          const Text('Puedes elegir el próximo juego sin activar sensores.'),
        ],
        if (profile.canNavigate && ['lobby', 'bowling'].contains(view.id)) ...[
          const SizedBox(height: 20),
          const Text('Mano con la que lanzas'),
          SegmentedButton<String>(
            segments: const [
              ButtonSegment(value: 'right', label: Text('Derecha')),
              ButtonSegment(value: 'left', label: Text('Izquierda')),
            ],
            selected: {profile.hand},
            onSelectionChanged: busy
                ? null
                : (value) => onCommand('set-hand', value.single),
          ),
        ],
        if (balance) ...[
          const SizedBox(height: 24),
          const Text('Inclinación de tu teléfono'),
          Semantics(
            label: 'Inclinación del teléfono',
            value: sensorsReady
                ? '${(tilt * 100).round()} %'
                : 'Esperando sensores',
            child: LinearProgressIndicator(
              value: sensorsReady ? (tilt.clamp(-1, 1) + 1) / 2 : .5,
              minHeight: 18,
            ),
          ),
          const Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [Text('Izquierda'), Text('Centro'), Text('Derecha')],
          ),
          if (!sensorsReady)
            const Text(
              'Sostén el teléfono vertical y quieto para preparar el control.',
            ),
        ],
        if (cameraOnly)
          Card(
            child: Padding(
              padding: const EdgeInsets.all(20),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    'Juega con las manos libres',
                    style: Theme.of(context).textTheme.titleLarge,
                  ),
                  const SizedBox(height: 8),
                  const Text(
                    'Puedes bloquear el celular y dejarlo a un lado. Esta prueba usa la cámara de la PC. Al desbloquear recuperaremos tu sesión automáticamente.',
                  ),
                ],
              ),
            ),
          ),
        ?scorePanel,
        if (sensorsReady &&
            ['bowling', 'baseball'].contains(view.id) &&
            recenter != null)
          recenter!,
        if (profile.canNavigate &&
            view.phase == 'Ready' &&
            [
              'log-balance',
              'coconut-slice',
              'jungle-keeper',
              'whack',
            ].contains(view.id))
          FilledButton(
            onPressed: busy || (profile.phoneRequired && !sensorsReady)
                ? null
                : () => onCommand('start', ''),
            child: const Text('COMENZAR PRUEBA'),
          ),
        if (profile.canNavigate && profile.screen == 'game') ...[
          const SizedBox(height: 24),
          OutlinedButton(
            onPressed: busy ? null : () => onCommand('menu', ''),
            child: const Text('VOLVER AL MENÚ'),
          ),
          OutlinedButton(
            onPressed: busy ? null : () => onCommand('restart', ''),
            child: const Text('NUEVA PARTIDA'),
          ),
        ],
      ],
    );
  }
}
