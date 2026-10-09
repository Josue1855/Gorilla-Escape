package com.gorillaescape.server.mobile;

import com.gorillaescape.server.hosting.LanInterface;
import java.net.URI;
import java.util.List;

/** Product QR origins are exact LAN IPv4 HTTPS endpoints, never a loopback fallback. */
public final class MobileOrigin {
    private MobileOrigin() { }
    public static URI page(String origin,String address,int port) {
        URI uri=URI.create(origin);
        LanInterface.choose(List.of(new LanInterface.Candidate("configured",address)),null,address);
        if(!"https".equals(uri.getScheme())||!address.equals(uri.getHost())||uri.getPort()!=port
            ||uri.getUserInfo()!=null||uri.getRawQuery()!=null||uri.getRawFragment()!=null
            ||uri.getRawPath()!=null&&!uri.getRawPath().isEmpty())throw new IllegalArgumentException("MOBILE_ORIGIN_INVALID");
        return uri.resolve("/mobile-lab/index.html");
    }
}
