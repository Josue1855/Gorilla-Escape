package com.gorillaescape.server.connectivity;

import java.io.*;
import java.nio.file.*;
import java.util.*;
import java.util.concurrent.*;

/** Background closed-operation helper. Only pkexec's helper child is elevated. */
public final class FirewallPreflight implements AutoCloseable {
    @FunctionalInterface interface Runner {String run(boolean install, boolean recovery)throws Exception;}
    private final ExecutorService worker=Executors.newSingleThreadExecutor(r->{var t=new Thread(r,"firewall-preflight");t.setDaemon(true);return t;});
    private final Runner runner;private final String address;private final Path helper;private final boolean lab;
    private volatile String state="CHECKING";private volatile boolean closed,recovery;
    private volatile Process command,lease;
    private volatile long nextCheck;
    private Future<?> pending;private boolean recheck;
    public FirewallPreflight(boolean lab,String address,String packageRoot){
        this.lab=lab;this.address=address;helper=packageRoot==null?null:Path.of(packageRoot,"tools","setup-firewall.sh").toAbsolutePath();
        runner=this::execute;
        boolean windows=System.getProperty("os.name","").toLowerCase(Locale.ROOT).contains("win");
        if(lab||windows)state="NOT_REQUIRED";else check();
    }
    FirewallPreflight(Runner runner){this.runner=runner;address="";helper=null;lab=false;check();}
    public String state(){return state;}
    public boolean ready(){return Set.of("READY","DISABLED","NOT_INSTALLED","NOT_REQUIRED").contains(state);}
    public synchronized void poll(){if(!closed&&pending!=null&&pending.isDone()&&!state.equals("NOT_REQUIRED")&&!state.equals("CANCELLED")&&System.nanoTime()>=nextCheck)submit(false);}
    public synchronized void check(){if(closed||lab||System.getProperty("os.name","").toLowerCase(Locale.ROOT).contains("win"))return;state="CHECKING";if(pending!=null&&!pending.isDone()){recheck=true;return;}submit(false);}
    public synchronized void install(boolean forRecovery){
        if(closed||lab||pending!=null&&!pending.isDone())return;
        recovery=forRecovery;state="AUTHORIZING";submit(true);
    }
    private void submit(boolean install){
        boolean requestedRecovery=recovery;
        pending=worker.submit(()->{
            try{String result=runner.run(install,requestedRecovery);if(!closed&&requestedRecovery==recovery)state=result;}
            catch(Exception failure){if(!closed)state=install?"CANCELLED":"FAILED";}
            finally{synchronized(this){nextCheck=System.nanoTime()+TimeUnit.SECONDS.toNanos(5);if(recheck&&!closed){recheck=false;submit(false);}}}
        });
    }
    private String execute(boolean install,boolean forRecovery)throws Exception{
        if(helper==null||!Files.isRegularFile(helper)||!Files.isExecutable(helper))return "HELPER_UNAVAILABLE";
        var args=new ArrayList<String>();if(install)args.add("/usr/bin/pkexec");
        args.add(helper.toString());args.add(install?"--install":"--check");args.add("--address");args.add(address);
        if(forRecovery)args.add("--recovery");
        var builder=new ProcessBuilder(args).redirectError(ProcessBuilder.Redirect.DISCARD);
        builder.environment().keySet().removeIf(k->k.startsWith("GORILLA_")||k.equals("PYTHONPATH")||k.equals("PYTHONHOME")||k.startsWith("LD_"));
        var child=builder.start();command=child;
        try{
            if(install&&forRecovery){
                // A bounded, root-only recovery lease removes its own rule on pipe EOF.
                var answer=CompletableFuture.supplyAsync(()->{try{return new BufferedReader(new InputStreamReader(child.getInputStream())).readLine();}catch(IOException e){return null;}});
                String line=answer.get(90,TimeUnit.SECONDS);
                if(line==null)return "CANCELLED";
                String result=parse(line);
                if(result.equals("READY")){lease=child;return result;}
                return result;
            }
            if(!child.waitFor(install?90:8,TimeUnit.SECONDS))throw new IOException("FIREWALL_TIMEOUT");
            if(child.exitValue()!=0)return install?"CANCELLED":"FAILED";
            return parse(new String(child.getInputStream().readNBytes(4097),java.nio.charset.StandardCharsets.UTF_8));
        }finally{command=null;if(lease!=child&&child.isAlive()){child.getOutputStream().close();child.destroy();if(!child.waitFor(500,TimeUnit.MILLISECONDS))child.destroyForcibly();}}
    }
    private static String parse(String line)throws IOException{
        if(line==null||line.length()>4096)throw new IOException("FIREWALL_RESULT");
        var result=com.gorillaescape.server.protocol.GorillaProtocol.JSON.readTree(line).get("state");
        if(result==null||!Set.of("READY","DISABLED","NOT_INSTALLED","NEEDS_PERMISSION","INSPECTION_REQUIRED","FAILED").contains(result.asString()))throw new IOException("FIREWALL_RESULT");
        return result.asString();
    }
    public synchronized void requireRecovery(){recovery=true;nextCheck=0;check();}
    public synchronized void endRecovery(){
        recovery=false;var child=lease;lease=null;
        if(child!=null)try{child.getOutputStream().close();}catch(IOException ignored){}
        nextCheck=0;
    }
    @Override public synchronized void close(){closed=true;endRecovery();worker.shutdownNow();var child=command;if(child!=null){try{child.getOutputStream().close();}catch(IOException ignored){}child.destroy();}}
}
