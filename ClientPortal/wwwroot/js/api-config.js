const API = {
  FARMER: "http://localhost:5001/api/farmer",
  SUPPLIER: "http://localhost:5002/api/supplier",
  OFFICER: "http://localhost:5003/api/officer",
  ADMIN: "http://localhost:5004/api/admin"
};

function notify(msg) {
  const el = document.getElementById("toast");
  if (!el) return alert(msg);
  el.textContent = msg;
  el.style.display = "block";
  setTimeout(() => { el.style.display = "none"; }, 3400);
}

function statusBadge(status) {
  const s = (status || "").toLowerCase();
  if (s.includes("approve") || s.includes("deliver") || s.includes("available") || s.includes("verified") || s.includes("accept"))
    return `<span class="badge badge-approved">${status}</span>`;
  if (s.includes("reject") || s.includes("notselected") || s.includes("sold"))
    return `<span class="badge badge-rejected">${status}</span>`;
  if (s.includes("confirm") || s.includes("transit"))
    return `<span class="badge badge-confirmed">${status}</span>`;
  if (s.includes("quote"))
    return `<span class="badge badge-quoted">${status}</span>`;
  return `<span class="badge badge-pending">${status}</span>`;
}
