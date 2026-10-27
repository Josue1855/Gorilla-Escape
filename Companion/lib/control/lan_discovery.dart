import 'dart:async';
import 'dart:io';

import 'package:flutter/services.dart';

import 'package:bonsoir_platform_interface/bonsoir_platform_interface.dart';
import 'package:flutter/foundation.dart';

import 'room_code.dart';

/// Only a LAN announcement; no directory, admissions or sensors are broadcast.
class NearbyPc {
  final String name;
  final Uri origin;
  const NearbyPc(this.name, this.origin);
  static NearbyPc? fromService(BonsoirService service) {
    final type = service.type.toLowerCase().replaceFirst(
      RegExp(r'\.(?:local\.)?$'),
      '',
    );
    if (type != '_gorillaescape._tcp' ||
        service.attributes['v'] != '1' ||
        service.attributes['game'] != 'gorilla-escape') {
      return null;
    }
    final origin = roomOrigin(service.attributes['origin'] ?? '');
    if (origin == null) return null;
    final ip = origin.host.split('.').map(int.tryParse).toList();
    final privateIp =
        ip.length == 4 &&
        ip.every((n) => n != null && n >= 0 && n <= 255) &&
        (ip[0] == 10 ||
            (ip[0] == 192 && ip[1] == 168) ||
            (ip[0] == 172 && ip[1]! >= 16 && ip[1]! <= 31));
    if (!origin.host.endsWith('.trycloudflare.com') && !privateIp) return null;
    return NearbyPc(service.name, origin);
  }
}

class LanDiscovery extends ChangeNotifier {
  final BonsoirAction<BonsoirDiscoveryEvent> Function()? createAction;
  LanDiscovery({this.createAction});
  final Map<String, NearbyPc> rooms = {};
  BonsoirAction<BonsoirDiscoveryEvent>? _action;
  StreamSubscription<BonsoirDiscoveryEvent>? _events;
  Timer? _deadline;
  bool searching = false;
  bool unavailable = false;
  bool _disposed = false;
  int _generation = 0;

  Future<void> start() async {
    if (_disposed || searching) return;
    final generation = ++_generation;
    searching = true;
    unavailable = false;
    rooms.clear();
    notifyListeners();
    try {
      if (createAction == null && Platform.isAndroid) {
        final allowed = await const MethodChannel(
          "gorilla_escape/local_discovery",
        ).invokeMethod<bool>("requestLocalNetwork");
        if (allowed != true) throw StateError("LAN_PERMISSION_DENIED");
        if (_disposed || generation != _generation) return;
      }
      // Depend on the native Darwin/Android plugins directly: the umbrella
      // package's Linux dbus dependency conflicts with the current wake lock.
      final action =
          createAction?.call() ??
          BonsoirPlatformInterface.instance.createDiscoveryAction(
            '_gorillaescape._tcp',
            printLogs: false,
          );
      _action = action;
      await action.initialize();
      if (_disposed || generation != _generation) {
        await action.stop();
        return;
      }
      _events = action.eventStream!.listen(
        (event) {
          if (_disposed || generation != _generation) return;
          if (event is BonsoirDiscoveryServiceFoundEvent &&
              action is ServiceResolver) {
            unawaited(
              event.service
                  .resolve(action as ServiceResolver)
                  .catchError((Object _) {}),
            );
          } else if (event is BonsoirDiscoveryServiceResolvedEvent ||
              event is BonsoirDiscoveryServiceUpdatedEvent) {
            final service = event.service;
            if (service != null) {
              final pc = NearbyPc.fromService(service);
              if (pc != null) {
                rooms[pc.name] = pc;
              } else {
                rooms.remove(service.name);
              }
              notifyListeners();
            }
          } else if (event is BonsoirDiscoveryServiceLostEvent) {
            rooms.remove(event.service.name);
            notifyListeners();
          }
        },
        onError: (Object _) {
          if (!_disposed) {
            unavailable = true;
            unawaited(stop());
          }
        },
      );
      await action.start();
      if (_disposed || generation != _generation) {
        await action.stop();
        return;
      }
      _deadline = Timer(const Duration(seconds: 12), () => unawaited(stop()));
    } catch (_) {
      if (!_disposed && generation == _generation) {
        unavailable = true;
        await stop();
      }
    }
  }

  Future<void> stop() async {
    ++_generation;
    _deadline?.cancel();
    _deadline = null;
    final action = _action;
    _action = null;
    final events = _events;
    _events = null;
    searching = false;
    await events?.cancel();
    if (action != null && action.isReady && !action.isStopped) {
      try {
        await action.stop();
      } catch (_) {}
    }
    if (!_disposed) notifyListeners();
  }

  @override
  void dispose() {
    _disposed = true;
    unawaited(stop());
    super.dispose();
  }
}
