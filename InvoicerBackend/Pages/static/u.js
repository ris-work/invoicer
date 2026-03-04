// Escapes HTML special characters to their entity equivalents for safe interpolation.
function he(str) {
    const p = document.createElement('p');
    p.textContent = str;
    return p.innerHTML;
}

// Unescapes HTML entities back to their original character representations.
function hu(str) {
    const p = document.createElement('p');
    p.innerHTML = str;
    return p.textContent;
}

/**
 * Global tagged template literal for safe HTML interpolation.
 * Sanitizes interpolated values using the existing he() function.
 * 
 * @param {TemplateStringsArray} strings - The static string parts.
 * @param  {...any} values - The dynamic values to be sanitized.
 * @returns {string} A safe HTML string.
 */
window.s = function (strings, ...values) {
    let result = strings[0];

    for (let i = 0; i < values.length; i++) {
        // Cast to string and sanitize the dynamic value
        result += he(String(values[i]));
        // Append the next static string part
        result += strings[i + 1];
    }

    return result;
};


/*
* Permission Utility Functions
* Requires s (XSS helper) to be present.
*/

// Global Cache
let __authToken = null;
let __userPrivileges = null; // Set<string> (lowercase)
let __endpointsDict = null;  // Map<string, {privilege: string}>

async function getCookie(name) {
    const value = `; ${document.cookie}`;
    const parts = value.split(`; ${name}=`);
    if (parts.length === 2) return parts.pop().split(';').shift();
}

async function ensureAuth() {
    if (!__authToken) __authToken = await getCookie("auth-token");
    return __authToken;
}

// Fetches and caches user privileges (lowercased)
async function getUserPrivileges() {
    if (__userPrivileges) return __userPrivileges;

    const token = await ensureAuth();
    if (!token) return new Set();

    try {
        const res = await fetch('/BearerPrivileges', {
            method: 'POST',
            headers: { 'Authorization': `Bearer ${token}`, 'Content-Type': 'application/json' },
            body: JSON.stringify({})
        });
        if (res.ok) {
            const data = await res.json();
            __userPrivileges = new Set(data.map(p => p.toLowerCase()));
        } else {
            __userPrivileges = new Set();
        }
    } catch (e) {
        console.error("Failed to fetch privileges", e);
        __userPrivileges = new Set();
    }
    return __userPrivileges;
}

// Fetches and caches endpoint definitions
async function getEndpointsDict() {
    if (__endpointsDict) return __endpointsDict;

    const token = await ensureAuth();
    if (!token) return new Map();

    try {
        const res = await fetch('/Endpoints', {
            method: 'GET',
            headers: { 'Authorization': `Bearer ${token}` }
        });
        if (res.ok) {
            const data = await res.json();
            const map = new Map();
            // Data structure: [{name: "...", privilege: "..."}, ...]
            data.forEach(ep => map.set(ep.name, ep));
            __endpointsDict = map;
        } else {
            __endpointsDict = new Map();
        }
    } catch (e) {
        console.error("Failed to fetch endpoints", e);
        __endpointsDict = new Map();
    }
    return __endpointsDict;
}

/* 
 * Main Page Permission Checker
 * param: requiredEndpoints (string[]) - Array of endpoint names used by the page.
 * Behavior: Looks up required privileges for each endpoint and displays warning if missing.
 */
async function checkPagePermissions(requiredEndpoints) {
    const [privs, endpoints] = await Promise.all([
        getUserPrivileges(),
        getEndpointsDict()
    ]);

    const missingPerms = new Set();

    requiredEndpoints.forEach(epName => {
        const epDef = endpoints.get(epName);
        if (epDef) {
            const reqPriv = (epDef.privilege || "").toLowerCase();
            // If privilege is defined and user does NOT have it
            if (reqPriv && !privs.has(reqPriv)) {
                missingPerms.add(epDef.privilege); // Keep original casing for display
            }
        } else {
            console.warn(`Endpoint definition not found: ${epName}`);
        }
    });

    if (missingPerms.size > 0) {
        const warningDiv = document.getElementById('permWarning');
        if (warningDiv) {
            warningDiv.style.display = 'block';
            // Use s for safety, though we trust the source mostly
            warningDiv.innerText = `The user you are logged in as is not allowed to do this (the buttons can be pressed, but won't do anything): ${[...missingPerms].join(', ')}`;
        }
    }
}