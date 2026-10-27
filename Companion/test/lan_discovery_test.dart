import 'dart:async';

import 'package:flutter_test/flutter_test.dart';
import 'package:bonsoir_platform_interface/bonsoir_platform_interface.dart';
import 'package:gorilla_companion/control/lan_discovery.dart';

BonsoirService service({
  String origin = 'https://room.trycloudflare.com',
  String version = '1',
  String type = '_gorillaescape._tcp',
}) => BonsoirService(
  name: 'Gorilla Escape Test',
  type: type,
  port: 8443,
  attributes: {'v': version, 'game': 'gorilla-escape', 'origin': origin},
);

class FakeAction extends BonsoirAction<BonsoirDiscoveryEvent>
    with ServiceResolver {
  final events = StreamController<BonsoirDiscoveryEvent>.broadcast(sync: true);
  bool ready = false, stopped = false;
  int starts = 0, stops = 0, resolves = 0;
  @override
  Stream<BonsoirDiscoveryEvent> get eventStream => events.stream;
  @override
  bool get isReady => ready;
  @override
  bool get isStopped => stopped;
  @override
  Future<void> initialize() async {
    ready = true;
  }

  @override
  Future<void> start() async {
    starts++;
  }

  @override
  Future<void> stop() async {
    stopped = true;
    stops++;
  }

  @override
  void resolveService(BonsoirService s) {
    resolves++;
  }

  @override
  bool supportsMdnsHostname() => false;
}

void main() {
  test('native Bonjour trailing-dot service types are recognized', () {
    final record = BonsoirService.ignoreNorms(
      name: 'PC',
      type: '_gorillaescape._tcp.',
      port: 8443,
      attributes: {
        'v': '1',
        'game': 'gorilla-escape',
        'origin': 'https://room.trycloudflare.com',
      },
    );
    expect(NearbyPc.fromService(record), isNotNull);
  });
  test('announcements accept only the expected game, version and secure local/tunnel origin', () {
    expect(
      NearbyPc.fromService(service())?.origin.host,
      'room.trycloudflare.com',
    );
    expect(
      NearbyPc.fromService(service(origin: 'https://192.168.100.15:8443'))
          ?.origin
          .port,
      8443,
    );
    for (final s in [
      service(version: '2'),
      service(type: '_http._tcp'),
      service(origin: 'http://192.168.1.2'),
      service(origin: 'https://evil.example'),
      service(origin: 'https://user:pass@room.trycloudflare.com'),
      service(origin: 'https://room.trycloudflare.com/#secret'),
    ]) {
      expect(NearbyPc.fromService(s), isNull);
    }
  });
  test('resolves found services, updates rooms, removes lost PCs and stops on exit', () async {
    final action = FakeAction();
    final discovery = LanDiscovery(createAction: () => action);
    await discovery.start();
    expect(action.starts, 1);
    action.events.add(BonsoirDiscoveryServiceFoundEvent(service: service()));
    expect(action.resolves, 1);
    action.events.add(BonsoirDiscoveryServiceResolvedEvent(service: service()));
    expect(discovery.rooms.length, 1);
    action.events.add(
      BonsoirDiscoveryServiceUpdatedEvent(
        service: service(origin: 'https://next.trycloudflare.com'),
      ),
    );
    expect(discovery.rooms.values.single.origin.host, 'next.trycloudflare.com');
    action.events.add(BonsoirDiscoveryServiceLostEvent(service: service()));
    expect(discovery.rooms, isEmpty);
    await discovery.stop();
    expect(action.stops, 1);
    expect(discovery.searching, false);
    action.events.add(BonsoirDiscoveryServiceResolvedEvent(service: service()));
    expect(discovery.rooms, isEmpty);
    discovery.dispose();
    await action.events.close();
  });
  test(
    'permission or platform failure leaves QR available without retry loops',
    () async {
      var calls = 0;
      final discovery = LanDiscovery(
        createAction: () {
          calls++;
          throw StateError('denied');
        },
      );
      await discovery.start();
      expect(discovery.searching, false);
      expect(discovery.unavailable, true);
      expect(calls, 1);
      discovery.dispose();
    },
  );
}
