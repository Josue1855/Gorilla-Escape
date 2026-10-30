import 'dart:async';
import 'dart:convert';
import 'dart:math' as math;

import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_rotation_sensor/flutter_rotation_sensor.dart';
import 'package:sensors_plus/sensors_plus.dart';
import 'package:wakelock_plus/wakelock_plus.dart';

import 'protocol.dart';
import 'transport.dart';
import 'game_surface.dart';

class ControlPage extends StatefulWidget {
  final Uri link;
  const ControlPage({super.key, required this.link});
  @override
  State<ControlPage> createState() => _ControlPageState();
}

class _ControlPageState extends State<ControlPage> with WidgetsBindingObserver {
  final transport = NativeTransport();
  final motion = MotionStore();
  int emittedRevision = -1;
  final name = TextEditingController();
  String profileStatus = '';
  bool savingProfile = false;
  final List<StreamSubscription<dynamic>> subscriptions = [];
  Timer? timer;
  bool activated = false, suspended = true, portrait = true;
  String sensorError = '';
  bool starting = false, foreground = true, permissionRequested = false;
  String previousTransportState = "";
  String? previousPhase;
  double? previousScore;
  int sensorGeneration = 0, renderTick = 0, inspectedAcks = 0;
  Timer? recoveryTimer;
  int recoveryAttempts = 0;
  bool recovering = false, commandPending = false;
  bool? wakeDesired;
  String commandStatus = '', profileContext = '';
  double? neutralGamma;
  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addObserver(this);
    transport.addListener(refresh);
    unawaited(
      SystemChrome.setPreferredOrientations([DeviceOrientation.portraitUp]),
    );
    unawaited(transport.connect(widget.link));
  }

  void refresh() {
    if (!mounted) return;
    final connectedNow = transport.state == 'connected';
    previousTransportState = transport.state;
    final ctx = transport.gameContext;
    final profile = GameControlProfile.fromContext(ctx, transport.playerId);
    if (profile.valid && profile.contextId != profileContext) {
      profileContext = profile.contextId!;
      motion.setAction(false);
      neutralGamma = null;
      previousPhase = null;
      previousScore = null;
    }
    if (connectedNow && foreground && profile.valid) {
      if (profile.phoneRequired && subscriptions.isEmpty && !starting) {
        unawaited(startSensors());
      }
      if (!profile.phoneRequired && subscriptions.isNotEmpty) {
        unawaited(stopSensors());
      }
      unawaited(keepAwake(profile.phoneRequired && activated));
    }
    if (!connectedNow &&
        foreground &&
        transport.canResume &&
        transport.state != 'connecting' &&
        !recovering) {
      scheduleRecovery();
    }
    final phase = ctx?['phase'];
    if (phase is String) {
      if (previousPhase != null && phase != previousPhase && foreground) {
        if (phase == 'Won' ||
            phase == 'Lost' ||
            phase == 'Fell' ||
            phase == 'Playing') {
          unawaited(HapticFeedback.mediumImpact());
        }
      }
      previousPhase = phase;
    }
    final stats = GameStats.fromContext(ctx);
    final score = stats?.values['score'];
    if (score != null) {
      if (previousScore != null && score > previousScore! && foreground) {
        unawaited(HapticFeedback.lightImpact());
      }
      previousScore = score;
    }
    setState(() {});
  }

  Future<void> stateMessage(String state) async {
    try {
      await transport.control('CLIENT_STATE', {'state': state});
    } catch (_) {}
  }

  Future<void> keepAwake(bool enabled) async {
    if (wakeDesired == enabled) return;
    wakeDesired = enabled;
    try {
      await WakelockPlus.toggle(enable: enabled);
    } catch (_) {
      wakeDesired = null;
    }
  }

  void scheduleRecovery() {
    if (recoveryTimer != null ||
        recoveryAttempts >= 3 ||
        !mounted ||
        !foreground) {
      return;
    }
    recoveryTimer = Timer(
      Duration(milliseconds: 1000 * (recoveryAttempts + 1)),
      () {
        recoveryTimer = null;
        unawaited(recoverSession());
      },
    );
  }

  Future<void> recoverSession() async {
    if (recovering || !mounted || !foreground || !transport.canResume) return;
    recovering = true;
    recoveryTimer?.cancel();
    recoveryTimer = null;
    await stopSensors();
    await transport.connect(widget.link, resume: true);
    recovering = false;
    if (!mounted) return;
    if (transport.state == 'connected') {
      recoveryAttempts = 0;
      profileContext = '';
      refresh();
    } else {
      recoveryAttempts++;
      scheduleRecovery();
    }
  }

  Future<void> sendCommand(String action, String target) async {
    if (commandPending) return;
    setState(() {
      commandPending = true;
      commandStatus = 'Enviando…';
    });
    try {
      await transport.command(action, target: target);
      commandStatus = 'Solicitud enviada a la PC.';
    } catch (_) {
      commandStatus = 'La PC no confirmó la solicitud. Inténtalo de nuevo.';
    } finally {
      if (mounted) setState(() => commandPending = false);
    }
  }

  Future<void> stopSensors() async {
    ++sensorGeneration;
    unawaited(keepAwake(false));
    timer?.cancel();
    timer = null;
    motion.clear();
    final old = List.of(subscriptions);
    subscriptions.clear();
    for (final subscription in old) {
      try {
        await subscription.cancel();
      } catch (_) {}
    }
    suspended = true;
    unawaited(keepAwake(false));
    await stateMessage('suspended');
  }

  Future<void> startSensors() async {
    if (starting) return;
    starting = true;
    try {
      await stopSensors();
      final generation = sensorGeneration;
      if (!mounted || !foreground || transport.state != 'connected') return;
      activated = true;
      unawaited(keepAwake(true));
      sensorError = '';
      try {
        if (!permissionRequested && RotationSensor.shouldRequestPermission) {
          permissionRequested = true;
          await RotationSensor.requestPermission();
        }
        if (!mounted || !foreground || generation != sensorGeneration) return;
        RotationSensor.referenceFrame = ReferenceFrame.arbitrary;
        RotationSensor.coordinateSystem = CoordinateSystem.device();
        RotationSensor.samplingPeriod = const Duration(milliseconds: 20);
        void failed(Object error) {
          sensorError =
              'No llegan datos de movimiento. Revisa el permiso de sensores.';
          refresh();
        }

        void record(String group, Map<String, num> values) {
          if (mounted && foreground && generation == sensorGeneration) {
            motion.record(group, values, transport.clock.elapsedMilliseconds);
            if (group == 'orientation') {
              neutralGamma ??= values['gamma']?.toDouble();
            }
          }
        }

        subscriptions.add(
          RotationSensor.orientationStream.listen(
            (event) =>
                record('orientation', deviceOrientation(event.rotationMatrix)),
            onError: failed,
          ),
        );
        subscriptions.add(
          accelerometerEventStream(
            samplingPeriod: const Duration(milliseconds: 20),
          ).listen(
            (e) => record('accelerationIncludingGravity', {
              'x': e.x,
              'y': e.y,
              'z': e.z,
            }),
            onError: failed,
          ),
        );
        subscriptions.add(
          userAccelerometerEventStream(
            samplingPeriod: const Duration(milliseconds: 20),
          ).listen(
            (e) => record('acceleration', {'x': e.x, 'y': e.y, 'z': e.z}),
            onError: failed,
          ),
        );
        subscriptions.add(
          gyroscopeEventStream(samplingPeriod: const Duration(milliseconds: 20))
              .listen(
                (e) => record('rotationRate', {
                  'alpha': e.z * 180 / math.pi,
                  'beta': e.x * 180 / math.pi,
                  'gamma': e.y * 180 / math.pi,
                }),
                onError: failed,
              ),
        );
        timer = Timer.periodic(const Duration(milliseconds: 20), (_) {
          if (!mounted || generation != sensorGeneration) return;
          final now = transport.clock.elapsedMilliseconds;
          final ready =
              foreground &&
              portrait &&
              motion.ready(now) &&
              transport.state == 'connected';
          if (ready) {
            if (suspended) {
              suspended = false;
              unawaited(stateMessage('active'));
            }
            if (motion.revision != emittedRevision) {
              emittedRevision = motion.revision;
              unawaited(transport.motion(motion.payload(now)));
            }
          } else if (!suspended) {
            suspended = true;
            unawaited(stateMessage('suspended'));
          }
          // Keep animation updates bounded independently of sensor delivery.
          if (++renderTick % 5 == 0) refresh();
        });
      } catch (_) {
        sensorError =
            'No pudimos activar los sensores. Revisa sus permisos en Ajustes.';
      }
      refresh();
    } finally {
      starting = false;
      // The OS permission sheet may complete while the app is inactive.
      if (mounted &&
          foreground &&
          transport.state == 'connected' &&
          activated &&
          subscriptions.isEmpty &&
          sensorError.isEmpty) {
        scheduleMicrotask(startSensors);
      }
    }
  }

  @override
  void didChangeAppLifecycleState(AppLifecycleState state) {
    foreground = state == AppLifecycleState.resumed;
    if (!foreground) {
      motion.setAction(false);
      recoveryTimer?.cancel();
      recoveryTimer = null;
    }
    if (state == AppLifecycleState.resumed) {
      recoveryAttempts = 0;
      if (transport.canResume && transport.needsRecovery) {
        unawaited(recoverSession());
      } else if (activated &&
          transport.state == 'connected' &&
          GameControlProfile.fromContext(
            transport.gameContext,
            transport.playerId,
          ).phoneRequired) {
        unawaited(startSensors());
      }
    } else if (state != AppLifecycleState.inactive) {
      unawaited(stopSensors());
    }
  }

  @override
  void dispose() {
    ++sensorGeneration;
    recoveryTimer?.cancel();
    foreground = false;
    unawaited(keepAwake(false));
    WidgetsBinding.instance.removeObserver(this);
    timer?.cancel();
    for (final subscription in subscriptions) {
      unawaited(subscription.cancel());
    }
    name.dispose();
    transport.removeListener(refresh);
    transport.dispose();
    unawaited(SystemChrome.setPreferredOrientations([]));
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    portrait = MediaQuery.orientationOf(context) == Orientation.portrait;
    final stats = GameStats.fromContext(transport.gameContext);
    final connected = transport.state == 'connected';
    final profile = GameControlProfile.fromContext(
      transport.gameContext,
      transport.playerId,
    );
    final gamma = motion.gamma(transport.clock.elapsedMilliseconds) ?? 0;
    final status = !connected
        ? (transport.state == 'connecting'
              ? 'Conectando con la isla…'
              : 'Conexión interrumpida')
        : profile.valid && !profile.phoneRequired
        ? 'CONECTADO · MANOS LIBRES'
        : !activated
        ? 'Conectado · activa el control'
        : !portrait
        ? 'Coloca el teléfono vertical'
        : suspended
        ? 'Esperando movimiento reciente…'
        : 'CONTROL ACTIVO';
    return Scaffold(
      appBar: AppBar(
        title: Text(
          transport.playerId == null
              ? 'Tu control'
              : 'NÁUFRAGO ${transport.playerId}',
        ),
      ),
      body: SafeArea(
        child: Center(
          child: ConstrainedBox(
            constraints: const BoxConstraints(maxWidth: 560),
            child: ListView(
              padding: const EdgeInsets.all(28),
              children: [
                Semantics(
                  liveRegion: true,
                  child: Text(
                    status,
                    style: const TextStyle(
                      color: Color(0xffe5bd75),
                      letterSpacing: 1.5,
                    ),
                  ),
                ),
                const SizedBox(height: 30),
                GameControlSurface(
                  contextData: transport.gameContext,
                  player: transport.playerId,
                  sensorsReady: activated && !suspended && connected,
                  tilt: ((gamma - (neutralGamma ?? gamma)) / 30).clamp(
                    -1.0,
                    1.0,
                  ),
                  busy: commandPending,
                  onCommand: (action, target) =>
                      unawaited(sendCommand(action, target)),
                  scorePanel: stats == null
                      ? null
                      : GameScorePanel(stats: stats),
                  recenter: Listener(
                    onPointerDown: (_) {
                      unawaited(HapticFeedback.selectionClick());
                      motion.setAction(true, center: true);
                    },
                    onPointerUp: (_) => motion.setAction(false, center: true),
                    onPointerCancel: (_) =>
                        motion.setAction(false, center: true),
                    child: OutlinedButton(
                      onPressed: () {},
                      child: const Text('CENTRAR APUNTADO'),
                    ),
                  ),
                ),
                Text(commandStatus),
                const SizedBox(height: 24),
                if (connected &&
                    profile.phoneRequired &&
                    (!activated || sensorError.isNotEmpty))
                  FilledButton(
                    onPressed: startSensors,
                    child: Text(
                      activated ? 'REINTENTAR SENSORES' : 'ACTIVAR CONTROL',
                    ),
                  ),
                if (!connected &&
                    transport.state != 'connecting' &&
                    transport.canResume)
                  FilledButton(
                    onPressed: () => unawaited(recoverSession()),
                    child: const Text('RECONECTAR'),
                  ),
                if (!connected && transport.state != 'connecting')
                  Padding(
                    padding: const EdgeInsets.symmetric(vertical: 16),
                    child: Text(
                      transport.code == 'TLS_UNTRUSTED'
                          ? 'El teléfono no confía en el certificado de la PC. Usa el QR HTTPS del túnel o configura la confianza del certificado local.'
                          : 'Vuelve a la PC y escanea un QR nuevo si la conexión no se recupera.',
                    ),
                  ),
                if (sensorError.isNotEmpty) Text(sensorError),
                const SizedBox(height: 20),
                const Text(
                  'Sujeta el teléfono con ambas manos. El control se ajusta automáticamente; el panel muestra el estado de la partida.',
                  textAlign: TextAlign.center,
                ),
                if (connected)
                  ExpansionTile(
                    title: const Text('Tu nombre en la isla (opcional)'),
                    children: [
                      TextField(
                        controller: name,
                        maxLength: 40,
                        decoration: const InputDecoration(
                          labelText: 'Nombre del náufrago',
                        ),
                      ),
                      TextButton(
                        onPressed: savingProfile || transport.profileAttempted
                            ? null
                            : () async {
                                if (name.text.trim().isEmpty) return;
                                setState(() => savingProfile = true);
                                final result = await transport.saveProfile(
                                  name.text,
                                );
                                if (!mounted) return;
                                setState(() {
                                  savingProfile = false;
                                  profileStatus = result == 'SAVED'
                                      ? 'Nombre guardado en esta PC.'
                                      : result == 'INVALID'
                                      ? 'Usa hasta 40 letras, números, espacios o signos simples.'
                                      : result == 'UNAVAILABLE'
                                      ? 'El registro aún no está disponible. Puedes intentarlo más tarde.'
                                      : 'No pudimos confirmar el guardado. Puedes seguir jugando.';
                                });
                              },
                        child: const Text('GUARDAR NOMBRE'),
                      ),
                      if (profileStatus.isNotEmpty) Text(profileStatus),
                    ],
                  ),
                ExpansionTile(
                  title: const Text('Ayuda de conexión'),
                  onExpansionChanged: (open) {
                    if (open) {
                      setState(() => inspectedAcks = transport.motionAcks);
                    }
                  },
                  children: [
                    Text('Muestras confirmadas: $inspectedAcks'),
                    if (transport.code.isNotEmpty)
                      Text('Código: ${transport.code}'),
                    TextButton(
                      onPressed: () => Clipboard.setData(
                        ClipboardData(text: jsonEncode(transport.diagnostic())),
                      ),
                      child: const Text('COPIAR DIAGNÓSTICO'),
                    ),
                  ],
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}

class GameScorePanel extends StatelessWidget {
  final GameStats stats;
  const GameScorePanel({super.key, required this.stats});
  @override
  Widget build(BuildContext context) => Card(
    child: Padding(
      padding: const EdgeInsets.all(16),
      child: Column(
        children: [
          if (stats.values.containsKey('score'))
            Text(
              'Puntos: ${stats['score'].round()}',
              style: Theme.of(context).textTheme.headlineSmall,
            ),
          if (stats.values.containsKey('progress')) ...[
            Text('Progreso ${(stats['progress'] * 100).round()} %'),
            LinearProgressIndicator(
              value: stats['progress'],
              semanticsLabel: 'Progreso del juego',
            ),
          ],
          if (stats.values.containsKey('balance')) ...[
            const Text('Equilibrio en la PC'),
            LinearProgressIndicator(
              value: (stats['balance'] + 1) / 2,
              semanticsLabel: 'Equilibrio en la PC',
            ),
          ],
          if (stats.values.containsKey('elapsed'))
            Text('Tiempo ${stats['elapsed'].toStringAsFixed(1)} s'),
          if (stats.values.containsKey('distance'))
            Text('Recorrido ${stats['distance'].toStringAsFixed(1)} m'),
        ],
      ),
    ),
  );
}
