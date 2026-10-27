using NUnit.Framework;
using GorillaEscape.Contracts;
using GorillaEscape.Input;
namespace GorillaEscape.Tests {
 public sealed class PhoneInputStoreTests {
  private static PhoneInputWire Sample(long seq=1,long epoch=1){return new PhoneInputWire{playerId=1,sequence=seq,connectionEpoch=epoch,sessionId="session",deviceSessionId="device",serverReceiveTimestamp=1000,quality=new PhoneQuality{source="synthetic",status="degraded"},acceleration=new PhoneValues{availability="partial",hasX=true,x=1}};}
  [Test] public void LatestFreshnessAndMissingAxis(){var s=new PhoneInputStore();Assert.IsTrue(s.Accept(Sample()));Assert.IsTrue(s.TryLatest(1,out var wire));Assert.IsTrue(wire.acceleration.hasX);Assert.IsFalse(wire.acceleration.hasY);Assert.AreEqual(20,s.AgeMilliseconds(1,1020));Assert.IsTrue(s.Fresh(1,1020,20));Assert.IsFalse(s.Fresh(1,1021,20));}
  [Test] public void DuplicateOldAndNewEpoch(){var s=new PhoneInputStore();Assert.IsTrue(s.Accept(Sample(5)));Assert.IsFalse(s.Accept(Sample(5)));Assert.IsFalse(s.Accept(Sample(4)));Assert.IsTrue(s.Accept(Sample(1,2)));Assert.IsFalse(s.Accept(Sample(9,1)));}
  [Test] public void RawPhoneInputPreservesPartialAndDisconnect(){var store=new PhoneInputStore();store.Accept(Sample());Assert.IsTrue(store.TryPhoneInput(1,out var input));Assert.IsFalse(input.HasAcceleration);Assert.IsFalse(input.HasOrientation);Assert.IsFalse(input.ProtocolSample.acceleration.hasY);var state=Sample();state.messageType="SERVER_STATE";state.connectionState="DISCONNECTED";Assert.IsTrue(store.Accept(state));Assert.IsTrue(store.TryPhoneInput(1,out input));Assert.IsFalse(input.IsConnected);Assert.AreEqual(1,input.Sequence);}
  [Test] public void FourPlayersAndClear(){var s=new PhoneInputStore();for(int p=1;p<=4;p++){var sample=Sample();sample.playerId=p;Assert.IsTrue(s.Accept(sample));}var bad=Sample();bad.playerId=5;Assert.IsFalse(s.Accept(bad));s.Clear();Assert.IsFalse(s.TryLatest(1,out _));}
 }
}
