package com.gorillaescape.server.connectivity;

import java.net.*;
import java.util.*;
import java.util.concurrent.*;
import javax.jmdns.*;

/** LAN-only DNS-SD announcement. Never advertises PINs, admissions or sensor input. */
public final class LocalDiscovery implements AutoCloseable {
    public static final String TYPE="_gorillaescape._tcp.local.";
    private final ExecutorService worker=Executors.newSingleThreadExecutor(r->{var t=new Thread(r,"lan-discovery");t.setDaemon(true);return t;});
    private final List<JmDNS> publishers=new ArrayList<>();
    private final int port;
    private final String bindAddress,name="Gorilla Escape "+UUID.randomUUID().toString().substring(0,8);
    private URI desired;private volatile boolean closed,active;private volatile long retryAfter;
    public LocalDiscovery(String bindAddress,int port){this.bindAddress=bindAddress;this.port=port;}
    public static Map<String,String> attributes(URI origin){
        if(origin==null||!"https".equals(origin.getScheme())||origin.getHost()==null||origin.getUserInfo()!=null||origin.getQuery()!=null||origin.getFragment()!=null||!(origin.getPath().isEmpty()||origin.getPath().equals("/")))throw new IllegalArgumentException("DISCOVERY_ORIGIN_INVALID");
        return Map.of("v","1","origin",origin.getScheme()+"://"+origin.getAuthority(),"game","gorilla-escape");
    }
    public synchronized void observe(URI origin){
        if(closed||Objects.equals(desired,origin)&&(origin==null||active||System.nanoTime()<retryAfter))return;
        if(origin!=null)attributes(origin);desired=origin;retryAfter=System.nanoTime()+30_000_000_000L;
        worker.execute(()->publish(origin));
    }
    private void publish(URI origin){
        shutdownPublishers();if(closed||origin==null)return;
        try{
            var addresses=new LinkedHashSet<InetAddress>();
            if(!"0.0.0.0".equals(bindAddress)&&!"::".equals(bindAddress))addresses.add(InetAddress.getByName(bindAddress));
            else for(var nic:Collections.list(NetworkInterface.getNetworkInterfaces()))if(nic.isUp()&&!nic.isLoopback()&&nic.supportsMulticast())for(var ip:Collections.list(nic.getInetAddresses()))if(ip instanceof Inet4Address&&ip.isSiteLocalAddress())addresses.add(ip);
            for(var ip:addresses){
                if(closed)break;
                if(!(ip instanceof Inet4Address)||!ip.isSiteLocalAddress()||ip.isLoopbackAddress())continue;
                JmDNS dns=JmDNS.create(ip);publishers.add(dns);
                dns.registerService(ServiceInfo.create(TYPE,name,port,0,0,attributes(origin)));
            }
            active=!publishers.isEmpty();
        }catch(Exception unavailable){shutdownPublishers();System.err.println("LAN_DISCOVERY_UNAVAILABLE");}
    }
    private void shutdownPublishers(){active=false;for(var dns:publishers)try{dns.close();}catch(Exception ignored){}publishers.clear();}
    @Override public synchronized void close(){if(closed)return;closed=true;worker.execute(this::shutdownPublishers);worker.shutdown();}
}
