package com.gorillaescape.server.hosting;

import com.gorillaescape.server.GorillaEscapeApplication;
import java.io.*;
import java.nio.channels.*;
import java.nio.file.*;
import java.nio.file.attribute.PosixFilePermissions;
import java.security.*;
import java.security.cert.*;
import java.time.*;
import java.util.*;
import java.util.concurrent.TimeUnit;
import org.springframework.boot.SpringApplication;
import org.springframework.core.env.MapPropertySource;

/** DEC-016: local encrypted CA, IP leaf and ordinary TLS. Uses the existing JDK keytool. */
public final class LocalTrust {
    public record Material(Path store, Path passwordFile, X509Certificate root, X509Certificate leaf, String fingerprint) { }
    private static volatile Material current;
    private LocalTrust() { }
    public static Material current() { return current; }
    public static void configure(SpringApplication application, String directory) {
        if (directory == null || directory.isBlank()) return;
        try {
            if (System.getenv("GORILLA_MOBILE_CONFIG") != null) throw new IOException("TRUST_CONFIG_CONFLICT");
            var lan = LanInterface.select(System.getenv("GORILLA_LAN_INTERFACE"), System.getenv("GORILLA_LAN_ADDRESS"));
            int port = Integer.parseInt(System.getenv().getOrDefault("GORILLA_HTTPS_PORT", "8443"));
            if (port < 1024 || port > 65535) throw new IOException("HTTPS_PORT_INVALID");
            current = prepare(Path.of(directory), lan.address());
            Map<String,Object> values = new HashMap<>();
            values.put("server.address", lan.address()); values.put("server.port", port);
            values.put("server.ssl.enabled", true); values.put("server.ssl.key-store", current.store().toUri().toString());
            values.put("server.ssl.key-store-type", "PKCS12"); values.put("server.ssl.key-store-password", Files.readString(current.passwordFile()));
            values.put("server.ssl.enabled-protocols", "TLSv1.2,TLSv1.3");
            values.put("gorilla.mobile.public-origin", "https://"+lan.address()+":"+port);
            values.put("gorilla.trust.enabled", true); values.put("gorilla.mobile.rtc-enabled", true); values.put("gorilla.mobile.lab-enabled", false);
            values.put("gorilla.trust.bootstrap-port", System.getenv().getOrDefault("GORILLA_TRUST_BOOTSTRAP_PORT", "0"));
            application.addInitializers(c -> c.getEnvironment().getPropertySources().addFirst(new MapPropertySource("local-trust", values)));
            System.err.println("{\"component\":\"local-trust\",\"event\":\"READY\",\"caSha256\":\""+current.fingerprint()+"\",\"origin\":\"https://"+lan.address()+":"+port+"\"}");
        } catch (Exception error) { throw new IllegalArgumentException("LOCAL_TRUST_SETUP_FAILED: "+safeCode(error)); }
    }
    private static String safeCode(Exception error) {
        String m=error.getMessage();return m!=null && m.matches("[A-Z_]+") ? m : "CHECK_CONFIGURATION";
    }
    public static Material prepare(Path directory, String address) throws Exception {
        if (!directory.isAbsolute() || !MobileHttpsSettings.privateIpv4(address)) throw new IOException("TRUST_CONFIG_INVALID");
        Files.createDirectories(directory, PosixFilePermissions.asFileAttribute(PosixFilePermissions.fromString("rwx------")));
        Path dir=directory.toRealPath();
        for(Path parent=dir;parent!=null;parent=parent.getParent()) if(Files.exists(parent.resolve(".git"))) throw new IOException("TRUST_DIRECTORY_IN_REPOSITORY");
        secure(dir,true);
        Path lock=dir.resolve("trust.lock");if(Files.isSymbolicLink(lock))throw new IOException("TRUST_FILE_INVALID");
        try(var channel=FileChannel.open(lock,StandardOpenOption.CREATE,StandardOpenOption.WRITE,LinkOption.NOFOLLOW_LINKS)) {
            secure(lock,false);
            try(var held=channel.tryLock()) {
                if(held==null)throw new IOException("TRUST_BUSY");
                return locked(dir,address);
            }
        }
    }
    private static Material locked(Path dir,String address) throws Exception {
        Path root=dir.resolve("root.p12"),password=dir.resolve("password"),publicRoot=dir.resolve("root.cer"),leaf=dir.resolve("server.p12");
        boolean exists=Files.exists(root)||Files.exists(password)||Files.exists(publicRoot)||Files.exists(leaf);
        if(exists && (!Files.exists(root)||!Files.exists(password)||!Files.exists(publicRoot)))throw new IOException("TRUST_STATE_INCOMPLETE");
        Path temp=Files.createTempDirectory(dir,"work-",PosixFilePermissions.asFileAttribute(PosixFilePermissions.fromString("rwx------")));
        try {
            if(!exists) {
                byte[] random=new byte[32];new SecureRandom().nextBytes(random);
                Path pass=temp.resolve("password");Files.writeString(pass,Base64.getUrlEncoder().withoutPadding().encodeToString(random),StandardOpenOption.CREATE_NEW);secure(pass,false);
                Path ca=temp.resolve("root.p12"),cer=temp.resolve("root.cer");
                tool(temp,pass,"-genkeypair","-alias","root","-keystore",ca.toString(),"-storetype","PKCS12","-keyalg","RSA","-keysize","3072","-sigalg","SHA256withRSA","-dname","CN=Gorilla Escape Local CA "+UUID.randomUUID(),"-validity","1825","-ext","BC:critical=ca:true,pathlen:0","-ext","KU:critical=keyCertSign,cRLSign");
                tool(temp,pass,"-exportcert","-alias","root","-keystore",ca.toString(),"-file",cer.toString());
                move(ca,root);move(cer,publicRoot);move(pass,password);
            }
            for(Path f:List.of(root,password,publicRoot))check(f);
            String pass=Files.readString(password);if(!pass.matches("[A-Za-z0-9_-]{43}"))throw new IOException("TRUST_STATE_INVALID");
            KeyStore ca=load(root,pass);X509Certificate certificate=(X509Certificate)ca.getCertificate("root");
            if(certificate==null||certificate.getBasicConstraints()<0||!ca.isKeyEntry("root"))throw new IOException("TRUST_STATE_INVALID");
            certificate.checkValidity();certificate.verify(certificate.getPublicKey());
            if(!Arrays.equals(certificate.getEncoded(),Files.readAllBytes(publicRoot)))throw new IOException("TRUST_STATE_INVALID");
            X509Certificate server=null;
            if(Files.exists(leaf)) {
                check(leaf);server=(X509Certificate)load(leaf,pass).getCertificate("server");
                try { validateLeaf(server,certificate,address,Instant.now().plus(Duration.ofDays(1))); }
                catch(GeneralSecurityException e) { server=null; }
            }
            if(server==null) {
                Path store=temp.resolve("server.p12"),csr=temp.resolve("server.csr"),signed=temp.resolve("server.cer");
                tool(temp,password,"-genkeypair","-alias","server","-keystore",store.toString(),"-storetype","PKCS12","-keyalg","RSA","-keysize","2048","-sigalg","SHA256withRSA","-dname","CN=Gorilla Escape LAN","-validity","7","-ext","SAN=ip:"+address);
                tool(temp,password,"-certreq","-alias","server","-keystore",store.toString(),"-file",csr.toString());
                tool(temp,password,"-gencert","-alias","root","-keystore",root.toString(),"-infile",csr.toString(),"-outfile",signed.toString(),"-validity","7","-ext","SAN=ip:"+address,"-ext","BC:critical=ca:false","-ext","KU:critical=digitalSignature,keyEncipherment","-ext","EKU=serverAuth");
                tool(temp,password,"-importcert","-noprompt","-alias","root","-keystore",store.toString(),"-file",publicRoot.toString());
                tool(temp,password,"-importcert","-noprompt","-alias","server","-keystore",store.toString(),"-file",signed.toString());
                server=(X509Certificate)load(store,pass).getCertificate("server");validateLeaf(server,certificate,address,Instant.now());move(store,leaf);
            }
            return new Material(leaf,password,certificate,server,HexFormat.of().formatHex(MessageDigest.getInstance("SHA-256").digest(certificate.getEncoded())));
        } finally {
            try(var files=Files.list(temp)){for(Path f:files.toList())Files.deleteIfExists(f);}Files.deleteIfExists(temp);
        }
    }
    public static void validateLeaf(X509Certificate leaf,X509Certificate root,String address,Instant at) throws GeneralSecurityException {
        if(leaf==null)throw new CertificateException("LEAF_INVALID");
        root.checkValidity(Date.from(at));leaf.checkValidity(Date.from(at));leaf.verify(root.getPublicKey());
        if(leaf.getBasicConstraints()!=-1 || leaf.getSubjectAlternativeNames()==null
            || leaf.getSubjectAlternativeNames().stream().noneMatch(s -> Integer.valueOf(7).equals(s.get(0))&&address.equals(s.get(1)))
            || leaf.getExtendedKeyUsage()==null || !leaf.getExtendedKeyUsage().contains("1.3.6.1.5.5.7.3.1"))throw new CertificateException("LEAF_INVALID");
    }
    private static KeyStore load(Path path,String password)throws Exception {var k=KeyStore.getInstance("PKCS12");try(var in=Files.newInputStream(path)){k.load(in,password.toCharArray());}return k;}
    private static void tool(Path dir,Path password,String...args)throws Exception {
        Path binary=Path.of(System.getProperty("java.home"),"bin",System.getProperty("os.name").startsWith("Windows")?"keytool.exe":"keytool");
        if(!Files.isExecutable(binary))throw new IOException("KEYTOOL_REQUIRED");
        List<String> command=new ArrayList<>();command.add(binary.toString());command.addAll(List.of(args));command.addAll(List.of("-storepass:file",password.toString()));
        var parentBefore=GorillaEscapeApplication.managedParent();
        if(Thread.currentThread().isInterrupted()||parentBefore!=null&&!parentBefore.publishIfAlive(()->{}))throw new IOException("TRUST_GENERATION_CANCELLED");
        var process=new ProcessBuilder(command).directory(dir.toFile()).redirectOutput(ProcessBuilder.Redirect.DISCARD).redirectError(ProcessBuilder.Redirect.DISCARD).start();
        try {
            long end=System.nanoTime()+TimeUnit.SECONDS.toNanos(10);
            while(!process.waitFor(100,TimeUnit.MILLISECONDS)) {
                var parent=GorillaEscapeApplication.managedParent();
                if(Thread.currentThread().isInterrupted()||System.nanoTime()>end||parent!=null&&!parent.publishIfAlive(()->{}))throw new IOException("TRUST_GENERATION_CANCELLED");
            }
            if(process.exitValue()!=0)throw new IOException("KEYTOOL_FAILED");
        } finally {if(process.isAlive()){process.destroy();if(!process.waitFor(1,TimeUnit.SECONDS))process.destroyForcibly().waitFor(2,TimeUnit.SECONDS);}}
    }
    private static void move(Path from,Path to)throws IOException {secure(from,false);Files.move(from,to,StandardCopyOption.ATOMIC_MOVE,StandardCopyOption.REPLACE_EXISTING);}
    private static void check(Path f)throws IOException {if(Files.isSymbolicLink(f)||!Files.isRegularFile(f)||Files.size(f)>1024*1024)throw new IOException("TRUST_FILE_INVALID");secure(f,false);}
    private static void secure(Path f,boolean directory)throws IOException {
        if(!Files.getFileStore(f).supportsFileAttributeView("posix"))throw new IOException("TRUST_PERMISSIONS_UNSUPPORTED");
        var permissions=PosixFilePermissions.fromString(directory?"rwx------":"rw-------");
        if(Files.isSymbolicLink(f))throw new IOException("TRUST_FILE_INVALID");
        // Never quietly tighten existing shared files: this may indicate the wrong installation path.
        if(!Files.getPosixFilePermissions(f).equals(permissions)) {
            if(f.getFileName().toString().equals("trust.lock")||f.getParent().getFileName().toString().startsWith("work-"))Files.setPosixFilePermissions(f,permissions);
            else throw new IOException("TRUST_PERMISSIONS_INVALID");
        }
    }
}
