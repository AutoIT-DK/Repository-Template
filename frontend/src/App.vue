<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'

type Claim = {
  type: string
  value: string
}

type BffUserObject = {
  sub?: string
  name?: string
  claims?: Claim[]
}

const loading = ref(true)
const error = ref('')
const bffClaims = ref<Claim[]>([])
const userProfile = ref<unknown>(null)
const userAccess = ref<unknown>(null)

const isAuthenticated = computed(() => {
  return bffClaims.value.length > 0
})

const userId = computed(() => {
  const subClaim = bffClaims.value.find((x) => x.type === 'sub')
  return subClaim?.value ?? 'unknown'
})

const logoutUrl = computed(() => {
  const claim = bffClaims.value.find((x) => x.type === 'bff:logout_url')
  return claim?.value ?? '/bff/logout'
})

async function loadSecurityData() {
  loading.value = true
  error.value = ''
  userProfile.value = null
  userAccess.value = null

  try {
    const bffResponse = await fetch('/bff/user', {
      headers: { 'X-CSRF': '1' },
      credentials: 'include',
    })

    if (!bffResponse.ok) {
      bffClaims.value = []
      return
    }

    const raw = (await bffResponse.json()) as Claim[] | BffUserObject
    bffClaims.value = Array.isArray(raw) ? raw : (raw.claims ?? [])

    if (!bffClaims.value.length) {
      return
    }

    const localApiHeaders = { 'X-CSRF': '1' }
    const [profileResponse, accessResponse] = await Promise.all([
      fetch('/api/user', { credentials: 'include', headers: localApiHeaders }),
      fetch('/api/user/access', { credentials: 'include', headers: localApiHeaders }),
    ])

    if (profileResponse.ok) {
      userProfile.value = await profileResponse.json()
    }

    if (accessResponse.ok) {
      userAccess.value = await accessResponse.json()
    }
  } catch (e) {
    error.value = e instanceof Error ? e.message : 'Unexpected error while loading security data.'
  } finally {
    loading.value = false
  }
}

function login() {
  window.location.href = '/bff/login?returnUrl=/'
}

function logout() {
  window.location.href = logoutUrl.value
}

onMounted(async () => {
  await loadSecurityData()
})
</script>

<template>
  <main class="page">
    <header class="header">
      <h1>AutoIT Security Starter</h1>
      <p>Backend + frontend wired for Duende BFF and policy-provider calls.</p>
    </header>

    <section class="actions">
      <button v-if="!isAuthenticated" class="primary" type="button" @click="login">Login</button>
      <button v-if="isAuthenticated" class="primary" type="button" @click="loadSecurityData">Refresh User Data</button>
      <button v-if="isAuthenticated" class="secondary" type="button" @click="logout">Logout</button>
    </section>

    <section v-if="loading" class="card">
      <h2>Status</h2>
      <p>Loading security context...</p>
    </section>

    <section v-else-if="!isAuthenticated" class="card">
      <h2>Authentication</h2>
      <p>You are not logged in. Use the Login button to start the BFF flow.</p>
    </section>

    <section v-else class="card">
      <h2>Authentication</h2>
      <p>Signed in user id: <strong>{{ userId }}</strong></p>
    </section>

    <section v-if="error" class="card error">
      <h2>Error</h2>
      <p>{{ error }}</p>
    </section>

    <section v-if="userProfile || userAccess" class="data-grid">
      <article v-if="userProfile" class="card data-card">
        <h2>Profile (Policy Provider)</h2>
        <pre>{{ JSON.stringify(userProfile, null, 2) }}</pre>
      </article>

      <article v-if="userAccess" class="card data-card">
        <h2>Access (Policy Provider)</h2>
        <pre>{{ JSON.stringify(userAccess, null, 2) }}</pre>
      </article>
    </section>
  </main>
</template>

<style scoped>
.page {
  max-width: 1400px;
  margin: 0 auto;
  padding: 2.25rem 1.5rem 3rem;
}

.header h1 {
  font-size: 1.8rem;
  margin: 0 0 0.5rem;
}

.header p {
  margin: 0 0 1.5rem;
  color: #555;
}

.actions {
  display: flex;
  gap: 0.75rem;
  flex-wrap: wrap;
  justify-content: center;
  margin-bottom: 1rem;
}

button {
  border: 0;
  border-radius: 0.5rem;
  padding: 0.6rem 1rem;
  font-weight: 600;
  cursor: pointer;
}

.primary {
  background: #115e59;
  color: #fff;
}

.secondary {
  background: #334155;
  color: #fff;
}

.card {
  border: 1px solid #e5e7eb;
  border-radius: 1rem;
  padding: 1.1rem;
  margin-top: 1rem;
  background: #fff;
  box-shadow: 0 10px 30px rgba(2, 6, 23, 0.05);
}

.error {
  border-color: #fecaca;
  background: #fef2f2;
}

.data-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 1rem;
  margin-top: 1rem;
}

.data-grid .card {
  margin-top: 0;
}

.data-card {
  border-color: #cbd5e1;
  background: linear-gradient(180deg, #ffffff 0%, #f8fafc 100%);
}

.data-card h2 {
  margin-top: 0;
  margin-bottom: 0.75rem;
}

pre {
  margin: 0;
  white-space: pre-wrap;
  overflow-wrap: anywhere;
  font-size: 0.85rem;
  line-height: 1.4;
  padding: 0.9rem;
  border-radius: 0.75rem;
  border: 1px solid #dbeafe;
  background: #f8fafc;
  max-height: 420px;
  overflow: auto;
}

@media (max-width: 960px) {
  .data-grid {
    grid-template-columns: 1fr;
  }
}
</style>
