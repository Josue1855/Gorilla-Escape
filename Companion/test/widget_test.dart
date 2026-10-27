import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:gorilla_companion/main.dart';
import 'package:gorilla_companion/control/page.dart';
import 'package:gorilla_companion/control/protocol.dart';

void main() {
  test('only complete HTTPS admission links can open', () {
    expect(
      controllerLink(
        'https://192.168.1.2:8443/#v=1&sessionId=00000000-0000-0000-0000-000000000001&admission=aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa',
      ),
      isNotNull,
    );
    for (final url in [
      'http://example.com/#sessionId=x&admission=y',
      'https://example.com/',
      'https://u:p@example.com/#sessionId=x&admission=y',
      'https://example.com/?token=x#sessionId=x&admission=y',
    ]) {
      expect(controllerLink(url), isNull);
    }
  });
  testWidgets(
    'expedition home remains reachable at narrow width and large text',
    (tester) async {
      await tester.binding.setSurfaceSize(const Size(320, 800));
      addTearDown(() => tester.binding.setSurfaceSize(null));
      await tester.pumpWidget(
        MaterialApp(
          home: MediaQuery(
            data: const MediaQueryData(textScaler: TextScaler.linear(2)),
            child: const IslandHome(),
          ),
        ),
      );
      await tester.pumpAndSettle();
      expect(tester.takeException(), isNull);
      await tester.scrollUntilVisible(
        find.text('ESCANEAR QR DE LA PC'),
        250,
        scrollable: find.byType(Scrollable).first,
      );
      expect(find.text('ESCANEAR QR DE LA PC').hitTestable(), findsOneWidget);
    },
  );
  testWidgets(
    'real game score remains readable on narrow screen with large text',
    (tester) async {
      final stats = GameStats.fromContext({
        'gameId': 'log-balance',
        'stats': {
          'balance': .2,
          'tendency': .3,
          'progress': 1,
          'distance': 18,
          'elapsed': 22.7,
          'roll': 10,
          'speed': .88,
          'wind': .2,
        },
      })!;
      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            body: MediaQuery(
              data: const MediaQueryData(textScaler: TextScaler.linear(2)),
              child: Center(
                child: SizedBox(
                  width: 320,
                  child: SingleChildScrollView(
                    child: GameScorePanel(stats: stats),
                  ),
                ),
              ),
            ),
          ),
        ),
      );
      expect(find.text('Recorrido 100 %'), findsOneWidget);
      expect(find.text('18.0 m · 22.7 s'), findsNothing);
      await tester.ensureVisible(find.text('Detalles del cruce'));
      await tester.tap(find.text('Detalles del cruce'));
      await tester.pumpAndSettle();
      expect(find.text('18.0 m · 22.7 s'), findsOneWidget);
      expect(tester.takeException(), isNull);
    },
  );
  testWidgets('home explains setup, scanner and safe physical control', (
    tester,
  ) async {
    await tester.pumpWidget(const GorillaCompanion());
    expect(find.text('TU SALIDA\nDE LA ISLA'), findsOneWidget);
    expect(find.text('ESCANEAR QR DE LA PC'), findsOneWidget);
    expect(find.byType(TextField), findsNothing);
  });
  testWidgets('home allows expanding room code entry', (tester) async {
    await tester.pumpWidget(const GorillaCompanion());
    await tester.scrollUntilVisible(
      find.text('Ingresar código de sala'),
      200,
      scrollable: find.byType(Scrollable).first,
    );
    await tester.drag(find.byType(Scrollable).first, const Offset(0, -150));
    await tester.pumpAndSettle();
    expect(find.text('Ingresar código de sala'), findsOneWidget);
    await tester.tap(find.text('Ingresar código de sala'));
    await tester.pumpAndSettle();
    expect(find.text('Código de 4 caracteres (ej. K7M4)'), findsOneWidget);
    expect(find.textContaining('Dirección o IP'), findsNothing);
    expect(find.text('CONECTAR CON CÓDIGO'), findsOneWidget);
  });
}
