using System;
using System.IO;
using GorillaEscape.QA;
using GorillaEscape.Contracts;
using NUnit.Framework;
using UnityEngine;
public class LiveQaTests {
 [TestCase(null,"EMPTY")][TestCase("CONNECTED","CONNECTING")][TestCase("ACTIVE","ACTIVE")][TestCase("DISCONNECTED","DISCONNECTED")]
 public void Slots(string state,string expected){Assert.AreEqual(expected,LiveQaModel.Slot(state==null?null:new LobbyPlayerWire{connectionState=state}));}
 [Test]public void AuthoritativeNetwork(){Assert.AreEqual("CONNECTED",LiveQaModel.Slot(new LobbyPlayerWire{connectionState="CONNECTED",networkReady=true}));}
 [Test]public void UnknownAndActualZero(){Assert.AreEqual("NOT MEASURED",LiveQaModel.Triple(null));Assert.AreEqual("0.00",LiveQaModel.Value(true,0));Assert.AreEqual("NOT MEASURED",LiveQaModel.Value(false,10));}
 [Test]public void SensorsAndVisibleMapping(){var p=new PhoneInputWire{acceleration=new PhoneValues{hasX=true,hasY=true,x=40,y=-10}};Assert.AreEqual(new Vector2(1,-.5f),LiveQaModel.Response(p));Assert.That(LiveQaModel.Triple(p.acceleration),Does.Contain("40.00"));Assert.AreEqual(Vector2.zero,LiveQaModel.Response(null));}
 [Test]public void LetterboxCoordinates(){var r=LiveQaModel.Letterbox(new Rect(0,0,800,600),640,360);Assert.AreEqual(new Rect(0,75,800,450),r);Assert.AreEqual(new Vector2(400,300),LiveQaModel.Point(r,new CameraLandmark{x=.5f,y=.5f}));}
 [Test]public void ResetAndDeviceClassification(){var m=new LiveQaModel();m.DeviceState="Unprepared";m.Start(100);m.Stop(200);Assert.AreEqual(200,m.EndMs);m.Reset();Assert.AreEqual(0,m.StartMs);Assert.False(m.Measuring);Assert.AreEqual("NOT CONFIRMED",m.DeviceState);Assert.AreEqual("NOT VERIFIED",LiveQaModel.Classification("physical"));Assert.AreEqual("REPLAY",LiveQaModel.Classification("replay"));}
 [Test]public void PreviewBoundsAndDisabled(){Assert.Throws<InvalidDataException>(()=>CameraPreviewClient.Length(new byte[]{0,1,0,1}));Assert.False(CameraPreviewClient.ValidJpeg(new byte[]{255,216,255,217}));var previous=Environment.GetEnvironmentVariable("GORILLA_QA_OVERLAY");try{Environment.SetEnvironmentVariable("GORILLA_QA_OVERLAY",null);using(var p=new CameraPreviewClient())Assert.Throws<InvalidOperationException>(()=>p.Start(1234));}finally{Environment.SetEnvironmentVariable("GORILLA_QA_OVERLAY",previous);}}
 [Test]public void QrRejectsOversizedOrTruncatedModules(){Assert.Throws<InvalidDataException>(()=>QaRuntimeService.DecodeQr(new QaAdmissionWire{size=90}));Assert.Throws<InvalidDataException>(()=>QaRuntimeService.DecodeQr(new QaAdmissionWire{size=21,modules="AA=="}));}
 [Test]public void ReadinessUsesAuthoritativeLobbyEvenWithPartialAxes(){Assert.AreEqual("INPUT_READY",LiveQaModel.Quality(new PhoneInputWire{quality=new PhoneQuality{status="degraded"}},new LobbyPlayerWire{inputReady=true,networkReady=true}));}
 [Test]public void DisconnectedOverridesPreviousReady(){Assert.AreEqual("DISCONNECTED",LiveQaModel.Quality(new PhoneInputWire{quality=new PhoneQuality{status="available"}},new LobbyPlayerWire{connectionState="DISCONNECTED",inputReady=true}));}
 [Test]public void MissingReadinessIsExplicit(){Assert.AreEqual("NO_SENSOR_INPUT",LiveQaModel.Quality(null,null));}
 [TestCase("ACTIVE",true,true)][TestCase("SUSPENDED",true,false)][TestCase("CONNECTED",true,false)][TestCase("DISCONNECTED",true,false)][TestCase("ACTIVE",false,false)]
 public void VisibleResponseRequiresLiveAcceptedMovement(string state,bool fresh,bool expected){Assert.AreEqual(expected,LiveQaModel.HasFreshResponse(new PhoneInputWire{connectionState=state,acceleration=new PhoneValues{hasX=true,hasY=true}},fresh));}
}
