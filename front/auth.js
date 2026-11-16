// auth.js - sessão do usuário (S1): renovação do token e logout.
// Depende do config.js (API_URL). Incluir logo depois dele nas páginas autenticadas.

let renovando = null;

// Renova a sessão uma vez só, mesmo que várias chamadas tomem 401 ao mesmo tempo
// (um segundo uso do mesmo refresh token seria tratado pela API como roubo e derrubaria a sessão).
function renovarSessao() {
    if (!renovando) {
        renovando = (async () => {
            const rt = localStorage.getItem('refreshToken');
            if (!rt) return false;
            try {
                const r = await fetch(`${API_URL}/api/Auth/refresh`, {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ refreshToken: rt })
                });
                if (!r.ok) return false;
                const d = await r.json();
                localStorage.setItem('token', d.token);
                localStorage.setItem('refreshToken', d.refreshToken);
                return true;
            } catch {
                return false;
            }
        })().finally(() => { renovando = null; });
    }
    return renovando;
}

// fetch autenticado: manda o token e, se a API responder 401, tenta renovar a sessão e repete a chamada uma vez.
// Se a renovação falhar, devolve o 401 original para a página tratar (alert + logout).
async function apiFetch(url, opcoes = {}) {
    const montar = () => ({
        ...opcoes,
        headers: { ...(opcoes.headers || {}), 'Authorization': `Bearer ${localStorage.getItem('token')}` }
    });

    let resp = await fetch(url, montar());
    if (resp.status === 401 && await renovarSessao())
        resp = await fetch(url, montar());
    return resp;
}

// Encerra a sessão: revoga o refresh token na API e limpa o navegador.
function logout() {
    const rt = localStorage.getItem('refreshToken');
    if (rt) {
        fetch(`${API_URL}/api/Auth/logout`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ refreshToken: rt }),
            keepalive: true
        }).catch(() => {});
    }
    localStorage.removeItem('token');
    localStorage.removeItem('refreshToken');
    localStorage.removeItem('usuarioNome');
    window.location.href = 'login.html';
}
