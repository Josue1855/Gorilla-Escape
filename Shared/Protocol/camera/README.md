# CameraInput v1 — PC camera / Phase0 laboratory

Implemented opt-in local adapter; not a mobile protocol and not a minigame input detector.
[Validation and limits](../../../docs/PHASE0_CAMERA_FUSION_VALIDATION.md).

## Contract

`CameraInputWire.cs` in the existing engine-independent Contracts package is the only C# wire DTO.
JSON line `CAMERA_FRAME {…}` over private child stdout; no socket/listener, no Java camera backend.
Max 16384 characters per line; fixed subject limit 2 and landmark limit 8; finite values, named landmarks,
unique subject IDs and unique names, known source/state/clock, ordered capture/process/receive times.
The adapter is trusted local code. JsonUtility is not promoted to a hostile/general JSON parser.
Unity additionally rejects duplicate/out-of-order frameSequence. Restart requires a new/reset store.

| Field | Semantics |
|---|---|
| version | 1 |
| frameSequence | positive increasing per child launch; never reusable as human identity |
| captureTimestamp | UTC milliseconds on PC; physical `read-complete` is a capture proxy, exposure timestamp unknown |
| processTimestamp | PC UTC after inference; fixture producer time for replay |
| unityReceiveTimestamp | overwritten by Unity at pipe receive, never authoritative from adapter |
| clockDomain / captureTimeSource | pc-unix-ms / read-complete or fixture |
| source | physical-webcam, replay or synthetic; never inferred from confidence |
| quality | observed or fixture |
| trackingState | TRACKING if subjects present, LOST if none; TRACKING does not mean reliable identity |
| subjects | 0–2 ephemeral spatial subjects, no biometrics or persistent person identifier |
| subjectId, centerX/Y, ambiguous | transient track, shoulder centre in image coordinates, ambiguity explicit |
| confidence | minimum presence/visibility of six arm landmarks; not gesture accuracy or calibrated probability |
| landmarks | left/right shoulder/elbow/wrist; optional left/right hand proxies |

Unmirrored OpenCV frame → BGR-to-RGB only → MediaPipe normalized image x right, y down, z relative to hips,
approximately on x scale, **not metres**. Raw x/y/z retained; points may be outside image. Visibility/presence
remain separate. `visible` threshold .5 is laboratory display/data availability, not sporting eligibility.
Hands here are index-knuckle pose landmarks 19/20, **not a full hand detector or anatomical wrist rotation**.
No quaternion, world-coordinate conversion, inferred missing point, face landmark, image, or video is emitted.
The raw detector may calculate 33 points internally; only the eight needed points leave the adapter.
Unity `CameraInput` holds player association + raw frame/subject, no Throw/Smash gesture fields.

## Fixture corpus

`corpus-v1.json`: twelve camera cases; 21 fusion scenarios with controlled timestamps/expected eligibility;
eight conceptual traces containing ordered phone/pose values. Replay rebases relative fixture frame delivery
to PC UTC; unit tests use explicitly controlled clocks. Labels such as accidental jerk are **hypotheses**,
not classified gestures or validated recognizers. No human clips stored.

## Ownership / bounds

Unity owns the child, CameraInputStore, explicit turn/READY association, freshness/alignment and fusion.
Java and the mobile/IPC contract are unchanged. Four phones can be live, **one camera-active player per turn**.
Buffers: 16 camera frames; 32 motion samples per player, four players; one pending adapter frame;
no image queue, synchronous inference/writer, at most one EOF-reader daemon, bounded metric history.
Device read/native inference may block; parent waits 3 s after EOF, then terminates only its own child and
records forced cleanup, with total 5 s bound. No process tree kill, watchdog or automatic restart.

Phone timestamps for alignment are **Java PC receipt timestamps**, not unsynchronized client wall time.
This validates receipt-to-camera proxy infrastructure; true acquisition-time clock sync/drift/uncertainty
and camera exposure offset remain physical debt. Both clocks are PC UTC; backwards wall-clock steps
are not silently normalized into measured motion. Future tolerance configurable.
EOF is normal shutdown. Errors in camera do not stop Java/phones; phone disconnect does not stop camera.
Native capture/inference can block: a host crash during such a block has no watchdog guarantee in this lab.
Physical clock synchronization, human occlusion/identity continuity and Windows/product packaging remain deferred.
No global memory-leak, gesture-accuracy, 30 FPS or physical-latency claim follows from this gate.
