import 'dart:convert';
import 'dart:io';

import 'protocol.dart';

String? roomCode(String value) {
  final code = value.trim().toUpperCase();
  return RegExp(r'^[2-9A-HJKMNP-Z]{4}$').hasMatch(code) ? code : null;
}

Uri? roomOrigin(String value) {
  final uri = Uri.tryParse(value);
  return uri != null &&
          uri.scheme == 'https' &&
          uri.host.isNotEmpty &&
          uri.userInfo.isEmpty &&
          uri.query.isEmpty &&
          uri.fragment.isEmpty &&
          (uri.path.isEmpty || uri.path == '/')
      ? uri.replace(path: '/', query: null, fragment: null)
      : null;
}

/// Resolves a code without trusting arbitrary certificates or following redirects.
class RoomCodeClient {
  final HttpClient http;
  RoomCodeClient({HttpClient? client})
    : http =
          client ??
          (HttpClient()..connectionTimeout = const Duration(seconds: 4));
  Future<Map<String, dynamic>> post(Uri uri, Map<String, dynamic> body) async {
    final request = await http.postUrl(uri).timeout(const Duration(seconds: 5));
    request.followRedirects = false;
    final bytes = utf8.encode(jsonEncode(body));
    request.headers.contentType = ContentType.json;
    request.headers.set('Cache-Control', 'no-store');
    request.contentLength = bytes.length;
    request.add(bytes);
    final response = await request.close().timeout(const Duration(seconds: 6));
    final parts = <int>[];
    await for (final part in response.timeout(const Duration(seconds: 6))) {
      parts.addAll(part);
      if (parts.length > 4096) throw const FormatException('ROOM_REPLY_BOUND');
    }
    if (response.statusCode != 200) {
      throw const FormatException('ROOM_NOT_AVAILABLE');
    }
    final data = jsonDecode(utf8.decode(parts));
    if (data is! Map<String, dynamic>) {
      throw const FormatException('ROOM_REPLY_INVALID');
    }
    return data;
  }

  Future<Uri> join(String input, {Uri? recentOrigin}) async {
    final pin = roomCode(input);
    if (pin == null) throw const FormatException('PIN_INVALID');
    Uri? target = recentOrigin;
    if (target == null) throw const FormatException('PC_NOT_LINKED');
    final data = await post(target.resolve('/mobile/pin-admission'), {
      'pin': pin,
    });
    final uri = target.replace(
      path: '/',
      fragment: Uri(
        queryParameters: {
          'v': '1',
          'sessionId': '${data['sessionId']}',
          'admission': '${data['admission']}',
        },
      ).query,
    );
    final checked = controllerLink(uri.toString());
    if (checked == null) throw const FormatException('ROOM_REPLY_INVALID');
    return checked;
  }

  void close() => http.close(force: true);
}
