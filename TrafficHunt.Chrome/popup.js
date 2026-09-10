async function buildCookieHeader() {
  const all = await chrome.cookies.getAll({});

  const cookies = all.filter(c =>
    c.domain === "youtube.com" ||
    c.domain === ".youtube.com" ||
    c.domain.endsWith(".youtube.com"));

  if (cookies.length === 0) {
    throw new Error("No youtube.com cookies found. Log in to YouTube first.");
  }

  const required = ["SID", "SAPISID", "__Secure-1PAPISID", "HSID", "SSID"];
  const names = new Set(cookies.map(c => c.name));
  const missing = required.filter(r => !names.has(r));
  if (missing.length === required.length) {
    throw new Error("You are not logged in — no SID/SAPISID cookies. Log in to youtube.com and retry.");
  }

  // Order doesn't matter for the server, but keep SID/SAPISID first for readability
  cookies.sort((a, b) => {
    const rank = n => (n === "SID" ? 0 : n.startsWith("SAPISID") || n.includes("PAPISID") ? 1 : 2);
    return rank(a.name) - rank(b.name);
  });

  return cookies.map(c => `${c.name}=${c.value}`).join("; ");
}

function setStatus(text, ok) {
  const el = document.getElementById("status");
  el.textContent = text;
  el.className = ok ? "ok" : "err";
}

document.getElementById("copy").addEventListener("click", async () => {
  try {
    const header = await buildCookieHeader();
    await navigator.clipboard.writeText(header);
    setStatus(`Copied ${header.length} chars — paste into Settings → YouTube session.`, true);
  } catch (e) {
    setStatus(e.message, false);
  }
});

document.getElementById("send").addEventListener("click", async () => {
  try {
    const header = await buildCookieHeader();
    const resp = await fetch("http://localhost:52956/settings/receivecookie", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ cookie: header })
    });
    if (!resp.ok) throw new Error(`TrafficHunt returned ${resp.status}`);
    setStatus("Sent! Saved to TrafficHunt settings.", true);
  } catch (e) {
    setStatus(e.message, false);
  }
});