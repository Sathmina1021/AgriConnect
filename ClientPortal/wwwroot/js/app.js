const SERVICES = {
  farmer: "http://localhost:5001/api/farmer",
  supplier: "http://localhost:5002/api/supplier",
  officer: "http://localhost:5003/api/officer",
  admin: "http://localhost:5004/api/admin"
};

function getBadge(status) {
  const s = (status || "").toLowerCase();
  if (s.includes("approve") || s.includes("deliver") || s.includes("available") || s.includes("accept") || s.includes("running")) {
    return `<span class="status status-approved">${status}</span>`;
  }
  if (s.includes("reject") || s.includes("sold") || s.includes("offline")) {
    return `<span class="status status-rejected">${status}</span>`;
  }
  if (s.includes("confirm") || s.includes("quote")) {
    return `<span class="status status-confirmed">${status}</span>`;
  }
  return `<span class="status status-pending">${status}</span>`;
}
