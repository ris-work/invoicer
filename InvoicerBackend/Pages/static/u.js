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