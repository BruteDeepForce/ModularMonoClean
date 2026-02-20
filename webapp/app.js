const state = {
  apiMode: false,
  accessToken: null,
  refreshToken: null,
  currentTenantId: null,
  tenants: JSON.parse(localStorage.getItem("mock.tenants") || "[]"),
  branches: JSON.parse(localStorage.getItem("mock.branches") || "[]"),
  users: JSON.parse(localStorage.getItem("mock.users") || "[]")
};

const API_BASE = "http://localhost:5000/api";

const el = {
  authScreen: document.getElementById("authScreen"),
  appShell: document.getElementById("appShell"),
  loginForm: document.getElementById("loginForm"),
  authStatus: document.getElementById("authStatus"),
  apiMode: document.getElementById("apiMode"),
  loginEmail: document.getElementById("loginEmail"),
  loginPassword: document.getElementById("loginPassword"),
  clock: document.getElementById("clock"),
  dateLabel: document.getElementById("dateLabel"),
  activeTenantLabel: document.getElementById("activeTenantLabel"),
  activeBranchLabel: document.getElementById("activeBranchLabel"),
  sessionInfo: document.getElementById("sessionInfo"),
  logoutBtn: document.getElementById("logoutBtn"),
  tenantList: document.getElementById("tenantList"),
  branchList: document.getElementById("branchList"),
  userList: document.getElementById("userList"),
  tenantDialog: document.getElementById("tenantDialog"),
  userDialog: document.getElementById("userDialog"),
  openTenantPanel: document.getElementById("openTenantPanel"),
  openUserPanel: document.getElementById("openUserPanel"),
  tenantForm: document.getElementById("tenantForm"),
  userForm: document.getElementById("userForm"),
  tenantName: document.getElementById("tenantName"),
  branchName: document.getElementById("branchName"),
  branchCode: document.getElementById("branchCode"),
  userFullName: document.getElementById("userFullName"),
  userPhone: document.getElementById("userPhone"),
  userEmail: document.getElementById("userEmail"),
  userRole: document.getElementById("userRole"),
  userBranchSelect: document.getElementById("userBranchSelect"),
  userPin: document.getElementById("userPin"),
  refreshTenants: document.getElementById("refreshTenants"),
  refreshBranches: document.getElementById("refreshBranches"),
  refreshUsers: document.getElementById("refreshUsers")
};

function uid() {
  return crypto.randomUUID ? crypto.randomUUID() : `${Date.now()}-${Math.random().toString(16).slice(2)}`;
}

function persistMock() {
  localStorage.setItem("mock.tenants", JSON.stringify(state.tenants));
  localStorage.setItem("mock.branches", JSON.stringify(state.branches));
  localStorage.setItem("mock.users", JSON.stringify(state.users));
}

function setStatus(text, kind = "info") {
  el.authStatus.textContent = text;
  el.authStatus.style.background = kind === "error" ? "#fff1f1" : kind === "ok" ? "#e7fbf4" : "#edf2ff";
  el.authStatus.style.color = kind === "error" ? "#a11f2c" : kind === "ok" ? "#0a7b5d" : "#2a3a8f";
}

function initClock() {
  const tick = () => {
    const now = new Date();
    el.clock.textContent = now.toLocaleTimeString("tr-TR", { hour12: false });
    el.dateLabel.textContent = now.toLocaleDateString("tr-TR", { day: "2-digit", month: "long", year: "numeric" });
  };
  tick();
  setInterval(tick, 1000);
}

async function apiFetch(path, options = {}) {
  const headers = { "Content-Type": "application/json", ...(options.headers || {}) };
  if (state.accessToken) {
    headers.Authorization = `Bearer ${state.accessToken}`;
  }
  const res = await fetch(`${API_BASE}${path}`, { ...options, headers });
  if (!res.ok) {
    const text = await res.text();
    throw new Error(text || `HTTP ${res.status}`);
  }
  return res.json();
}

async function login(email, password) {
  if (!state.apiMode) {
    state.accessToken = "mock-token";
    state.refreshToken = "mock-refresh";
    return {
      id: uid(),
      email,
      branchId: state.branches[0]?.id || null,
      roles: ["manager"]
    };
  }

  const data = await apiFetch("/auth/login", {
    method: "POST",
    body: JSON.stringify({ email, password })
  });

  state.accessToken = data.accessToken;
  state.refreshToken = data.refreshToken;

  return {
    id: data.id,
    email: data.email,
    branchId: data.branchId,
    roles: data.roles || []
  };
}

function renderTenantList() {
  el.tenantList.innerHTML = "";
  if (!state.tenants.length) {
    el.tenantList.innerHTML = "<li>Tenant bulunamadi</li>";
    return;
  }

  for (const t of state.tenants) {
    const li = document.createElement("li");
    li.innerHTML = `<span>${t.name}</span><span class="badge">${t.branchCount} sube</span>`;
    li.onclick = () => {
      state.currentTenantId = t.id;
      renderBranchList();
      renderUserList();
      el.activeTenantLabel.textContent = `Tenant: ${t.name}`;
    };
    el.tenantList.appendChild(li);
  }
}

function renderBranchList() {
  el.branchList.innerHTML = "";
  const list = state.currentTenantId ? state.branches.filter(b => b.tenantId === state.currentTenantId) : state.branches;
  if (!list.length) {
    el.branchList.innerHTML = "<li>Sube bulunamadi</li>";
    return;
  }

  for (const b of list) {
    const li = document.createElement("li");
    li.innerHTML = `<span>${b.name} <small>(${b.code})</small></span><span class="badge">${b.code}</span>`;
    li.onclick = () => {
      el.activeBranchLabel.textContent = b.name.toUpperCase();
      const tenant = state.tenants.find(t => t.id === b.tenantId);
      el.activeTenantLabel.textContent = `Tenant: ${tenant?.name || "-"}`;
    };
    el.branchList.appendChild(li);
  }
}

function renderUserList() {
  el.userList.innerHTML = "";
  const list = state.currentTenantId
    ? state.users.filter(u => state.branches.some(b => b.id === u.branchId && b.tenantId === state.currentTenantId))
    : state.users;

  if (!list.length) {
    el.userList.innerHTML = "<li>Kullanici bulunamadi</li>";
    return;
  }

  for (const u of list) {
    const li = document.createElement("li");
    li.innerHTML = `<span>${u.fullName} <small>${u.phone || ""}</small></span><span class="badge">${u.role}</span>`;
    el.userList.appendChild(li);
  }
}

function renderBranchSelect() {
  el.userBranchSelect.innerHTML = "";
  const list = state.currentTenantId ? state.branches.filter(b => b.tenantId === state.currentTenantId) : state.branches;
  for (const b of list) {
    const option = document.createElement("option");
    option.value = b.id;
    option.textContent = `${b.name} (${b.code})`;
    el.userBranchSelect.appendChild(option);
  }
}

function openApp(session) {
  el.authScreen.classList.add("hidden");
  el.appShell.classList.remove("hidden");
  el.sessionInfo.textContent = `Aktif kullanici: ${session.email} | Roller: ${(session.roles || []).join(", ") || "-"}`;
  renderTenantList();
  renderBranchList();
  renderUserList();
  renderBranchSelect();
}

function logout() {
  state.accessToken = null;
  state.refreshToken = null;
  el.appShell.classList.add("hidden");
  el.authScreen.classList.remove("hidden");
  setStatus("Cikis yapildi", "ok");
}

async function createTenantAndBranch(name, branchName, code) {
  const tenant = { id: uid(), name, branchCount: 1 };
  const branch = { id: uid(), tenantId: tenant.id, name: branchName, code };

  state.tenants.push(tenant);
  state.branches.push(branch);
  state.currentTenantId = tenant.id;
  persistMock();
  return { tenant, branch };
}

async function createTenantUser(payload) {
  if (state.apiMode) {
    const created = await apiFetch("/auth/manager-create-user", {
      method: "POST",
      body: JSON.stringify({
        fullName: payload.fullName,
        phone: payload.phone,
        email: payload.email,
        role: payload.role,
        pinHash: payload.pinHash || null,
        branchId: payload.branchId
      })
    });
    return created;
  }

  const user = {
    id: uid(),
    fullName: payload.fullName,
    phone: payload.phone,
    email: payload.email,
    role: payload.role,
    pinHash: payload.pinHash,
    branchId: payload.branchId
  };

  state.users.push(user);
  persistMock();
  return user;
}

el.apiMode.addEventListener("change", () => {
  state.apiMode = el.apiMode.checked;
  setStatus(state.apiMode ? "Gercek API modu acik" : "Mock modu acik", "ok");
});

el.loginForm.addEventListener("submit", async e => {
  e.preventDefault();
  setStatus("Giris deneniyor...");
  try {
    const session = await login(el.loginEmail.value, el.loginPassword.value);
    openApp(session);
    setStatus("Giris basarili", "ok");
  } catch (err) {
    setStatus(`Giris hatasi: ${err.message}`, "error");
  }
});

el.logoutBtn.addEventListener("click", logout);
el.openTenantPanel.addEventListener("click", () => el.tenantDialog.showModal());
el.openUserPanel.addEventListener("click", () => {
  renderBranchSelect();
  el.userDialog.showModal();
});

el.tenantForm.addEventListener("submit", async e => {
  e.preventDefault();
  const name = el.tenantName.value.trim();
  const branchName = el.branchName.value.trim();
  const code = el.branchCode.value.trim().toUpperCase();
  if (!name || !branchName || !code) return;

  await createTenantAndBranch(name, branchName, code);
  el.tenantDialog.close();
  el.tenantForm.reset();
  renderTenantList();
  renderBranchList();
  renderBranchSelect();
  setStatus("Tenant ve sube olusturuldu", "ok");
});

el.userForm.addEventListener("submit", async e => {
  e.preventDefault();
  try {
    const payload = {
      fullName: el.userFullName.value.trim(),
      phone: el.userPhone.value.trim(),
      email: el.userEmail.value.trim() || null,
      role: el.userRole.value,
      branchId: el.userBranchSelect.value,
      pinHash: el.userPin.value.trim() || null
    };

    await createTenantUser(payload);
    el.userDialog.close();
    el.userForm.reset();
    renderUserList();
    setStatus("Kullanici eklendi", "ok");
  } catch (err) {
    setStatus(`Kullanici eklenemedi: ${err.message}`, "error");
  }
});

el.refreshTenants.addEventListener("click", renderTenantList);
el.refreshBranches.addEventListener("click", renderBranchList);
el.refreshUsers.addEventListener("click", renderUserList);

initClock();
setStatus("Hazir");
