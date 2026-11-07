import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:gorilla_companion/main.dart';
import 'package:gorilla_companion/control/page.dart';
import 'package:gorilla_companion/control/game_surface.dart';
import 'package:gorilla_companion/control/protocol.dart';
import 'package:gorilla_companion/control/transport.dart';

Map<String, dynamic> snapshot(
  String id, {
  String phase = 'Playing',
  String? screen,
}) => {
  'version': 3,
  'gameId': id,
  'phase': phase,
  'playerId': id == 'lobby' ? 0 : 1,
  'screen': screen ?? (id == 'lobby' ? 'menu' : 'game'),
  'contextId': 'a' * 32,
  'hand': 'right',
  'phoneRequired':
      !['lobby', 'coconut-slice', 'jungle-keeper'].contains(id) ||
      screen == 'connection',
};

class UiTransport extends NativeTransport {
  final Completer<String> nameReceipt = Completer<String>();
  String? submittedName;
  @override
  int? get playerId => 1;
  @override
  Map<String, dynamic>? get gameContext =>
      snapshot('lobby', screen: 'connection');
  @override
  Future<void> connect(Uri link, {bool resume = false}) async {
    state = 'connected';
    notifyListeners();
  }

  @override
  Future<String> setPlayerName(String value) {
    submittedName = value;
    return nameReceipt.future;
  }
}

class DisconnectedUiTransport extends UiTransport {
  @override
  Future<void> connect(Uri link, {bool resume = false}) async {
    state = 'disconnected';
    notifyListeners();
  }
}

void main() {
  testWidgets('disconnected control never claims it is ready', (tester) async {
    await tester.pumpWidget(
      MaterialApp(
        home: ControlPage(
          link: Uri.parse('https://example.invalid'),
          transport: DisconnectedUiTransport(),
        ),
      ),
    );
    await tester.pump();
    expect(find.text('RECUPERA TU CONEXIÓN'), findsOneWidget);
    expect(find.text('Control preparado'), findsNothing);
    await tester.pumpWidget(const SizedBox());
  });
  test('connection profile prepares sensors but cannot bypass the PC gate', () {
    final context = snapshot('lobby', screen: 'connection');
    final profile = GameControlProfile.fromContext(context, 1);
    expect(profile.phoneRequired, isTrue);
    expect(profile.canNavigate, isFalse);
    expect(GameView.fromContext(context, 1).instruction, contains('sensores'));
  });
  testWidgets('native name stays editable until the PC confirms it', (
    tester,
  ) async {
    final transport = UiTransport();
    await tester.pumpWidget(
      MaterialApp(
        home: ControlPage(
          link: Uri.parse('https://example.test/'),
          transport: transport,
        ),
      ),
    );
    await tester.pump();
    await tester.pump();
    final input = find.byType(TextField).first;
    await tester.enterText(input, 'Josué');
    expect(transport.submittedName, isNull);
    await tester.tap(find.text('GUARDAR NOMBRE'));
    await tester.pump();
    expect(transport.submittedName, 'Josué');
    expect(find.byType(TextField), findsOneWidget);
    transport.nameReceipt.complete('SAVED');
    await tester.pump();
    await tester.pump();
    expect(find.text('Nombre confirmado en la PC.'), findsOneWidget);
    expect(find.text('Josué'), findsOneWidget);
    expect(find.text('EDITAR NOMBRE'), findsOneWidget);
    expect(tester.takeException(), isNull);
    await tester.pumpWidget(const SizedBox());
    await tester.pump();
  });
  testWidgets('back from a connected control asks before leaving the room', (
    tester,
  ) async {
    final transport = UiTransport();
    await tester.pumpWidget(
      MaterialApp(
        home: Builder(
          builder: (context) => Scaffold(
            body: TextButton(
              onPressed: () => Navigator.of(context).push(
                MaterialPageRoute<void>(
                  builder: (_) => ControlPage(
                    link: Uri.parse('https://example.test/'),
                    transport: transport,
                  ),
                ),
              ),
              child: const Text('OPEN CONTROL'),
            ),
          ),
        ),
      ),
    );
    await tester.tap(find.text('OPEN CONTROL'));
    await tester.pumpAndSettle();
    await tester.binding.handlePopRoute();
    await tester.pumpAndSettle();
    expect(find.text('¿Salir de la partida?'), findsOneWidget);
    await tester.tap(find.text('CANCELAR'));
    await tester.pumpAndSettle();
    expect(find.byType(ControlPage), findsOneWidget);
    expect(tester.takeException(), isNull);
    await tester.pumpWidget(const SizedBox());
    await tester.pump();
  });
  testWidgets(
    'all ten games and phases fit compact and landscape screens with enlarged text',
    (tester) async {
      tester.view.devicePixelRatio = 1;
      addTearDown(tester.view.resetPhysicalSize);
      addTearDown(tester.view.resetDevicePixelRatio);
      for (final size in [const Size(320, 568), const Size(844, 390)]) {
        tester.view.physicalSize = size;
        for (final id in controllerGames.keys) {
          for (final phase in ['Ready', 'Playing', 'Paused', 'Finished']) {
            await tester.pumpWidget(
              MaterialApp(
                home: Builder(
                  builder: (context) => MediaQuery(
                    data: MediaQuery.of(context)
                        .copyWith(textScaler: TextScaler.linear(1.8)),
                    child: Scaffold(
                      body: ListView(
                        padding: const EdgeInsets.all(16),
                        children: [
                          GameControlSurface(
                            contextData: snapshot(id, phase: phase),
                            player: 1,
                            sensorsReady: false,
                            tilt: 0,
                            onCommand: (_, _) {},
                          ),
                        ],
                      ),
                    ),
                  ),
                ),
              ),
            );
            await tester.pump();
            expect(tester.takeException(), isNull, reason: '$id $phase $size');
          }
        }
      }
    },
  );
  testWidgets('spectators see the pause status without authority buttons', (
    tester,
  ) async {
    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: SingleChildScrollView(
            child: GameControlSurface(
              contextData: snapshot('drums', phase: 'Paused'),
              player: 2,
              sensorsReady: false,
              tilt: 0,
              onCommand: (_, _) {},
            ),
          ),
        ),
      ),
    );
    expect(find.text('JUEGO EN PAUSA'), findsOneWidget);
    expect(find.text('REANUDAR'), findsNothing);
    expect(find.text('VOLVER AL MENÚ'), findsNothing);
  });
  testWidgets(
    'home branding remains readable on compact screens with enlarged text',
    (tester) async {
      tester.view.physicalSize = const Size(320, 568);
      tester.view.devicePixelRatio = 1;
      addTearDown(tester.view.resetPhysicalSize);
      addTearDown(tester.view.resetDevicePixelRatio);
      await tester.pumpWidget(
        MaterialApp(
          home: Builder(
            builder: (context) => MediaQuery(
              data: MediaQuery.of(context)
                  .copyWith(textScaler: TextScaler.linear(1.8)),
              child: const IslandHome(),
            ),
          ),
        ),
      );
      await tester.pump();
      expect(tester.takeException(), isNull);
      expect(find.text('GORILLA ESCAPE'), findsOneWidget);
    },
  );
}
