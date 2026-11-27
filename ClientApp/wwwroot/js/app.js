const SERVICES = {
  farmer: "/api/farmer",
  supplier: "/api/supplier",
  officer: "/api/officer",
  admin: "/api/admin",
  auth: "/api/auth",
  health: "/api/health",
  endpoints: "/api/endpoints"
};

function escHtml(str) {
  return String(str ?? "")
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;")
    .replace(/"/g, "&quot;")
    .replace(/'/g, "&#39;");
}

function escJs(str) {
  return String(str ?? "").replace(/\\/g, "\\\\").replace(/'/g, "\\'");
}

function getBadge(status) {
  const s = (status || "").toLowerCase();
  if (s.includes("approve") || s.includes("deliver") || s.includes("available") || s.includes("accept") || s.includes("running") || s.includes("healthy")) {
    return `<span class="status status-approved">${escHtml(status)}</span>`;
  }
  if (s.includes("reject") || s.includes("sold") || s.includes("offline") || s.includes("notselected")) {
    return `<span class="status status-rejected">${escHtml(status)}</span>`;
  }
  if (s.includes("confirm") || s.includes("quote") || s.includes("dispatch") || s.includes("transit")) {
    return `<span class="status status-confirmed">${escHtml(status)}</span>`;
  }
  return `<span class="status status-pending">${escHtml(status)}</span>`;
}

function roleHome(role) {
  switch (role) {
    case "Admin": return "admin.html";
    case "Farmer": return "farmer.html";
    case "Supplier": return "supplier.html";
    case "Officer": return "officer.html";
    default: return "login.html";
  }
}

async function apiFetch(url, options = {}) {
  const res = await fetch(url, options);
  const data = await res.json().catch(() => ({}));
  if (!res.ok) {
    throw new Error(data.message || `Request failed (${res.status})`);
  }
  return data;
}

function renderUserBadge(user) {
  const navRight = document.querySelector(".navbar > div:last-child");
  if (!navRight || document.getElementById("userBadge")) return;
  const span = document.createElement("span");
  span.id = "userBadge";
  span.style.marginLeft = "16px";
  span.innerHTML = ` &nbsp;|&nbsp; ${escHtml(user.fullName)} (${escHtml(user.role)}) &nbsp;
    <a href="#" onclick="logout();return false;">Logout</a>`;
  navRight.appendChild(span);
}

async function logout() {
  await fetch(`${SERVICES.auth}/logout`, { method: "POST" });
  location.href = "login.html";
}

async function requireRole(expectedRole) {
  try {
    const res = await fetch(`${SERVICES.auth}/me`);
    if (!res.ok) {
      const here = location.pathname.split("/").pop();
      location.href = `login.html?redirect=${encodeURIComponent(here)}`;
      return null;
    }
    const user = await res.json();
    if (user.role !== expectedRole && user.role !== "Admin") {
      location.href = roleHome(user.role);
      return null;
    }
    renderUserBadge(user);
    return user;
  } catch (e) {
    location.href = "login.html";
    return null;
  }
}
