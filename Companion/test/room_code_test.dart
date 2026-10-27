import 'dart:io';

import 'package:flutter_test/flutter_test.dart';
import 'package:gorilla_companion/control/room_code.dart';

class FakeRooms extends RoomCodeClient {
  final calls = <String>[];
  @override
  Future<Map<String, dynamic>> post(Uri uri, Map<String, dynamic> body) async {
    calls.add(uri.toString());
    return {
      'sessionId': '00000000-0000-0000-0000-000000000001',
      'admission': 'a' * 43,
    };
  }
}

void main() {
  test('unknown PC asks for QR without contacting another service', () async {
    final rooms = FakeRooms();
    try {
      await expectLater(rooms.join('K7M4'), throwsFormatException);
      expect(rooms.calls, isEmpty);
    } finally {
      rooms.close();
    }
  });
  test('four-character codes normalize and reject ambiguous characters', () {
    expect(roomCode(' k7m4 '), 'K7M4');
    for (final value in ['I111', 'OOOO', 'AAAAA', '12', 'L234']) {
      expect(roomCode(value), isNull);
    }
    expect(roomOrigin('http://example.com'), isNull);
    expect(roomOrigin('https://u:p@example.com'), isNull);
  });
  test(
    'local PC admission produces the same native control link as QR',
    () async {
      final rooms = FakeRooms();
      try {
        final uri = await rooms.join(
          'k7m4',
          recentOrigin: Uri.parse('https://room.trycloudflare.com'),
        );
        expect(uri.host, 'room.trycloudflare.com');
        expect(uri.fragment, contains('admission='));
        expect(rooms.calls, [
          'https://room.trycloudflare.com/mobile/pin-admission',
        ]);
      } finally {
        rooms.close();
      }
    },
  );
  test('HTTP posts are bounded and never follow redirects', () async {
    final server = await HttpServer.bind(InternetAddress.loopbackIPv4, 0);
    var calls = 0;
    server.listen((request) async {
      calls++;
      request.response.statusCode = 302;
      request.response.headers.set('Location', '/redirect');
      await request.response.close();
    });
    final rooms = RoomCodeClient();
    try {
      await expectLater(
        rooms.post(Uri.parse('http://127.0.0.1:${server.port}/'), {
          'pin': 'K7M4',
        }),
        throwsFormatException,
      );
      expect(calls, 1);
    } finally {
      rooms.close();
      await server.close(force: true);
    }
  });
}
