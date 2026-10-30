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
          minimumSize: const Size(double.infinity, 56),
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
  final link = TextEditingController();
  String? message;
  bool opening = false;
  @override
  void dispose() {
    link.dispose();
    super.dispose();
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
                  Icon(Icons.forest, color: Color(0xffe5bd75), size: 34),
                  SizedBox(width: 12),
                  Text('GORILIMPIADAS', style: TextStyle(letterSpacing: 3)),
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
                onPressed: opening ? null : scan,
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
                      'SIETE PRUEBAS EN LA ISLA',
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
