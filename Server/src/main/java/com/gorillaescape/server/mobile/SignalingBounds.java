package com.gorillaescape.server.mobile;
import jakarta.servlet.*;import jakarta.servlet.http.*;import java.io.IOException;
import org.springframework.stereotype.Component;import org.springframework.boot.autoconfigure.condition.ConditionalOnProperty;
import org.springframework.web.filter.OncePerRequestFilter;
/** Known-length signaling bodies only. Reject before MVC allocates or deserializes payload. */
@Component
@ConditionalOnProperty(name="gorilla.mobile.rtc-enabled",havingValue="true")
public final class SignalingBounds extends OncePerRequestFilter {
 @Override protected void doFilterInternal(HttpServletRequest request,HttpServletResponse response,FilterChain chain)throws ServletException,IOException{
  if(request.getRequestURI().startsWith("/mobile/")){
   response.setHeader("Cache-Control","no-store");response.setHeader("Referrer-Policy","no-referrer");
   if(request.getContentLengthLong()>90000||request.getHeader("Transfer-Encoding")!=null){response.setStatus(413);return;}
  }
  chain.doFilter(request,response);
 }
}
