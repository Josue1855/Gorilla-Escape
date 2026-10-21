import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:gorilla_companion/main.dart';

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
  testWidgets('home explains setup, scanner and safe physical control', (
    tester,
  ) async {
    await tester.pumpWidget(const GorillaCompanion());
    expect(find.text('TU SALIDA\nDE LA ISLA'), findsOneWidget);
    expect(find.text('ESCANEAR QR DE LA PC'), findsOneWidget);
    expect(find.byType(TextField), findsNothing);
  });
}
