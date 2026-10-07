package com.gorillaescape.server.hosting;

import java.io.IOException;
import java.util.UUID;
import org.springframework.stereotype.Component;
import org.springframework.web.servlet.HandlerInterceptor;
import org.springframework.web.servlet.config.annotation.InterceptorRegistry;
import org.springframework.web.servlet.config.annotation.WebMvcConfigurer;
import jakarta.servlet.http.HttpServletRequest;
import jakarta.servlet.http.HttpServletResponse;

/** A launch identity is diagnostic only. It is never a session or credential. */
@Component
public final class HostingDiagnostics implements WebMvcConfigurer, HandlerInterceptor {
    private final String instance = UUID.randomUUID().toString();
    private final java.util.concurrent.atomic.AtomicLong healthRequests = new java.util.concurrent.atomic.AtomicLong();
    public String instanceId() { return instance; }
    public long confirmHealth() { return healthRequests.incrementAndGet(); }
    @Override public void addInterceptors(InterceptorRegistry registry) { registry.addInterceptor(this); }
    @Override public boolean preHandle(HttpServletRequest request, HttpServletResponse response, Object handler) throws IOException {
        if (request.getRequestURI().startsWith("/api/")) response.setHeader("Cache-Control", "no-store");
        response.setHeader("X-Content-Type-Options", "nosniff");
        response.setHeader("Referrer-Policy", "no-referrer");
        return true;
    }
}
