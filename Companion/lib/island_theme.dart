import 'package:flutter/material.dart';

/// Shared expedition palette and local typography, also shipped with Unity/PWA.
abstract final class IslandStyle {
  static const forest = Color(0xff102b25);
  static const sand = Color(0xfff4e6c4);
  static const amber = Color(0xffe8b75f);
  static const wood = Color(0xff6a4028);
  static ThemeData theme() {
    final base = ThemeData(
      brightness: Brightness.dark,
      fontFamily: 'IslandText',
      scaffoldBackgroundColor: forest,
      colorScheme: const ColorScheme.dark(
        primary: amber,
        onPrimary: forest,
        surface: Color(0xff193d33),
        onSurface: sand,
        secondary: Color(0xff3f9c9a),
        error: Color(0xffe56a4f),
      ),
      useMaterial3: true,
    );
    return base.copyWith(
      textTheme: base.textTheme
          .apply(bodyColor: sand, displayColor: sand)
          .copyWith(
            headlineLarge: const TextStyle(
              fontFamily: 'IslandTitle',
              fontSize: 36,
              color: amber,
            ),
            headlineMedium: const TextStyle(
              fontFamily: 'IslandTitle',
              fontSize: 28,
              color: sand,
            ),
            titleLarge: const TextStyle(
              fontFamily: 'IslandTitle',
              fontSize: 22,
              color: sand,
            ),
          ),
      appBarTheme: const AppBarTheme(
        backgroundColor: forest,
        foregroundColor: sand,
        centerTitle: false,
      ),
      filledButtonTheme: FilledButtonThemeData(
        style: FilledButton.styleFrom(
          minimumSize: const Size(double.infinity, 56),
          backgroundColor: amber,
          foregroundColor: forest,
          textStyle: const TextStyle(fontFamily: 'IslandTitle', fontSize: 19),
          shape: const BeveledRectangleBorder(
            borderRadius: BorderRadius.all(Radius.circular(7)),
          ),
        ),
      ),
      outlinedButtonTheme: OutlinedButtonThemeData(
        style: OutlinedButton.styleFrom(
          foregroundColor: sand,
          minimumSize: const Size(48, 52),
          side: const BorderSide(color: amber),
          shape: const BeveledRectangleBorder(
            borderRadius: BorderRadius.all(Radius.circular(7)),
          ),
        ),
      ),
      cardTheme: const CardThemeData(
        color: Color(0xff193d33),
        elevation: 0,
        shape: BeveledRectangleBorder(
          side: BorderSide(color: Color(0xff7a6245)),
          borderRadius: BorderRadius.all(Radius.circular(8)),
        ),
      ),
      dividerColor: const Color(0xff52705a),
    );
  }
}

class IslandBanner extends StatelessWidget {
  final bool compact;
  const IslandBanner({super.key, this.compact = false});
  @override
  Widget build(BuildContext context) => ClipPath(
    clipper: ShapeBorderClipper(
      shape: const BeveledRectangleBorder(
        borderRadius: BorderRadius.all(Radius.circular(10)),
      ),
    ),
    child: Image.asset(
      'assets/brand/island-route.png',
      height: compact ? 120 : 160,
      width: double.infinity,
      fit: BoxFit.cover,
      excludeFromSemantics: true,
    ),
  );
}

class IslandSeal extends StatelessWidget {
  final double size;
  const IslandSeal({super.key, this.size = 40});
  @override
  Widget build(BuildContext context) => Image.asset(
    'assets/brand/island-seal.png',
    width: size,
    height: size,
    excludeFromSemantics: true,
  );
}
