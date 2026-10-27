import 'dart:async';
import 'dart:convert';
import 'dart:io';

import 'package:flutter/foundation.dart';
import 'package:flutter_webrtc/flutter_webrtc.dart';

/// Preserve zero: the plugin's default toMap omits maxRetransmits == 0.
class MotionChannelInit extends RTCDataChannelInit {
  MotionChannelInit() {
    ordered = false;
    maxRetransmits = 0;
  }
  @override
  Map<String, dynamic> toMap() => {...super.toMap(), 'maxRetransmits': 0};
}

class ControlFailure implements Exception {
  final String code;
  const ControlFailure(this.code);
  @override
  String toString() => code;
}

/// Bounded JSON signaling with an explicit byte length (the server rejects
/// chunked requests), and no redirects that could replay an admission.
Future<Map<String, dynamic>> postControllerJson(
  HttpClient client,
  Uri endpoint,
  Map<String, dynamic> body,
) async {
  final request = await client
      .postUrl(endpoint)
      .timeout(const Duration(seconds: 6));
  request.followRedirects = false;
  request.headers.contentType = ContentType.json;
  request.headers.set(HttpHeaders.cacheControlHeader, 'no-store');
  final encoded = utf8.encode(jsonEncode(body));
  request.contentLength = encoded.length;
  request.add(encoded);
  final response = await request.close().timeout(const Duration(seconds: 15));
  final bytes = <int>[];
  await for (final chunk in response.timeout(const Duration(seconds: 5))) {
    if (bytes.length + chunk.length > 73728) {
      throw const ControlFailure('RESPONSE_BOUND');
    }
    bytes.addAll(chunk);
  }
  if (response.statusCode != 200) {
    throw ControlFailure(
      response.statusCode == 400
          ? 'QR_REJECTED'
          : 'HTTP_${response.statusCode}',
    );
  }
  final decoded = jsonDecode(utf8.decode(bytes));
  if (decoded is! Map<String, dynamic>) {
    throw const ControlFailure('RESPONSE_INVALID');
  }
  return decoded;
}

class NativeTransport extends ChangeNotifier {
  RTCPeerConnection? _pc;
  RTCDataChannel? _control, _motion;
  Map<String, dynamic>? _identity;
  Map<String, dynamic>? _context;
  Uri? _origin;
  final HttpClient _http = HttpClient()
    ..connectionTimeout = const Duration(seconds: 5);
  final Stopwatch clock = Stopwatch()..start();
  Timer? _heartbeat;
  int _generation = 0, _sequence = 0, _contextAt = -10000;
  int sent = 0, motionAcks = 0, errors = 0, lastAckMs = -10000;
  final Map<int, Completer<void>> _pending = {};
  final Set<int> _motionPending = {};
  String state = 'idle', stage = 'idle', code = '';
  bool _disposed = false, profileAttempted = false, _motionSending = false;
  bool get canResume => _identity != null;
  int? get playerId => _identity?['playerId'];
  Map<String, dynamic>? get gameContext =>
      clock.elapsedMilliseconds - _contextAt < 2500 ? _context : null;
  void changed() {
    if (!_disposed) notifyListeners();
  }

  void check(int generation) {
    if (_disposed || generation != _generation) {
      throw const ControlFailure('CANCELLED');
    }
  }

  Future<Map<String, dynamic>> request(
    String path,
    Map<String, dynamic> body,
  ) => postControllerJson(_http, _origin!.resolve(path), body);

  Future<void> connect(Uri link, {bool resume = false}) async {
    final prior = resume ? _identity : null;
    final generation = ++_generation;
    await _release();
    check(generation);
    state = 'connecting';
    stage = 'offer';
    code = '';
    _context = null;
    changed();
    _origin = Uri(
      scheme: link.scheme,
      host: link.host,
      port: link.hasPort ? link.port : null,
      path: '/',
    );
    try {
      final pc = await createPeerConnection({'iceServers': <dynamic>[]});
      if (_disposed || generation != _generation) {
        await pc.close();
        await pc.dispose();
        check(generation);
      }
      _pc = pc;
      final gathered = Completer<void>();
      pc.onIceGatheringState = (s) {
        if (s == RTCIceGatheringState.RTCIceGatheringStateComplete &&
            !gathered.isCompleted) {
          gathered.complete();
        }
      };
      pc.onConnectionState = (s) {
        if (generation != _generation || _disposed) return;
        if (s == RTCPeerConnectionState.RTCPeerConnectionStateFailed ||
            s == RTCPeerConnectionState.RTCPeerConnectionStateClosed) {
          state = 'disconnected';
          code = 'CONNECTION_LOST';
          changed();
        }
      };
      final controlOpen = Completer<void>(), motionOpen = Completer<void>();
      for (final label in ['control', 'motion']) {
        final channel = await pc.createDataChannel(
          label,
          label == 'motion' ? MotionChannelInit() : RTCDataChannelInit(),
        );
        check(generation);
        if (label == 'control') {
          _control = channel;
        } else {
          _motion = channel;
        }
        final opened = label == 'control' ? controlOpen : motionOpen;
        void observe(RTCDataChannelState s) {
          if (generation != _generation || _disposed) return;
          if (s == RTCDataChannelState.RTCDataChannelOpen &&
              !opened.isCompleted) {
            opened.complete();
          }
          if (s == RTCDataChannelState.RTCDataChannelClosed &&
              state == 'connected') {
            state = 'disconnected';
            code = 'CONNECTION_LOST';
            changed();
          }
        }

        channel.onDataChannelState = observe;
        if (channel.state != null) observe(channel.state!);
        channel.onMessage = (message) {
          if (generation == _generation && !_disposed) _receive(message);
        };
      }
      final offer = await pc.createOffer({
        'mandatory': {
          'OfferToReceiveAudio': false,
          'OfferToReceiveVideo': false,
        },
        'optional': [],
      });
      check(generation);
      await pc.setLocalDescription(offer);
      if (pc.iceGatheringState ==
              RTCIceGatheringState.RTCIceGatheringStateComplete &&
          !gathered.isCompleted) {
        gathered.complete();
      }
      await gathered.future.timeout(const Duration(seconds: 10));
      check(generation);
      stage = 'signaling';
      changed();
      final local = await pc.getLocalDescription();
      check(generation);
      final fragment = Uri(query: link.fragment).queryParameters;
      final response = await request(
        prior == null ? '/mobile/join' : '/mobile/reconnect',
        prior == null
            ? {
                'protocolVersion': 1,
                'messageType': 'JOIN',
                'sessionId': fragment['sessionId'],
                'admission': fragment['admission'],
                'offer': local!.sdp,
              }
            : {
                'sessionId': prior['sessionId'],
                'playerId': prior['playerId'],
                'resumeToken': prior['resumeToken'],
                'offer': local!.sdp,
              },
      );
      check(generation);
      if (response['messageType'] != 'JOIN_ACK' ||
          response['protocolVersion'] != 1 ||
          response['playerId'] is! int ||
          response['playerId'] < 1 ||
          response['playerId'] > 4 ||
          response['answer'] is! String ||
          response['sessionId'] is! String ||
          response['deviceSessionId'] is! String ||
          response['peerId'] is! String ||
          response['resumeToken'] is! String) {
        throw const ControlFailure('RESPONSE_INVALID');
      }
      _identity = response;
      _sequence = 0;
      await pc.setRemoteDescription(
        RTCSessionDescription(response['answer'], 'answer'),
      );
      check(generation);
      stage = 'channels';
      changed();
      await Future.wait([controlOpen.future, motionOpen.future])
          .timeout(const Duration(seconds: 10));
      check(generation);
      state = 'connected';
      stage = 'hello';
      await control('HELLO', {});
      check(generation);
      stage = 'ready';
      lastAckMs = clock.elapsedMilliseconds;
      changed();
      _heartbeat = Timer.periodic(const Duration(milliseconds: 200), (_) async {
        if (state != 'connected') return;
        try {
          await control('HEARTBEAT', {});
        } catch (_) {
          if (generation == _generation &&
              clock.elapsedMilliseconds - lastAckMs > 3000) {
            state = 'disconnected';
            code = 'CONNECTION_LOST';
            changed();
          }
        }
      });
    } catch (error) {
      if (generation != _generation || _disposed) return;
      code = error is ControlFailure
          ? error.code
          : error is HandshakeException
          ? 'TLS_UNTRUSTED'
          : error is TimeoutException
          ? 'CONNECTION_TIMEOUT'
          : 'CONNECTION_FAILED';
      state = 'error';
      await _release();
      changed();
    }
  }

  Map<String, dynamic> envelope(
    String type,
    Map<String, dynamic> payload, {
    String quality = 'available',
  }) => {
    'protocolVersion': 1,
    'messageType': type,
    'sessionId': _identity!['sessionId'],
    'playerId': playerId,
    'deviceSessionId': _identity!['deviceSessionId'],
    'sequence': ++_sequence,
    'clientTimestamp': DateTime.now().millisecondsSinceEpoch,
    'serverReceiveTimestamp': null,
    'capabilities': [
      'acceleration',
      'accelerationIncludingGravity',
      'rotationRate',
      'orientation',
      'screenOrientation',
    ],
    'quality': {
      'source': kDebugMode && const bool.fromEnvironment('GORILLA_EMULATOR_LAB')
          ? 'emulator'
          : 'physical',
      'status': quality,
    },
    'payload': payload,
  };
  Future<void> control(String type, Map<String, dynamic> payload) async {
    if (state != 'connected' ||
        _control?.state != RTCDataChannelState.RTCDataChannelOpen ||
        (_control?.bufferedAmount ?? 0) > 8192 ||
        _pending.length >= 8) {
      throw const ControlFailure('CONTROL_NOT_READY');
    }
    final frame = envelope(type, payload), ack = Completer<void>();
    final sequence = frame['sequence'] as int;
    _pending[sequence] = ack;
    try {
      await _control!.send(RTCDataChannelMessage(jsonEncode(frame)));
      await ack.future.timeout(const Duration(seconds: 2));
    } finally {
      _pending.remove(sequence);
    }
  }

  Future<void> motion(Map<String, dynamic> payload) async {
    if (_motionSending ||
        state != 'connected' ||
        _motion?.state != RTCDataChannelState.RTCDataChannelOpen ||
        (_motion?.bufferedAmount ?? 0) > 8192) {
      return;
    }
    final quality =
        [
          'acceleration',
          'accelerationIncludingGravity',
          'rotationRate',
          'orientation',
        ].every((k) => payload[k]['availability'] == 'present')
        ? 'available'
        : 'degraded';
    final frame = envelope('MOTION_SAMPLE', payload, quality: quality);
    final text = jsonEncode(frame);
    if (utf8.encode(text).length > 2048) return;
    if (_motionPending.length >= 64) {
      _motionPending.remove(_motionPending.first);
    }
    _motionPending.add(frame['sequence']);
    final generation = _generation;
    _motionSending = true;
    try {
      await _motion!.send(RTCDataChannelMessage(text));
      sent++;
    } catch (_) {
      errors++;
    } finally {
      if (generation == _generation) _motionSending = false;
    }
  }

  void _receive(RTCDataChannelMessage message) {
    if (message.isBinary || message.text.length > 4096) return;
    try {
      final data = jsonDecode(message.text);
      if (data['messageType'] == 'ERROR') {
        errors++;
        return;
      }
      if (data['protocolVersion'] != 1 ||
          data['messageType'] != 'ACK' ||
          data['sessionId'] != _identity?['sessionId'] ||
          data['playerId'] != playerId ||
          data['deviceSessionId'] != _identity?['deviceSessionId']) {
        return;
      }
      lastAckMs = clock.elapsedMilliseconds;
      if (_motionPending.remove(data['sequence'])) {
        motionAcks++;
        return;
      }
      final pending = _pending[data['sequence']];
      if (pending == null || pending.isCompleted) return;
      if (data['payload'] is Map && data['payload']['gameContext'] is Map) {
        _context = Map<String, dynamic>.from(data['payload']['gameContext']);
        _contextAt = clock.elapsedMilliseconds;
      } else if (data['payload'] is Map && (data['payload'] as Map).isEmpty) {
        /* HELLO or empty heartbeat: freshness expires. */
      }
      pending.complete();
      changed();
    } catch (_) {
      errors++;
    }
  }

  Future<String> saveProfile(String displayName) async {
    if (displayName.trim().isEmpty ||
        displayName.trim().length > 40 ||
        !RegExp(
          r"^[\p{L}\p{M}\p{N} _.'-]+$",
          unicode: true,
        ).hasMatch(displayName.trim())) {
      return 'INVALID';
    }
    if (profileAttempted || state != 'connected') {
      return 'UNCONFIRMED';
    }
    profileAttempted = true;
    try {
      final result = await request('/mobile/profile', {
        'peerId': _identity!['peerId'],
        'resumeToken': _identity!['resumeToken'],
        'displayName': displayName.trim(),
      });
      return result['id'] is String ? 'SAVED' : 'UNCONFIRMED';
    } on ControlFailure catch (error) {
      if (error.code == 'HTTP_503') {
        profileAttempted = false;
        return 'UNAVAILABLE';
      }
      return 'UNCONFIRMED';
    } catch (_) {
      return 'UNCONFIRMED';
    }
  }

  Map<String, dynamic> diagnostic() => {
    'platform': 'flutter-native',
    'state': state,
    'stage': stage,
    'code': code,
    'motionSent': sent,
    'motionAck': motionAcks,
    'errors': errors,
  };
  Future<void> _release() async {
    _heartbeat?.cancel();
    _heartbeat = null;
    _motionPending.clear();
    _motionSending = false;
    // Existing control calls retain their bounded timeout; closing must never
    // fabricate a successful acknowledgement.
    _pending.clear();
    final motion = _motion, control = _control, pc = _pc;
    _motion = null;
    _control = null;
    _pc = null;
    if (pc != null) {
      pc.onConnectionState = null;
      pc.onIceGatheringState = null;
    }
    for (final channel in [motion, control]) {
      if (channel != null) {
        channel.onDataChannelState = null;
        channel.onMessage = null;
      }
      try {
        await channel?.close();
      } catch (_) {}
    }
    try {
      await pc?.close();
    } catch (_) {}
    try {
      await pc?.dispose();
    } catch (_) {}
  }

  Future<void> close() async {
    ++_generation;
    state = 'closed';
    final identity = _identity;
    _identity = null;
    _context = null;
    await _release();
    if (identity != null) {
      try {
        await request('/mobile/disconnect', {
          'peerId': identity['peerId'],
          'resumeToken': identity['resumeToken'],
        }).timeout(const Duration(seconds: 2));
      } catch (_) {}
    }
  }

  @override
  void dispose() {
    _disposed = true;
    unawaited(close().whenComplete(() => _http.close(force: true)));
    super.dispose();
  }
}
