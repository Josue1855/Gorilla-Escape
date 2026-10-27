import 'dart:io';
import 'dart:ui' as ui;

import 'package:flutter/material.dart';
import 'package:flutter/rendering.dart';
import 'package:flutter/services.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:gorilla_companion/main.dart';

void main() {
  testWidgets('branded home renders with bundled fonts and illustrations', (
    tester,
  ) async {
    for (final font in {
      'IslandTitle': 'LilitaOne.ttf',
      'IslandText': 'NunitoSans.ttf',
    }.entries) {
      final loader = FontLoader(font.key)
        ..addFont(rootBundle.load('assets/brand/${font.value}'));
      await loader.load();
    }
    final icons = FontLoader('MaterialIcons')
      ..addFont(rootBundle.load('fonts/MaterialIcons-Regular.otf'));
    await icons.load();
    await tester.binding.setSurfaceSize(const Size(393, 852));
    addTearDown(() => tester.binding.setSurfaceSize(null));
    final key = GlobalKey();
    await tester.pumpWidget(
      RepaintBoundary(key: key, child: const GorillaCompanion()),
    );
    await tester.runAsync(() async {
      final context = tester.element(find.byType(IslandHome));
      await precacheImage(
        const AssetImage('assets/brand/island-route.png'),
        context,
      );
      await precacheImage(
        const AssetImage('assets/brand/island-seal.png'),
        context,
      );
    });
    await tester.pumpAndSettle();
    expect(find.text('GORILLA ESCAPE'), findsOneWidget);
    expect(tester.takeException(), isNull);
    const directory = String.fromEnvironment('DESIGN_SCREENSHOT_DIR');
    if (directory.isNotEmpty) {
      final boundary =
          key.currentContext!.findRenderObject()! as RenderRepaintBoundary;
      await tester.runAsync(() async {
        final image = await boundary.toImage(pixelRatio: 1);
        final bytes = await image.toByteData(format: ui.ImageByteFormat.png);
        await File('$directory/flutter-home.png')
            .writeAsBytes(bytes!.buffer.asUint8List());
        image.dispose();
      });
    }
  });
}
