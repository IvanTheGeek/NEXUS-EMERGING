function normalizeDiscussionPath(sourcePath) {
  const normalizedSourcePath = (sourcePath || "").replace(/\\/g, "/").trim();

  if (!normalizedSourcePath) {
    return "home";
  }

  let normalized = normalizedSourcePath.replace(/\.md$/i, "");

  if (normalized === "index") {
    return "home";
  }

  normalized = normalized.replace(/\/README$/i, "");

  return normalized || "home";
}

function deriveDiscussionId(sourcePath, namespace) {
  const scopedNamespace = (namespace || "nexus-emerging").trim() || "nexus-emerging";
  const normalizedPath = normalizeDiscussionPath(sourcePath);
  return `${scopedNamespace}:${normalizedPath}`;
}

function ensureTalkyardScript(serverUrl) {
  const scriptId = "nexus-talkyard-comments-script";
  let script = document.getElementById(scriptId);

  if (script) {
    return script;
  }

  script = document.createElement("script");
  script.id = scriptId;
  script.async = true;
  script.defer = true;
  script.src = `${serverUrl.replace(/\/$/, "")}/-/talkyard-comments.min.js`;
  document.head.appendChild(script);
  return script;
}

function installTalkyardComments() {
  const container = document.querySelector(".nexus-talkyard-comments");

  if (!container) {
    return;
  }

  const serverUrl = container.dataset.talkyardServerUrl;

  if (!serverUrl) {
    return;
  }

  const namespace = container.dataset.talkyardNamespace;
  const discussionId =
    container.dataset.discussionId ||
    deriveDiscussionId(container.dataset.sourcePath, namespace);

  container.dataset.discussionId = discussionId;

  if (container.dataset.talkyardCategory) {
    container.dataset.category = container.dataset.talkyardCategory;
  }

  if (container.dataset.pageTitle) {
    container.dataset.discussionTitle = container.dataset.pageTitle;
  }

  if (container.dataset.pageUrl) {
    container.dataset.discussionUrl = container.dataset.pageUrl;
  }

  window.talkyardServerUrl = serverUrl;
  ensureTalkyardScript(serverUrl);
}

if (document.readyState === "loading") {
  document.addEventListener("DOMContentLoaded", installTalkyardComments);
} else {
  installTalkyardComments();
}
