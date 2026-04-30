using System;
using System.Net;

namespace FlowFlex.Domain.Shared.Helpers
{
    /// <summary>
    /// Helper for validating URLs to prevent SSRF (Server-Side Request Forgery) attacks
    /// </summary>
    public static class UrlValidationHelper
    {
        /// <summary>
        /// Validate that a URL is safe for server-side requests (not targeting internal networks)
        /// </summary>
        /// <param name="url">URL to validate</param>
        /// <returns>True if the URL is safe to request</returns>
        public static bool IsSafeUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return false;

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
                return false;

            // Only allow HTTP and HTTPS schemes
            if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
                return false;

            var host = uri.Host;

            // Block localhost
            if (string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(host, "127.0.0.1", StringComparison.Ordinal) ||
                string.Equals(host, "::1", StringComparison.Ordinal) ||
                string.Equals(host, "[::1]", StringComparison.Ordinal) ||
                string.Equals(host, "0.0.0.0", StringComparison.Ordinal))
                return false;

            // Block private IP ranges
            if (IPAddress.TryParse(host, out var ipAddress))
            {
                return !IsPrivateIpAddress(ipAddress);
            }

            // Block metadata endpoints (cloud provider instance metadata)
            if (string.Equals(host, "169.254.169.254", StringComparison.Ordinal) ||
                host.EndsWith(".internal", StringComparison.OrdinalIgnoreCase) ||
                host.EndsWith(".local", StringComparison.OrdinalIgnoreCase))
                return false;

            return true;
        }

        /// <summary>
        /// Check if an IP address is in a private/reserved range
        /// </summary>
        private static bool IsPrivateIpAddress(IPAddress ipAddress)
        {
            var bytes = ipAddress.GetAddressBytes();

            // IPv4 private ranges
            if (bytes.Length == 4)
            {
                // 10.0.0.0/8
                if (bytes[0] == 10) return true;
                // 172.16.0.0/12
                if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) return true;
                // 192.168.0.0/16
                if (bytes[0] == 192 && bytes[1] == 168) return true;
                // 169.254.0.0/16 (link-local / cloud metadata)
                if (bytes[0] == 169 && bytes[1] == 254) return true;
                // 127.0.0.0/8 (loopback)
                if (bytes[0] == 127) return true;
                // 0.0.0.0/8
                if (bytes[0] == 0) return true;
            }

            // IPv6 private ranges
            if (bytes.Length == 16)
            {
                // ::1 (loopback)
                if (IPAddress.IsLoopback(ipAddress)) return true;
                // fe80::/10 (link-local)
                if (bytes[0] == 0xFE && (bytes[1] & 0xC0) == 0x80) return true;
                // fc00::/7 (unique local)
                if ((bytes[0] & 0xFE) == 0xFC) return true;
            }

            return false;
        }
    }
}
