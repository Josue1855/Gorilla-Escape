import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:gorilla_companion/control/protocol.dart';
import 'package:gorilla_companion/control/game_surface.dart';

Map<String, dynamic> contextFor(String id) => {
  'version': 3,
  'gameId': id,
  'screen': id == 'lobby' ? 'menu' : 'game',
  'playerId': id == 'lobby' ? 0 : 1,
  'phase': 'Ready',
  'phoneRequired': !['lobby', 'coconut-slice', 'jungle-keeper'].contains(id),
  'hand': 'left',
  'contextId': 'a' * 32,
};
void main() {
  test(
    'menu, one-hand Bowling and camera-only games have distinct profiles',
    () {
      for (final id in ['lobby', ...controllerGames.keys]) {
        final c = contextFor(id),
            profile = GameControlProfile.fromContext(c, 1);
        expect(profile.valid, isTrue);
        expect(profile.hand, 'left');
        expect(profile.canNavigate, isTrue);
        expect(GameControlProfile.fromContext(c, 2).canNavigate, isFalse);
        expect(
          profile.phoneRequired,
          !['lobby', 'coconut-slice', 'jungle-keeper'].contains(id),
        );
        expect(GameView.fromContext(c, 1).id, id);
      }
      expect(
        GameView.fromContext(contextFor('bowling'), 1).instruction,
        contains('izquierda'),
      );
      expect(
        GameControlProfile.fromContext({
          ...contextFor('lobby'),
          'contextId': 'bad',
        }, 1).canNavigate,
        isFalse,
      );
    },
  );
  test('canonical room links preserve the route for native reconnect', () {
    expect(
      controllerLink(
        'https://play.gorillas.run/abcd/#v=1&sessionId=00000000-0000-0000-0000-000000000001&admission=${'a' * 43}',
      ),
      isNotNull,
    );
  });
  testWidgets('menu works without sensors and sends the selected game', (
    tester,
  ) async {
    final commands = <String>[];
    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: SingleChildScrollView(
            child: GameControlSurface(
              contextData: contextFor('lobby'),
              player: 1,
              sensorsReady: false,
              tilt: 0,
              onCommand: (action, target) => commands.add('$action:$target'),
            ),
          ),
        ),
      ),
    );
    expect(find.text('BANANA BOWLING'), findsOneWidget);
    await tester.tap(find.text('BANANA BOWLING'));
    expect(commands, ['select-game:bowling']);
    expect(find.text('Inclinación de tu teléfono'), findsNothing);
    expect(tester.takeException(), isNull);
  });
  testWidgets('Slice permits screen lock and omits tilt controls', (
    tester,
  ) async {
    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: SingleChildScrollView(
            child: GameControlSurface(
              contextData: contextFor('coconut-slice'),
              player: 1,
              sensorsReady: false,
              tilt: 0,
              onCommand: (_, _) {},
            ),
          ),
        ),
      ),
    );
    expect(find.textContaining('Puedes bloquear el celular'), findsOneWidget);
    expect(find.text('Inclinación de tu teléfono'), findsNothing);
    expect(find.text('VOLVER AL MENÚ'), findsOneWidget);
    expect(tester.takeException(), isNull);
  });
  testWidgets(
    'equilibrium has its own tilt bar without camera-only instructions',
    (tester) async {
      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            body: SingleChildScrollView(
              child: GameControlSurface(
                contextData: contextFor('log-balance'),
                player: 1,
                sensorsReady: true,
                tilt: -.4,
                onCommand: (_, _) {},
              ),
            ),
          ),
        ),
      );
      expect(find.text('Inclinación de tu teléfono'), findsOneWidget);
      expect(find.textContaining('Puedes bloquear el celular'), findsNothing);
      final bar = tester.widget<LinearProgressIndicator>(
        find.byType(LinearProgressIndicator),
      );
      expect(bar.value, closeTo(.3, .001));
      expect(tester.takeException(), isNull);
    },
  );
}
