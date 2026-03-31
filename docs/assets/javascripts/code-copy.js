function copyTextToClipboard(text) {
  if (navigator.clipboard && window.isSecureContext) {
    return navigator.clipboard.writeText(text);
  }

  return new Promise((resolve, reject) => {
    const helper = document.createElement("textarea");
    helper.value = text;
    helper.setAttribute("readonly", "");
    helper.style.position = "fixed";
    helper.style.top = "-9999px";
    helper.style.left = "-9999px";
    document.body.appendChild(helper);
    helper.select();
    helper.setSelectionRange(0, helper.value.length);

    try {
      const copied = document.execCommand("copy");
      document.body.removeChild(helper);

      if (!copied) {
        reject(new Error("Copy command was not successful."));
        return;
      }

      resolve();
    } catch (error) {
      document.body.removeChild(helper);
      reject(error);
    }
  });
}

function setCopyButtonState(button, label, stateClass) {
  button.textContent = label;
  button.classList.remove("is-success", "is-error");

  if (stateClass) {
    button.classList.add(stateClass);
  }
}

function installCodeCopyButtons() {
  const blocks = document.querySelectorAll("pre > code");

  blocks.forEach((codeBlock) => {
    const pre = codeBlock.parentElement;
    if (!pre || pre.dataset.copyReady === "true") {
      return;
    }

    pre.dataset.copyReady = "true";
    pre.classList.add("nx-code-block");

    const button = document.createElement("button");
    button.type = "button";
    button.className = "nx-copy-button";
    button.textContent = "Copy";
    button.setAttribute("aria-label", "Copy code to clipboard");

    let resetTimerId = null;

    button.addEventListener("click", async () => {
      if (resetTimerId !== null) {
        window.clearTimeout(resetTimerId);
        resetTimerId = null;
      }

      try {
        await copyTextToClipboard(codeBlock.textContent.replace(/\n$/, ""));
        setCopyButtonState(button, "Copied", "is-success");
      } catch (_error) {
        setCopyButtonState(button, "Press Ctrl+C", "is-error");
      }

      resetTimerId = window.setTimeout(() => {
        setCopyButtonState(button, "Copy", null);
        resetTimerId = null;
      }, 1800);
    });

    pre.appendChild(button);
  });
}

if (document.readyState === "loading") {
  document.addEventListener("DOMContentLoaded", installCodeCopyButtons);
} else {
  installCodeCopyButtons();
}
