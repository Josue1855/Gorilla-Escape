import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:mobile_scanner/mobile_scanner.dart';

import 'control/page.dart';
import 'control/protocol.dart';
export 'control/protocol.dart' show controllerLink;

void main() => runApp(const GorillaCompanion());

class GorillaCompanion extends StatelessWidget {
  const GorillaCompanion({super.key});
  @override
  Widget build(BuildContext context) => MaterialApp(
    title: 'Gorilla Escape',
    debugShowCheckedModeBanner: false,
    theme: ThemeData(
      brightness: Brightness.dark,
      scaffoldBackgroundColor: const Color(0xff102b25),
      colorScheme: ColorScheme.fromSeed(
        seedColor: const Color(0xffe5bd75),
        brightness: Brightness.dark,
      ),
      useMaterial3: true,
      filledButtonTheme: FilledButtonThemeData(
        style: FilledButton.styleFrom(
          minimumSize: const Size(0, 56),
          backgroundColor: const Color(0xffe5bd75),
          foregroundColor: const Color(0xff102b25),
        ),
      ),
    ),
    home: const IslandHome(),
  );
}

class IslandHome extends StatefulWidget {
  const IslandHome({super.key});
  @override
  State<IslandHome> createState() => _IslandHomeState();
}

class _IslandHomeState extends State<IslandHome> {
  static const _linkChannel = MethodChannel('com.gorillaescape/app_links');
  final link = TextEditingController();
  String? message;
  bool opening = false, scanning = false, replacingLink = false;
  String? activeLink;

  @override
  void initState() {
    super.initState();
    _initAppLinks();
  }

  void _initAppLinks() async {
    _linkChannel.setMethodCallHandler((call) async {
      if (call.method == 'onLinkReceived') {
        final rawLink = call.arguments as String?;
        if (rawLink != null && mounted) {
          await open(rawLink);
        }
      }
    });

    try {
      final initialLink = await _linkChannel.invokeMethod<String>(
        'getInitialLink',
      );
      if (initialLink != null && mounted) {
        await open(initialLink);
      }
    } catch (_) {}
  }

  @override
  void dispose() {
    _linkChannel.setMethodCallHandler(null);
    link.dispose();
    super.dispose();
  }

  Future<void> open(String text) async {
    if (!mounted) return;
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
    if (opening) {
      if (replacingLink || activeLink == uri.toString()) return;
      replacingLink = true;
      final replace = await showDialog<bool>(
        context: context,
        builder: (dialog) => AlertDialog(
          title: const Text('¿Usar otra sala?'),
          content: const Text(
            'Cerrarás este control para abrir el QR nuevo. La PC conservará los resultados ya confirmados.',
          ),
          actions: [
            TextButton(
              onPressed: () => Navigator.pop(dialog, false),
              child: const Text('SEGUIR AQUÍ'),
            ),
            FilledButton(
              onPressed: () => Navigator.pop(dialog, true),
              child: const Text('ABRIR QR NUEVO'),
            ),
          ],
        ),
      );
      replacingLink = false;
      if (replace == true && mounted) {
        Navigator.of(context).popUntil((route) => route.isFirst);
        await Future<void>.delayed(Duration.zero);
        if (mounted) await open(text);
      }
      return;
    }
    setState(() {
      opening = true;
      activeLink = uri.toString();
      message = null;
    });
    try {
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
      if (mounted) {
        setState(() {
          opening = false;
          activeLink = null;
        });
      }
    }
  }

  Future<void> scan() async {
    if (opening || scanning) return;
    setState(() => scanning = true);
    String? value;
    try {
      value = await Navigator.of(context)
          .push<String>(MaterialPageRoute(builder: (_) => const ScanPage()));
    } finally {
      if (mounted) setState(() => scanning = false);
    }
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
                  Icon(Icons.forest, color: Color(0xffe5bd75), size: 34),
                  SizedBox(width: 12),
                  Expanded(
                    child: Text(
                      'GORILLA ESCAPE',
                      style: TextStyle(
                        letterSpacing: 2,
                        fontWeight: FontWeight.w800,
                      ),
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 48),
              const Text(
                'TU SALIDA\nDE LA ISLA',
                style: TextStyle(
                  fontSize: 44,
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
                onPressed: opening || scanning ? null : scan,
                icon: const Icon(Icons.qr_code_scanner),
                label: const Text('ESCANEAR QR DE LA PC'),
              ),
              const SizedBox(height: 32),
              Container(
                padding: const EdgeInsets.all(22),
                decoration: BoxDecoration(
                  color: const Color(0xff1d3c33),
                  borderRadius: BorderRadius.circular(24),
                ),
                child: const Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Icon(Icons.water, color: Color(0xff9bc6bd), size: 42),
                    SizedBox(height: 18),
                    Text(
                      'DIEZ PRUEBAS EN LA ISLA',
                      style: TextStyle(
                        fontSize: 21,
                        fontWeight: FontWeight.w800,
                      ),
                    ),
                    SizedBox(height: 10),
                    Text(
                      'Conecta tu teléfono y elige una prueba en la PC. Las instrucciones cambiarán con el juego.',
                      style: TextStyle(fontSize: 16, height: 1.5),
                    ),
                  ],
                ),
              ),
              const SizedBox(height: 28),
              const SizedBox(height: 14),
              const Text(
                'Abre Gorilla Escape en la PC y elige jugar con teléfono. Usa el QR vigente de la PC. En conexión local, utiliza la misma Wi-Fi.',
                style: TextStyle(height: 1.5),
              ),
              const SizedBox(height: 20),
              ExpansionTile(
                title: const Text('Tengo un enlace de partida'),
                childrenPadding: const EdgeInsets.symmetric(
                  horizontal: 8,
                  vertical: 8,
                ),
                children: [
                  TextField(
                    controller: link,
                    autocorrect: false,
                    enableSuggestions: false,
                    obscureText: false,
                    scrollPadding: const EdgeInsets.all(80),
                    decoration: const InputDecoration(
                      labelText: 'Enlace HTTPS del QR',
                      hintText: 'https://...',
                    ),
                    keyboardType: TextInputType.url,
                  ),
                  const SizedBox(height: 12),
                  OutlinedButton(
                    onPressed: opening ? null : () => open(link.text),
                    child: const Text('ABRIR CONTROL'),
                  ),
                  if (message != null)
                    Padding(
                      padding: const EdgeInsets.only(top: 12, bottom: 4),
                      child: Semantics(
                        liveRegion: true,
                        child: Text(
                          message!,
                          style: const TextStyle(
                            color: Color(0xffffa07a),
                            fontWeight: FontWeight.w600,
                            height: 1.4,
                          ),
                        ),
                      ),
                    ),
                ],
              ),
              const SizedBox(height: 16),
              const Text(
                'Sujeta el teléfono con firmeza. No lo sueltes ni lo lances.',
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
  String? scanMessage;
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
              if (value != null && controllerLink(value) == null) {
                if (scanMessage == null) {
                  setState(
                    () => scanMessage = 'Este QR no es una partida de Gorilla Escape. Apunta al QR vigente de la PC.',
                  );
                }
                continue;
              }
              if (value != null) {
                accepted = true;
                Navigator.pop(context, value);
                break;
              }
            }
          },
        ),
        Positioned(
          left: 24,
          right: 24,
          bottom: 32,
          child: Card(
            child: Padding(
              padding: EdgeInsets.all(20),
              child: Text(
                scanMessage ?? 'Apunta al QR que muestra Gorilla Escape.\nEl enlace de partida caduca: usa uno nuevo.',
                textAlign: TextAlign.center,
              ),
            ),
          ),
        ),
      ],
    ),
  );
}
