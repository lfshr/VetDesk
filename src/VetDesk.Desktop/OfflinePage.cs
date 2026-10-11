using System.Net;
using Microsoft.Web.WebView2.Core;

namespace VetDesk.Desktop;

/// <summary>The page the window shows when the VetDesk server didn't answer, with a Retry button.</summary>
public static class OfflinePage
{
    public const string RetryMessage = "retry";

    public static string Html(ServerAddress server, CoreWebView2WebErrorStatus status)
    {
        var address = WebUtility.HtmlEncode(server.Uri.GetLeftPart(UriPartial.Authority));
        var (title, advice) = IsCertificateProblem(status)
            ? ("This PC doesn't trust the VetDesk server yet",
               "Install the server's root certificate on this PC, as the install guide describes, then retry.")
            : ("VetDesk can't reach the server",
               "Check that this PC is on the practice network and that the VetDesk server is switched on, then retry.");

        return $$"""
            <!DOCTYPE html>
            <html lang="en-GB">
            <head>
            <meta charset="utf-8" />
            <title>VetDesk</title>
            <style>
                body { margin: 0; height: 100vh; display: flex; align-items: center; justify-content: center;
                       background: #0E2A2A; font-family: "IBM Plex Sans", "Segoe UI", system-ui, sans-serif; color: #17201E; }
                .card { background: #FFFFFF; border-radius: 12px; padding: 28px 32px; max-width: 460px; }
                h1 { font-size: 18px; font-weight: 600; margin: 0 0 10px; }
                p { font-size: 14px; line-height: 1.5; color: #55615E; margin: 0 0 8px; }
                code { font-family: "IBM Plex Mono", Consolas, monospace; font-size: 13px; color: #17201E; }
                button { margin-top: 14px; font: inherit; font-size: 14px; font-weight: 600; color: #FFFFFF; background: #0B6E63;
                         border: 0; border-radius: 8px; padding: 9px 18px; cursor: pointer; }
                button:hover { background: #08524A; }
            </style>
            </head>
            <body>
            <div class="card">
                <h1>{{title}}</h1>
                <p>{{advice}}</p>
                <p>Server: <code>{{address}}</code></p>
                <button autofocus onclick="chrome.webview.postMessage('{{RetryMessage}}')">Retry</button>
            </div>
            </body>
            </html>
            """;
    }

    private static bool IsCertificateProblem(CoreWebView2WebErrorStatus status) => status is
        CoreWebView2WebErrorStatus.CertificateCommonNameIsIncorrect or
        CoreWebView2WebErrorStatus.CertificateExpired or
        CoreWebView2WebErrorStatus.ClientCertificateContainsErrors or
        CoreWebView2WebErrorStatus.CertificateRevoked or
        CoreWebView2WebErrorStatus.CertificateIsInvalid;
}
