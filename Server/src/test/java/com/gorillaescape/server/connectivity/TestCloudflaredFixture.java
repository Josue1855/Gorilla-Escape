package com.gorillaescape.server.connectivity;

import java.nio.file.*;
import java.util.*;

final class TestCloudflaredFixture {
    private TestCloudflaredFixture(){}

    static Path create(Path directory,String scenario)throws Exception{
        boolean windows=System.getProperty("os.name","").toLowerCase(Locale.ROOT).contains("win");
        Path binary=directory.resolve("cloudflared-fixture-"+UUID.randomUUID()+(windows?".cmd":""));
        if(!windows){
            String prefix=scenario.equals("HANG_VERSION")?"#!/bin/sh\nexec sleep 20\n":"#!/bin/sh\nif [ \"$1\" = '--version' ]; then echo 'cloudflared version 2026.10.0'; exit 0; fi\n";
            Files.writeString(binary,prefix+(scenario.equals("HANG_VERSION")?"":scenario));
            binary.toFile().setExecutable(true);return binary;
        }
        var lines=new ArrayList<String>();lines.add("@echo off");
        if(scenario.equals("HANG_VERSION")){lines.add("ping -n 21 127.0.0.1 >nul");}
        else {
            lines.add("if \"%~1\"==\"--version\" (echo cloudflared version 2026.10.0& exit /b 0)");
            if(scenario.contains("exit 1")){lines.add("exit /b 1");}
            else if(scenario.contains("trycloudflare.com.evil.org")){lines.add("echo https://qa-test.trycloudflare.com.evil.org");lines.add("echo Registered tunnel connection");lines.add("ping -n 21 127.0.0.1 >nul");}
            else if(scenario.contains("should-never-exist")){lines.add("echo https://qa-test.trycloudflare.com;malformed");lines.add("echo Registered tunnel connection");lines.add("ping -n 21 127.0.0.1 >nul");}
            else {
                var url=java.util.regex.Pattern.compile("https://[a-z-]+\\.trycloudflare\\.com").matcher(scenario);if(url.find())lines.add("echo "+url.group());
                var registrations=java.util.regex.Pattern.compile("(?:Registered|Unregistered) tunnel connection(?: connIndex=[0-9]+)?").matcher(scenario);while(registrations.find())lines.add("echo "+registrations.group());
                if(scenario.contains("sleep 0.1"))lines.add("powershell.exe -NoProfile -Command \"Start-Sleep -Milliseconds 200\"");else lines.add("ping -n 21 127.0.0.1 >nul");
            }
        }
        Files.writeString(binary,String.join("\r\n",lines)+"\r\n");return binary;
    }
}
