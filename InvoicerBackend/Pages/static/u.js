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