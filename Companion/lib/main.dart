import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:mobile_scanner/mobile_scanner.dart';

import 'control/page.dart';
import 'control/room_code.dart';
import 'control/lan_discovery.dart';

import 'dart:async';

import 'island_theme.dart';
import 'control/protocol.dart';
export 'control/protocol.dart' show controllerLink;

void main() => runApp(const GorillaCompanion());

class GorillaCompanion extends StatelessWidget {
  const GorillaCompanion({super.key});
  @override
  Widget build(BuildContext context) => MaterialApp(
    title: 'Gorilla Escape',
    debugShowCheckedModeBanner: false,
    theme: IslandStyle.theme(),
    home: const IslandHome(),
  );
}

class IslandHome extends StatefulWidget {
  const IslandHome({super.key});
  @override
  State<IslandHome> createState() => _IslandHomeState();
}

class _IslandHomeState extends State<IslandHome> with WidgetsBindingObserver {
  final nearby = LanDiscovery();
  Uri? selectedOrigin;
  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addObserver(this);
    nearby.addListener(nearbyChanged);
    unawaited(nearby.start());
  }

  void nearbyChanged() {
    if (mounted) {
      if (selectedOrigin != null &&
          !nearby.rooms.values.any((pc) => pc.origin == selectedOrigin)) {
        selectedOrigin = null;
      }
      setState(() {});
    }
  }

  @override
  void didChangeAppLifecycleState(AppLifecycleState state) {
    if (state == AppLifecycleState.resumed && !opening) {
      unawaited(nearby.start());
    } else if (state != AppLifecycleState.resumed) {
      unawaited(nearby.stop());
    }
  }

  final link = TextEditingController();
  final pin = TextEditingController();
  Uri? recentOrigin;
  String? message;
  bool opening = false;
  @override
  void dispose() {
    WidgetsBinding.instance.removeObserver(this);
    nearby.removeListener(nearbyChanged);
    nearby.dispose();
    link.dispose();
    pin.dispose();
    super.dispose();
  }

  Future<void> openWithPin() async {
    if (opening) return;
    if (roomCode(pin.text) == null) {
      setState(() => message = 'El código debe tener 4 letras o números.');
      return;
    }
    setState(() {
      opening = true;
      message = null;
    });
    final client = RoomCodeClient();
    try {
      final found = nearby.rooms.values.toList();
      final target =
          selectedOrigin ??
          (found.length == 1 ? found.single.origin : null) ??
          recentOrigin;
      if (selectedOrigin == null && found.length > 1) {
        throw const FormatException("CHOOSE_PC");
      }
      final uri = await client.join(pin.text, recentOrigin: target);
      if (!mounted) return;
      client.close();
      pin.clear();
      await open(uri.toString());
    } on FormatException catch (error) {
      if (mounted) {
        setState(
          () => message = error.message == 'PC_NOT_LINKED'
              ? 'No encontramos la PC. Conéctate al mismo Wi-Fi, busca de nuevo o escanea su QR.'
              : error.message == 'CHOOSE_PC'
              ? 'Selecciona la PC de tu partida.'
              : 'Sala no disponible. Revisa el código o espera antes de reintentar.',
        );
      }
    } catch (_) {
      if (mounted) {
        setState(
          () =>
              message = 'No pudimos contactar con la sala. Revisa la conexión.',
        );
      }
    } finally {
      client.close();
      if (mounted) setState(() => opening = false);
    }
  }

  Future<void> open(String text) async {
    Uri? uri;
    try {
      uri = controllerLink(text);
    } on FormatException {
      uri = null;
    }
    if (uri == null) {
      setState(
        () => message = 'Este enlace no es un QR de partida válido. Escanea el QR de la PC.',
      );
      return;
    }
    setState(() {
      opening = true;
      message = null;
    });
    try {
      await nearby.stop();
      recentOrigin = uri.replace(path: '/', query: null, fragment: null);
      link.clear();
      await HapticFeedback.selectionClick();
      if (!mounted) return;
      await Navigator.of(
        context,
      ).push<void>(MaterialPageRoute(builder: (_) => ControlPage(link: uri!)));
    } catch (_) {
      if (mounted) {
        setState(
          () => message = 'No se pudo iniciar el control. Inténtalo de nuevo.',
        );
      }
    } finally {
      if (mounted) setState(() => opening = false);
    }
  }

  Future<void> scan() async {
    final value = await Navigator.of(context)
        .push<String>(MaterialPageRoute(builder: (_) => const ScanPage()));
    if (value != null && mounted) await open(value);
  }

  @override
  Widget build(BuildContext context) => Scaffold(
    body: SafeArea(
      child: Center(
        child: ConstrainedBox(
          constraints: const BoxConstraints(maxWidth: 520),
          child: ListView(
            padding: const EdgeInsets.all(28),
            children: [
              const SizedBox(height: 20),
              const Row(
                children: [
                  IslandSeal(),
                  SizedBox(width: 12),
                  Expanded(
                    child: Text(
                      'GORILLA ESCAPE',
                      style: TextStyle(
                        fontFamily: 'IslandTitle',
                        fontSize: 23,
                        letterSpacing: 1,
                      ),
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 24),
              const IslandBanner(),
              const SizedBox(height: 24),
              const Text(
                'TU SALIDA\nDE LA ISLA',
                style: TextStyle(
                  fontFamily: 'IslandTitle',
                  fontSize: 38,
                  height: 1.04,
                  fontWeight: FontWeight.w900,
                  color: Color(0xfff4e6c9),
                ),
              ),
              const SizedBox(height: 20),
              const Text(
                'La PC muestra el mundo. Tu teléfono te ayuda a superar las pruebas de los gorilas.',
                style: TextStyle(fontSize: 18, height: 1.5),
              ),
              const SizedBox(height: 24),
              FilledButton.icon(
                onPressed: opening ? null : scan,
                icon: const Icon(Icons.qr_code_scanner),
                label: const Text('ESCANEAR QR DE LA PC'),
              ),
              const SizedBox(height: 16),
              Text(
                nearby.searching
                    ? 'Buscando partidas en tu Wi-Fi…'
                    : nearby.rooms.isNotEmpty
                    ? 'PC encontrada. Ingresa el código que muestra el juego.'
                    : 'Busca la PC en tu Wi-Fi o escanea su QR.',
              ),
              for (final pc in nearby.rooms.values)
                ListTile(
                  leading: Icon(
                    selectedOrigin == pc.origin ||
                            (selectedOrigin == null && nearby.rooms.length == 1)
                        ? Icons.check_circle
                        : Icons.computer,
                  ),
                  title: Text(pc.name),
                  onTap: opening
                      ? null
                      : () => setState(() => selectedOrigin = pc.origin),
                ),
              TextButton.icon(
                onPressed: opening || nearby.searching
                    ? null
                    : () {
                        selectedOrigin = null;
                        unawaited(nearby.start());
                      },
                icon: const Icon(Icons.wifi_find),
                label: const Text('BUSCAR PC'),
              ),
              ExpansionTile(
                key: ValueKey(nearby.rooms.isNotEmpty),
                initiallyExpanded: nearby.rooms.isNotEmpty,
                title: const Text('Ingresar código de sala'),
                children: [
                  TextField(
                    controller: pin,
                    autocorrect: false,
                    enableSuggestions: false,
                    textCapitalization: TextCapitalization.characters,
                    maxLength: 4,
                    inputFormatters: [
                      FilteringTextInputFormatter.allow(
                        RegExp(r'[2-9a-hjkmnp-zA-HJKMNP-Z]'),
                      ),
                    ],
                    decoration: const InputDecoration(
                      labelText: 'Código de 4 caracteres (ej. K7M4)',
                      counterText: '',
                    ),
                  ),
                  const SizedBox(height: 10),
                  const SizedBox(height: 12),
                  FilledButton(
                    onPressed: opening ? null : openWithPin,
                    child: const Text('CONECTAR CON CÓDIGO'),
                  ),
                  const SizedBox(height: 8),
                ],
              ),
              const SizedBox(height: 32),
              Container(
                padding: const EdgeInsets.all(22),
                decoration: BoxDecoration(
                  color: const Color(0xff1d3c33),
                  borderRadius: BorderRadius.circular(4),
                ),
                child: const Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    IslandSeal(size: 32),
                    SizedBox(height: 18),
                    Text(
                      'DESAFÍO DEL TRONCO',
                      style: TextStyle(
                        fontSize: 21,
                        fontWeight: FontWeight.w800,
                      ),
                    ),
                    SizedBox(height: 10),
                    Text(
                      'Cruza el río como el náufrago. Inclina suavemente para mantener el equilibrio.',
                      style: TextStyle(fontSize: 16, height: 1.5),
                    ),
                  ],
                ),
              ),
              const SizedBox(height: 28),
              const SizedBox(height: 14),
              const Text(
                'Abre Gorilla Escape en la PC y elige jugar con teléfono. Mantén ambos equipos en la misma Wi-Fi.',
                style: TextStyle(height: 1.5),
              ),
              const SizedBox(height: 20),
              ExpansionTile(
                title: const Text('Tengo un enlace de partida'),
                children: [
                  TextField(
                    controller: link,
                    autocorrect: false,
                    enableSuggestions: false,
                    obscureText: true,
                    decoration: const InputDecoration(
                      labelText: 'Enlace HTTPS del QR',
                    ),
                    keyboardType: TextInputType.url,
                  ),
                  const SizedBox(height: 12),
                  OutlinedButton(
                    onPressed: opening ? null : () => open(link.text),
                    child: const Text('ABRIR CONTROL'),
                  ),
                ],
              ),
              if (message != null)
                Padding(
                  padding: const EdgeInsets.symmetric(vertical: 16),
                  child: Semantics(
                    liveRegion: true,
                    child: Text(message!, style: const TextStyle(height: 1.5)),
                  ),
                ),
              const SizedBox(height: 16),
              const Text(
                'Sujeta el teléfono con ambas manos.\nNo lo lances ni lo agites.',
                textAlign: TextAlign.center,
                style: TextStyle(color: Color(0xffbfcec5), height: 1.5),
              ),
            ],
          ),
        ),
      ),
    ),
  );
}

class ScanPage extends StatefulWidget {
  const ScanPage({super.key});
  @override
  State<ScanPage> createState() => _ScanPageState();
}

class _ScanPageState extends State<ScanPage> {
  bool accepted = false;
  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: AppBar(title: const Text('QR de la PC')),
    body: Stack(
      children: [
        MobileScanner(
          errorBuilder: (context, error) => const Center(
            child: Padding(
              padding: EdgeInsets.all(28),
              child: Text(
                'No pudimos usar la cámara. Revisa su permiso en Ajustes o vuelve para abrir un enlace de partida.',
                textAlign: TextAlign.center,
              ),
            ),
          ),
          onDetect: (capture) {
            if (accepted) return;
            for (final code in capture.barcodes) {
              final value = code.rawValue;
              if (value != null) {
                accepted = true;
                Navigator.pop(context, value);
                break;
              }
            }
          },
        ),
        const Positioned(
          left: 24,
          right: 24,
          bottom: 32,
          child: Card(
            child: Padding(
              padding: EdgeInsets.all(20),
              child: Text(
                'Apunta al QR que muestra Gorilla Escape.\nEl enlace de partida caduca: usa uno nuevo.',
                textAlign: TextAlign.center,
              ),
            ),
          ),
        ),
      ],
    ),
  );
}
