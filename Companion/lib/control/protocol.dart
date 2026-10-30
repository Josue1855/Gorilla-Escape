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
        (uri.path != '/' &&
            !(uri.host == 'play.gorillas.run' &&
                RegExp(r'^/[a-z0-9]{4,8}/?$').hasMatch(uri.path)))) {
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
  int revision = 0;
  bool actionPressed = false, recenter = false;
  void setAction(bool pressed, {bool center = false}) {
    actionPressed = pressed;
    recenter = center;
  }

  final Map<String, Map<String, num>> _values = {};
  final Map<String, int> _received = {};
  final Map<String, int> _counts = {};
  void clear() {
    actionPressed = false;
    revision++;
    _values.clear();
    _received.clear();
    _counts.clear();
  }

  void record(String group, Map<String, num> values, int nowMs) {
    if (values.values.any((v) => !v.isFinite)) return;
    revision++;
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
    'touch': {
      'availability': 'present',
      'values': {'x': recenter ? 1 : 0, 'y': 0.5, 'pressed': actionPressed},
    },
  };
}

const controllerGames = <String, String>{
  'bowling': 'BANANA BOWLING',
  'boxing': 'BOXEO CON GORILA',
  'baseball': 'JUNGLE BASEBALL',
  'drums': 'JUNGLE DRUMS',
  'coconut-slice': 'COCONUT SLICE',
  'jungle-keeper': 'PORTERO DE LA JUNGLA',
  'whack': 'GORILLA WHACK',
  'log-balance': 'DESAFÍO DEL TRONCO',
};

class GameControlProfile {
  final String game, screen, hand;
  final String? contextId;
  final bool valid, phoneRequired, canNavigate;
  const GameControlProfile(
    this.game,
    this.screen,
    this.hand,
    this.contextId,
    this.valid,
    this.phoneRequired,
    this.canNavigate,
  );
  static GameControlProfile fromContext(
    Map<String, dynamic>? context,
    int? player,
  ) {
    final id = context?['gameId'],
        screen = context?['screen'],
        hand = context?['hand'],
        token = context?['contextId'];
    final owner = context?['playerId'];
    final valid =
        context?['version'] == 3 &&
        token is String &&
        RegExp(r'^[a-f0-9]{32}$').hasMatch(token) &&
        ['menu', 'connection', 'game'].contains(screen) &&
        ['left', 'right'].contains(hand) &&
        context?['phoneRequired'] is bool &&
        (id == 'lobby'
            ? owner == 0 && screen != 'game'
            : controllerGames.containsKey(id) &&
                  screen == 'game' &&
                  owner is int &&
                  owner >= 1 &&
                  owner <= 4);
    final game = controllerGames.containsKey(id) ? id as String : 'lobby';
    return GameControlProfile(
      game,
      valid ? screen as String : 'unknown',
      valid ? hand as String : 'right',
      valid ? token : null,
      valid,
      valid
          ? context!['phoneRequired'] as bool
          : game != 'lobby' &&
                !['coconut-slice', 'jungle-keeper'].contains(game),
      valid && player == 1,
    );
  }
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
    final profile = GameControlProfile.fromContext(context, player);
    if (profile.valid && profile.game == 'lobby') {
      return GameView(
        'lobby',
        'Ready',
        'JUNGLE PARTY',
        'Elige tu desafío',
        player == 1
            ? 'Elige un juego aquí o en la PC.'
            : 'El jugador 1 elige la próxima prueba.',
        false,
      );
    }
    if (context?['version'] == 2 || context?['version'] == 3) {
      const games = {
        'bowling': ['BANANA BOWLING', 'Apunta y realiza un lanzamiento suave.'],
        'boxing': [
          'BOXEO CON GORILA',
          'Mantén ambas manos visibles y realiza los golpes.',
        ],
        'baseball': ['JUNGLE BASEBALL', 'Espera la pelota y realiza el bateo.'],
        'drums': ['JUNGLE DRUMS', 'Sigue el ritmo con ambas manos.'],
        'coconut-slice': [
          'COCONUT SLICE',
          'Corta con tus manos y evita obstáculos.',
        ],
        'jungle-keeper': [
          'PORTERO DE LA JUNGLA',
          'Detén los tiros con las manos y el torso frente a la cámara.',
        ],
        'whack': ['GORILLA WHACK', 'Apunta y realiza un golpe hacia abajo.'],
        'log-balance': [
          'DESAFÍO DEL TRONCO',
          'Inclina suavemente y frena la inercia.',
        ],
      };
      const phases = {
        'Ready': 'Prepárate',
        'Countdown': 'La prueba comienza',
        'Playing': '¡A jugar!',
        'Paused': 'Juego en pausa',
        'Won': '¡Prueba superada!',
        'Lost': 'Inténtalo de nuevo',
        'Finished': 'Prueba terminada',
      };
      final id = context?['gameId'],
          phase = context?['phase'],
          who = context?['playerId'];
      if (!games.containsKey(id) ||
          !phases.containsKey(phase) ||
          who is! int ||
          who < 1 ||
          who > 4) {
        return lobby;
      }
      final mine = who == player;
      return GameView(
        id,
        phase,
        games[id]![0],
        mine ? phases[phase]! : 'Espera tu turno',
        mine
            ? (phase == 'Paused'
                  ? 'Revisa la PC, la cámara y la conexión.'
                  : id == 'bowling'
                  ? 'Sujeta el teléfono con la mano ${profile.hand == 'left' ? 'izquierda' : 'derecha'}. Muestra solo esa mano y lanza suavemente.'
                  : games[id]![1])
            : 'Otro náufrago está jugando. Mira la PC.',
        mine,
      );
    }
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
  static GameStats? fromContext(Map<String, dynamic>? context) {
    final stats = context?['stats'];
    if (context?['version'] == 2 || context?['version'] == 3) {
      if (GameView.fromContext(context, context?['playerId']).id == 'lobby' ||
          stats is! Map ||
          stats.length != 10) {
        return null;
      }
      const ranges = {
        'score': [0, 1000000000],
        'progress': [0, 1],
        'elapsed': [0, 3600],
        'balance': [-1, 1],
        'distance': [0, 10000],
      };
      final values = <String, double>{};
      for (final entry in ranges.entries) {
        final key = entry.key,
            value = stats[entry.key],
            flag =
                stats['has${entry.key[0].toUpperCase()}${entry.key.substring(1)}'];
        if (flag is! bool ||
            value is! num ||
            !value.isFinite ||
            value < entry.value[0] ||
            value > entry.value[1]) {
          return null;
        }
        if (flag) values[key] = value.toDouble();
      }
      return GameStats._(values);
    }
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
    return GameStats._(values);
  }
}
