import 'dart:math' as math;
import 'dart:io';
import 'dart:convert';

import 'package:flutter_rotation_sensor/flutter_rotation_sensor.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:gorilla_companion/control/protocol.dart';
import 'package:gorilla_companion/control/transport.dart';

void main() {
  test('score rejects incomplete or impossible Unity data', () {
    expect(GameStats.fromContext(null), isNull);
    expect(
      GameStats.fromContext({
        'gameId': 'log-balance',
        'stats': {'progress': 1},
      }),
      isNull,
    );
  });
  test(
    'native signaling sends a bounded length and never follows redirects',
    () async {
      final server = await HttpServer.bind(InternetAddress.loopbackIPv4, 0);
      final client = HttpClient();
      var calls = 0;
      final body = {'displayName': 'Náufrago'};
      final subscription = server.listen((request) async {
        calls++;
        expect(request.contentLength, utf8.encode(jsonEncode(body)).length);
        expect(
          request.headers.value(HttpHeaders.transferEncodingHeader),
          isNull,
        );
        expect(jsonDecode(await utf8.decoder.bind(request).join()), body);
        request.response.statusCode = 302;
        request.response.headers.set(HttpHeaders.locationHeader, '/other');
        await request.response.close();
      });
      try {
        await expectLater(
          postControllerJson(
            client,
            Uri.parse('http://127.0.0.1:${server.port}/mobile/join'),
            body,
          ),
          throwsA(
            isA<ControlFailure>().having((e) => e.code, 'code', 'HTTP_302'),
          ),
        );
        expect(calls, 1);
      } finally {
        client.close(force: true);
        await subscription.cancel();
        await server.close(force: true);
      }
    },
  );
  test('loopback lab admission stays explicit', () {
    final value = controllerLink(
      'http://127.0.0.1:12345/#v=1&sessionId=00000000-0000-0000-0000-000000000001&admission=aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa',
    );
    expect(value != null, const bool.fromEnvironment('GORILLA_EMULATOR_LAB'));
  });
  test(
    'native rotation reconstructs W3C intrinsic Z-X-Y across hemispheres',
    () {
      for (final a in [0, 45, 270]) {
        for (final b in [-170, -80, 0, 80, 170]) {
          for (final g in [-70, 0, 70]) {
            final r =
                Matrix3.rotateZ(a * math.pi / 180) *
                Matrix3.rotateX(b * math.pi / 180) *
                Matrix3.rotateY(g * math.pi / 180);
            final o = deviceOrientation(r);
            expect(o['beta'], closeTo(b, 0.0001));
            expect(o['gamma'], closeTo(g, 0.0001));
            expect(
              (o['alpha']! - a + 360) % 360,
              anyOf(closeTo(0, 0.0001), closeTo(360, 0.0001)),
            );
          }
        }
      }
    },
  );
  test('stale and pre-resume samples cannot drive Unity', () {
    final store = MotionStore();
    for (var n = 0; n < 3; n++) {
      store.record('orientation', {'alpha': 0, 'beta': 0, 'gamma': 5}, n * 20);
    }
    expect(store.ready(50), isTrue);
    expect(store.ready(390), isFalse);
    expect(store.payload(390)['orientation']['availability'], 'unavailable');
    store.clear();
    expect(store.ready(400), isFalse);
    store.record('orientation', {'gamma': double.nan}, 401);
    expect(store.gamma(402), isNull);
  });
  test('unreliable motion channel preserves zero retransmissions', () {
    expect(MotionChannelInit().toMap()['maxRetransmits'], 0);
    expect(MotionChannelInit().toMap()['ordered'], false);
  });
  test(
    'unknown game context and other players never impersonate active game',
    () {
      expect(
        GameView.fromContext({'version': 1, 'gameId': 'unknown'}, 1).id,
        'lobby',
      );
      final view = GameView.fromContext({
        'version': 1,
        'gameId': 'log-balance',
        'phase': 'Crossing',
        'playerId': 2,
      }, 1);
      expect(view.myTurn, isFalse);
      expect(view.heading, 'Espera tu turno');
    },
  );
}
