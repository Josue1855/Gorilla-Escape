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

class ControlPage extends StatefulWidget {
  final Uri link;
  const ControlPage({super.key, required this.link});
  @override
  State<ControlPage> createState() => _ControlPageState();
}

class _ControlPageState extends State<ControlPage> with WidgetsBindingObserver {
  final transport = NativeTransport();
  final motion = MotionStore();
  final name = TextEditingController();
  String profileStatus = '';
  bool savingProfile = false;
  final List<StreamSubscription<dynamic>> subscriptions = [];
  Timer? timer;
  bool activated = false, suspended = true, portrait = true;
  String sensorError = '';
  bool starting = false, foreground = true, permissionRequested = false;
  String previousTransportState = "";
  int sensorGeneration = 0, renderTick = 0, inspectedAcks = 0;
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
    final becameConnected =
        connectedNow && previousTransportState != 'connected';
    previousTransportState = transport.state;
    if (becameConnected && foreground) unawaited(startSensors());
    setState(() {});
  }

  Future<void> stateMessage(String state) async {
    try {
      await transport.control('CLIENT_STATE', {'state': state});
    } catch (_) {}
  }

  Future<void> keepAwake(bool enabled) async {
    try {
      await WakelockPlus.toggle(enable: enabled);
    } catch (_) {}
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
            unawaited(transport.motion(motion.payload(now)));
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
    if (state == AppLifecycleState.resumed) {
      if (activated && transport.state == 'connected') {
        unawaited(startSensors());
      }
    } else if (state != AppLifecycleState.inactive) {
      unawaited(stopSensors());
    }
  }

  @override
  void dispose() {
    ++sensorGeneration;
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
    final view = GameView.fromContext(
      transport.gameContext,
      transport.playerId,
    );
    final stats = GameStats.fromContext(transport.gameContext);
    final connected = transport.state == 'connected';
    final gamma = motion.gamma(transport.clock.elapsedMilliseconds) ?? 0;
    final status = !connected
        ? (transport.state == 'connecting'
              ? 'Conectando con la isla…'
              : 'Conexión interrumpida')
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
                Text(
                  view.title,
                  style: const TextStyle(
                    fontSize: 42,
                    height: 1.05,
                    fontWeight: FontWeight.w900,
                  ),
                ),
                const SizedBox(height: 30),
                Container(
                  height: 180,
                  decoration: BoxDecoration(
                    color: const Color(0xff1d3c33),
                    borderRadius: BorderRadius.circular(28),
                  ),
                  child: Center(
                    child: Transform.rotate(
                      angle: activated && !suspended
                          ? (stats?['roll'] ?? gamma).clamp(-40, 40) *
                                math.pi /
                                180
                          : 0,
                      child: Icon(
                        view.phase == 'Won'
                            ? Icons.emoji_events
                            : Icons.accessibility_new,
                        size: 100,
                        color: const Color(0xffe5bd75),
                      ),
                    ),
                  ),
                ),
                if (stats != null) ...[
                  const SizedBox(height: 20),
                  GameScorePanel(stats: stats),
                ],
                const SizedBox(height: 24),
                Semantics(
                  liveRegion: true,
                  child: Text(
                    view.heading,
                    style: const TextStyle(
                      fontSize: 25,
                      fontWeight: FontWeight.bold,
                    ),
                  ),
                ),
                const SizedBox(height: 12),
                Text(
                  view.instruction,
                  style: const TextStyle(fontSize: 18, height: 1.5),
                ),
                const SizedBox(height: 24),
                if (connected && (!activated || sensorError.isNotEmpty))
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
                    onPressed: () async {
                      await transport.connect(widget.link, resume: true);
                      if (activated && transport.state == 'connected') {
                        await startSensors();
                      }
                    },
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
  Widget build(BuildContext context) => Semantics(
    label: 'Estado real de la partida',
    child: Card(
      child: Padding(
        padding: const EdgeInsets.all(20),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            const Wrap(
              alignment: WrapAlignment.spaceBetween,
              spacing: 12,
              runSpacing: 8,
              children: [
                Text('← IZQUIERDA'),
                Text('CENTRO'),
                Text('DERECHA →'),
              ],
            ),
            const SizedBox(height: 12),
            SizedBox(
              height: 32,
              child: LayoutBuilder(
                builder: (context, box) => Stack(
                  children: [
                    Positioned.fill(
                      top: 8,
                      bottom: 8,
                      child: ColoredBox(color: Color(0xff346d59)),
                    ),
                    Positioned(
                      left: box.maxWidth / 2 - 1,
                      top: 5,
                      bottom: 5,
                      child: SizedBox(
                        width: 2,
                        child: ColoredBox(color: Colors.white38),
                      ),
                    ),
                    Positioned(
                      left: ((stats['tendency'] + 1) / 2 * (box.maxWidth - 4)),
                      top: 6,
                      bottom: 6,
                      child: SizedBox(
                        width: 4,
                        child: ColoredBox(color: Color(0xffffbd59)),
                      ),
                    ),
                    Positioned(
                      left: ((stats['balance'] + 1) / 2 * (box.maxWidth - 8)),
                      top: 0,
                      bottom: 0,
                      child: SizedBox(
                        width: 8,
                        child: ColoredBox(color: Colors.white),
                      ),
                    ),
                  ],
                ),
              ),
            ),
            const Text(
              'Blanco: apoyo · Ámbar: tendencia',
              style: TextStyle(fontSize: 13),
            ),
            const SizedBox(height: 16),
            Text(
              'Recorrido ${(stats['progress'] * 100).round()} %',
              style: const TextStyle(fontSize: 24, fontWeight: FontWeight.bold),
            ),
            LinearProgressIndicator(
              value: stats['progress'],
              semanticsLabel: 'Avance del cruce',
            ),
            const SizedBox(height: 12),
            Text(
              '${stats['distance'].toStringAsFixed(1)} m · ${stats['elapsed'].toStringAsFixed(1)} s',
            ),
            Text(
              'Giro ${stats['roll'].round()}° · Paso ${stats['speed'].toStringAsFixed(2)} m/s',
            ),
            Text('Viento ${stats['wind'] >= 0 ? '→' : '←'}'),
          ],
        ),
      ),
    ),
  );
}
