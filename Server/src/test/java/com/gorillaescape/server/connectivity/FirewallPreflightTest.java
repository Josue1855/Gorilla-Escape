package com.gorillaescape.server.connectivity;
import org.junit.jupiter.api.Test;
import java.util.concurrent.*;
import java.util.concurrent.atomic.*;
import static org.junit.jupiter.api.Assertions.*;
class FirewallPreflightTest {
    static void await(FirewallPreflight f,String state)throws Exception{long end=System.nanoTime()+TimeUnit.SECONDS.toNanos(2);while(!f.state().equals(state)&&System.nanoTime()<end)Thread.sleep(5);assertEquals(state,f.state());}
    @Test void explicitLabNeverInvokesPhysicalCheckOrElevation(){
        try(var f=new FirewallPreflight(true,"127.0.0.1","/missing-test-package")){
            f.check();f.install(false);f.requireRecovery();f.install(true);f.poll();
            assertEquals("NOT_REQUIRED",f.state());assertTrue(f.ready());
            f.endRecovery();assertEquals("NOT_REQUIRED",f.state());
        }
    }
    @Test void checkNeverElevatesAndExistingRulesNeedNoInstall()throws Exception{
        var elevated=new AtomicInteger();try(var f=new FirewallPreflight((install,recovery)->{if(install)elevated.incrementAndGet();return "READY";})){
            await(f,"READY");assertTrue(f.ready());f.check();await(f,"READY");assertEquals(0,elevated.get());
        }
    }
    @Test void missingRulesRequireExplicitActionAndCancellationIsRecoverable()throws Exception{
        var attempts=new AtomicInteger();try(var f=new FirewallPreflight((install,recovery)->!install?"NEEDS_PERMISSION":attempts.incrementAndGet()==1?"CANCELLED":"READY")){
            await(f,"NEEDS_PERMISSION");assertFalse(f.ready());assertEquals(0,attempts.get());f.install(false);await(f,"CANCELLED");assertFalse(f.ready());f.install(false);await(f,"READY");assertTrue(f.ready());assertEquals(2,attempts.get());
        }
    }
    @Test void failedNetworkCheckCannotAdvertiseRtcReadiness()throws Exception{
        try(var f=new FirewallPreflight((install,recovery)->"FAILED")){await(f,"FAILED");assertFalse(f.ready());}
    }
    @Test void recoveryRequiresItsOwnCheck()throws Exception{
        var checked=new AtomicBoolean();try(var f=new FirewallPreflight((install,recovery)->{if(recovery)checked.set(true);return recovery?"NEEDS_PERMISSION":"READY";})){
            await(f,"READY");Thread.sleep(20);f.requireRecovery();await(f,"NEEDS_PERMISSION");assertTrue(checked.get());assertFalse(f.ready());
        }
    }
}
