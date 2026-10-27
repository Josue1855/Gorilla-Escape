import 'dart:math' as math;

import 'package:flutter/foundation.dart';
import 'package:flutter_rotation_sensor/flutter_rotation_sensor.dart';

Uri? controllerLink(String text) {
  try {
    if (text.length > 1024) return null;
    final uri = Uri.tryParse(text.trim());
    final lab =
        kDebugMode &&
        const bool.fromEnvironment('GORILLA_EMULATOR_LAB') &&
        uri?.scheme == 'http' &&
        uri?.host == '127.0.0.1';
    if (uri == null ||
        (!lab && uri.scheme != 'https') ||
        uri.host.isEmpty ||
        uri.userInfo.isNotEmpty ||
        uri.query.isNotEmpty ||
        uri.path != '/') {
      return null;
    }
    final fields = Uri(query: uri.fragment).queryParametersAll;
    if (fields.length != 3 ||
        fields.values.any((v) => v.length != 1) ||
        fields['v']?.single != '1' ||
        !RegExp(r'^[a-f0-9-]{36}$')
            .hasMatch(fields['sessionId']?.single ?? '') ||
        !RegExp(r'^[A-Za-z0-9_-]{43}$')
            .hasMatch(fields['admission']?.single ?? '')) {
      return null;
    }
    return uri;
  } on FormatException {
    return null;
  }
}

/// Device-to-world rotation, decomposed in the protocol's intrinsic Z-X-Y order.
/// CoreMotion/Android fused rotation is used; gravity alone cannot recover yaw.
Map<String, double> deviceOrientation(Matrix3 r) {
  var beta = math.asin(r.h.clamp(-1.0, 1.0));
  double alpha, gamma;
  if (math.sqrt(r.g * r.g + r.i * r.i) < 1e-6) {
    alpha = math.atan2(r.d, r.a);
    gamma = 0;
  } else {
    alpha = math.atan2(-r.b, r.e);
    gamma = math.atan2(-r.g, r.i);
    if (gamma > math.pi / 2 || gamma < -math.pi / 2) {
      gamma += gamma > 0 ? -math.pi : math.pi;
      beta = beta >= 0 ? math.pi - beta : -math.pi - beta;
      alpha += math.pi;
    }
  }
  const degrees = 180 / math.pi;
  return {
    'alpha': (alpha * degrees) % 360,
    'beta': (beta * degrees).clamp(-180, 180),
    'gamma': (gamma * degrees).clamp(-90, 90),
  };
}

class MotionStore {
  final Map<String, Map<String, num>> _values = {};
  final Map<String, int> _received = {};
  final Map<String, int> _counts = {};
  void clear() {
    _values.clear();
    _received.clear();
    _counts.clear();
  }

  void record(String group, Map<String, num> values, int nowMs) {
    if (values.values.any((v) => !v.isFinite)) return;
    _values[group] = Map.of(values);
    _received[group] = nowMs;
    _counts[group] = (_counts[group] ?? 0) + 1;
  }

  bool fresh(String group, int nowMs) =>
      _received[group] != null &&
      nowMs >= _received[group]! &&
      nowMs - _received[group]! < 350;
  bool ready(int nowMs) =>
      fresh('orientation', nowMs) && (_counts['orientation'] ?? 0) >= 3;
  double? gamma(int nowMs) => fresh('orientation', nowMs)
      ? (_values['orientation']?['gamma'])?.toDouble()
      : null;
  Map<String, dynamic> payload(int nowMs) => {
    for (final name in [
      'acceleration',
      'accelerationIncludingGravity',
      'rotationRate',
      'orientation',
    ])
      name: fresh(name, nowMs)
          ? {'availability': 'present', 'values': _values[name]}
          : {'availability': 'unavailable', 'values': null},
    'screenOrientation': {
      'availability': 'present',
      'values': {'angle': 0},
    },
    'touch': {'availability': 'unavailable', 'values': null},
  };
}

class GameView {
  final String id, phase, title, heading, instruction;
  final bool myTurn;
  const GameView(
    this.id,
    this.phase,
    this.title,
    this.heading,
    this.instruction,
    this.myTurn,
  );
  static const lobby = GameView(
    'lobby',
    'Ready',
    'LA ISLA\nTE ESPERA',
    'Control preparado',
    'Elige una prueba en la PC.',
    false,
  );
  static GameView fromContext(Map<String, dynamic>? context, int? player) {
    const phases = {
      'Ready': [
        'Prepara tu equilibrio',
        'Sujeta el teléfono cómodamente. El control se prepara automáticamente.',
      ],
      'Countdown': [
        'La prueba comienza',
        'Sostén el teléfono vertical y mira la PC.',
      ],
      'Crossing': [
        'Cruza el río',
        'Inclina con suavidad para corregir el equilibrio.',
      ],
      'Paused': [
        'La isla espera',
        'La partida está en pausa. Reanuda desde la PC.',
      ],
      'Won': [
        '¡Prueba superada!',
        'Los gorilas reconocen tu victoria. Continúa en la PC.',
      ],
      'Fell': [
        'El río ganó esta vez',
        'Recupera una posición cómoda e inténtalo de nuevo.',
      ],
    };
    if (context?['version'] != 1 ||
        context?['gameId'] != 'log-balance' ||
        !phases.containsKey(context?['phase']) ||
        context?['playerId'] is! int ||
        context!['playerId'] < 1 ||
        context['playerId'] > 4) {
      return lobby;
    }
    final turn = context['playerId'] == player;
    final copy = phases[context['phase']]!;
    return GameView(
      'log-balance',
      context['phase'],
      'DESAFÍO\nDEL TRONCO',
      turn ? copy[0] : 'Espera tu turno',
      turn ? copy[1] : 'Otro náufrago está jugando. Mira la PC.',
      turn,
    );
  }
}

/// Authoritative Unity display data; never derive progress or balance from local sensors.
class GameStats {
  final Map<String, double> values;
  GameStats._(this.values);
  double operator [](String key) => values[key]!;
  double get danger => values['danger'] ?? 0;
  static GameStats? fromContext(Map<String, dynamic>? context) {
    final stats = context?['stats'];
    if (context?['gameId'] != 'log-balance' || stats is! Map) return null;
    const bounds = {
      'balance': [-1, 1],
      'tendency': [-1, 1],
      'progress': [0, 1],
      'distance': [0, 10000],
      'elapsed': [0, 3600],
      'roll': [-3600, 3600],
      'speed': [0, 100],
      'wind': [-100, 100],
    };
    final values = <String, double>{};
    for (final entry in bounds.entries) {
      final value = stats[entry.key];
      if (value is! num ||
          !value.isFinite ||
          value < entry.value[0] ||
          value > entry.value[1]) {
        return null;
      }
      values[entry.key] = value.toDouble();
    }
    final danger = stats['danger'];
    if (danger != null) {
      if (danger is! num || !danger.isFinite || danger < 0 || danger > 1) {
        return null;
      }
      values['danger'] = danger.toDouble();
    }
    return GameStats._(values);
  }
}

/// Cues are based only on fresh Unity state and are bounded to avoid continuous vibration.
class BalanceCues {
  String previousPhase = '';
  int lastPulse = -10000;
  String? update(String phase, double danger, int now, bool myTurn) {
    final changed = phase != previousPhase;
    previousPhase = phase;
    if (!myTurn) return null;
    if (phase == 'Ready') {
      lastPulse = -10000;
      return null;
    }
    if (changed && (phase == 'Won' || phase == 'Fell')) return phase;
    if (phase != 'Crossing' || danger < .6) return null;
    final interval = danger >= .85 ? 450 : 900;
    if (now - lastPulse < interval) return null;
    lastPulse = now;
    return danger >= .85 ? 'critical' : 'warning';
  }
}
