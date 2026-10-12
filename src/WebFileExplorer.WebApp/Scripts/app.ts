type FileSystemItem = {
    name: string;
    relativePath: string;
    isDirectory: boolean;
    sizeBytes: number | null;
};

type FileSystemView = {
    path: string;
    items: FileSystemItem[];
    folderCount: number;
    fileCount: number;
    totalSizeBytes: number;
};

// DOM references
const breadcrumbsNav = document.querySelector<HTMLElement>("#breadcrumbs")!;
const summary = document.querySelector<HTMLParagraphElement>("#summary")!;
const statusMessage = document.querySelector<HTMLParagraphElement>("#status")!;
const items = document.querySelector<HTMLUListElement>("#items")!;
const searchInput = document.querySelector<HTMLInputElement>("#search")!;
const searchForm = document.querySelector<HTMLFormElement>("#search-form")!;
const clearSearchButton = document.querySelector<HTMLButtonElement>("#clear")!;
const uploadForm = document.querySelector<HTMLFormElement>("#upload-form")!;
const clearFileButton = document.querySelector<HTMLButtonElement>("#clear-file")!;
const uploadButton = uploadForm.querySelector<HTMLButtonElement>('button[type="submit"]')!;
const fileInput = document.querySelector<HTMLInputElement>("#file")!;

// The last request / view state.
let viewAbort: AbortController | null = null;
let currentView: FileSystemView | null = null;
let uploadInProgress = false;

// The URL owns the selected folder and search so reload and Back/Forward work.
function urlState(): URLSearchParams {
    return new URLSearchParams(location.search);
}

function currentPath(): string {
    return urlState().get("path") || "";
}

function currentQuery(): string {
    return urlState().get("q") || "";
}

function displaySize(bytes: number | null): string {
    if (bytes === null)
    {
        return "—";
    }

    const base = 1000;
    const labels = ["bytes", "KB", "MB", "GB", "TB"];

    let size = bytes;
    let index = 0;
    while (size >= base &&
           index < labels.length - 1) {
        size /= base;
        index++;
    }

    return index === 0
        ? `${bytes.toLocaleString()} bytes`
        : `${new Intl.NumberFormat(undefined, { maximumFractionDigits: 1 }).format(size)} ${labels[index]}`;
}

// Only upload responses carry this metadata. Listing limits keep their existing wording.
function uploadErrorMessage(
    body: {
        error?: string;
        code?: string;
        maxUploadBytes?: number;
        requestTooLarge?: boolean;
    } | null, status: number): string {
    if (body?.code === "FileSystem.LimitExceeded") {
        const bytes = body.maxUploadBytes;

        if (typeof bytes === "number" &&
            Number.isSafeInteger(bytes) &&
            bytes > 0) {
            // Use the same decimal units and display precision as the file listing.
            const limit = displaySize(bytes);

            return body.requestTooLarge === true
                ? `Upload request is too large. Maximum file size: ${limit}.`
                : `File is too large. Maximum upload size: ${limit}.`;
        }

        return "Upload exceeds the configured file-size limit.";
    }

    return body?.error || (status === 413
        ? "Upload exceeds the configured size limit."
        : `Upload failed (${status}).`);
}

function setStatus(message: string, kind: "info" | "error" = "info"): void {
    statusMessage.setAttribute("role", kind === "error" ? "alert" : "status");
    statusMessage.dataset.kind = kind;
    statusMessage.textContent = message;
}

function navigate(directory: string, search = ""): void {
    const next = new URLSearchParams();

    if (directory) {
        next.set("path", directory);
    }

    if (search) {
        next.set("q", search);
    }

    history.pushState(null, "", location.pathname + (next.size ? "?" + next : ""));
    void loadView();
}

// Rendering: each row is built once, then the view and breadcrumb are updated.
function createItemRow(
    item: FileSystemItem,
    searchRoot: string,
    isSearch: boolean)
    : HTMLLIElement {
    const row = document.createElement("li");
    const link = document.createElement("a");
    const label = isSearch
        ? item.relativePath.slice(searchRoot ? searchRoot.length + 1 : 0)
        : item.name;
    link.className = "item-link";
    link.title = item.relativePath;
    const title = document.createElement("span");
    title.className = "item-title";
    const icon = document.createElement("span");
    icon.className = "item-icon";
    icon.textContent = item.isDirectory ? "📁" : "📄";
    icon.setAttribute("aria-hidden", "true");
    const name = document.createElement("span");
    name.className = "item-name";
    name.textContent = item.name;
    title.append(icon, name);
    link.append(title);

    // Search shows the result name first; its location is relative to the searched folder.
    const lastSeparator = label.lastIndexOf("/");
    if (isSearch && lastSeparator >= 0) {
        const parent = document.createElement("span");
        parent.className = "item-path";
        parent.textContent = label.slice(0, lastSeparator);
        link.append(parent);
    }

    if (item.isDirectory) {
        // Folder navigation path and event handling
        link.href = "?path=" + encodeURIComponent(item.relativePath);
        link.addEventListener("click", event => {
            event.preventDefault();
            navigate(item.relativePath);
        });
    } else {
        // File download link
        link.href = "/api/files/download?path=" + encodeURIComponent(item.relativePath);
    }

    const size = document.createElement("span");
    size.className = "muted";
    size.textContent = item.isDirectory ? "Folder" : displaySize(item.sizeBytes);

    if (!item.isDirectory &&
        item.sizeBytes !== null) {
        size.title = `${item.sizeBytes.toLocaleString()} bytes`;
    }

    row.append(link, size);
    return row;
}

function renderItems(view: FileSystemView, isSearch: boolean): void {
    summary.textContent =
        `${view.folderCount} ${view.folderCount === 1 ? "folder" : "folders"},
         ${view.fileCount} ${view.fileCount === 1 ? "file" : "files"},
         ${displaySize(view.totalSizeBytes)} in shown files`;

    summary.title = `${view.totalSizeBytes.toLocaleString()} bytes in shown files`;
    items.replaceChildren();

    for (const item of view.items) {
        items.append(createItemRow(item, view.path, isSearch));
    }
}

function renderBreadcrumbs(directory: string): void {
    breadcrumbsNav.replaceChildren();
    breadcrumbsNav.setAttribute("aria-label", `Folder path: ${directory || "Root"}`);
    const parts = directory ? directory.split("/") : [];
    const last = parts.length;

    // Keep Root and the nearest parent visible; parent links lead to earlier folders.
    const visible = last > 2 ? [0, last - 1, last] : Array.from({ length: last + 1 }, (_, i) => i);
    for (const i of visible) {
        if (i) {
            const separator = document.createElement("span");
            separator.className = "separator";
            separator.textContent = last > 2 && i === last - 1 ? " / … / " : " / ";
            separator.setAttribute("aria-hidden", "true");
            breadcrumbsNav.append(separator);
        }

        const label = i ? parts[i - 1] : "Root";
        if (i === last) {
            const current = document.createElement("span");
            current.textContent = label;
            current.title = directory || "Root";
            current.setAttribute("aria-current", "page");
            breadcrumbsNav.append(current);
        } else {
            const target = parts.slice(0, i).join("/");
            const link = document.createElement("a");
            link.textContent = label;
            link.title = target || "Root";
            link.href = target ? "?path=" + encodeURIComponent(target) : "/";
            link.addEventListener("click", event => {
                event.preventDefault();
                navigate(target);
            });
            breadcrumbsNav.append(link);
        }
    }
}

// Request lifecycle: keep the verified view until its replacement succeeds.
async function loadView(): Promise<void> {
    viewAbort?.abort();
    const controller = new AbortController();
    viewAbort = controller;
    const params = urlState();
    const directory = currentPath();
    const query = currentQuery();
    const isSearch = Boolean(query);
    searchInput.value = query;

    // Search results may come from many folders; uploads belong only to browse views.
    uploadForm.hidden = isSearch;
    if (uploadForm.hidden) {
        fileInput.value = "";
        clearFileButton.hidden = true;
    }

    // Preserve the verified view while navigating; disable links into stale rows.
    const sameFolder = currentView?.path === directory && breadcrumbsNav.childNodes.length > 0;
    if (!sameFolder) {
        breadcrumbsNav.inert = true;
        uploadButton.disabled = true;
        uploadForm.inert = true;
        items.inert = true;
        items.setAttribute("aria-busy", "true");
    }

    setStatus("");
    let searchTooLong = false;

    try {
        let response = await fetch("/api/files?" + params, { signal: controller.signal });
        let displayedDirectory = directory;
        let missingFolder = false;

        if (response.status === 404 && directory) {
            response = await fetch("/api/files", { signal: controller.signal });
            displayedDirectory = "";
            missingFolder = true;
        }

        if (!response.ok) {
            const error = (await response.json().catch(() => null)) as { code?: string; error?: string } | null;

            if (controller.signal.aborted) {
                return;
            }

            searchTooLong = error?.code === "Browse.SearchTooLong";
            throw new Error(error?.error || `Could not load folder (${response.status}).`);
        }

        const result = (await response.json()) as FileSystemView;

        if (controller.signal.aborted) {
            return;
        }

        if (missingFolder) {
            history.replaceState(null, "", location.pathname);
            searchInput.value = "";
            uploadForm.hidden = false;
        }

        searchForm.hidden = false;
        currentView = result;
        renderBreadcrumbs(displayedDirectory);
        breadcrumbsNav.inert = false;
        uploadButton.disabled = uploadInProgress;
        uploadForm.inert = false;
        renderItems(result, missingFolder ? false : isSearch);
        items.inert = false;
        items.removeAttribute("aria-busy");
        setStatus(missingFolder ? "Folder not found. Returned to Root." : "", missingFolder ? "error" : "info");
    } catch (cause) {
        if (cause instanceof Error &&
            cause.name === "AbortError") {
            return;
        }

        const rootLink = document.createElement("a");
        rootLink.href = location.pathname;
        rootLink.textContent = "Root";
        rootLink.addEventListener("click", event => {
            event.preventDefault();
            history.replaceState(null, "", location.pathname);
            void loadView();
        });

        breadcrumbsNav.replaceChildren(rootLink);
        breadcrumbsNav.setAttribute("aria-label", "Return to Root");
        breadcrumbsNav.inert = false;

        searchForm.hidden = !searchTooLong;
        uploadButton.disabled = true;
        uploadForm.inert = true;
        uploadForm.hidden = true;
        fileInput.value = "";
        clearFileButton.hidden = true;

        currentView = null;

        items.replaceChildren();
        items.inert = false;
        items.removeAttribute("aria-busy");
        summary.textContent = "Total size unavailable";

        setStatus(cause instanceof Error ? cause.message : "Could not load folder.", "error");
    }
}

clearSearchButton.addEventListener("click", () => {
    searchInput.value = "";

    if (currentQuery()) {
        navigate(currentPath());
    }
});

searchInput.addEventListener("input", () => {
    if (!searchInput.value &&
        currentQuery()) {
        navigate(currentPath());
    }
});

searchForm.addEventListener("submit", event => {
    event.preventDefault();
    navigate(currentPath(), searchInput.value.trim());
});

fileInput.addEventListener("change", () => {
    clearFileButton.hidden = !fileInput.files?.length;
});

clearFileButton.addEventListener("click", () => {
    fileInput.value = "";
    clearFileButton.hidden = true;
});

uploadForm.addEventListener("submit", async event => {
    event.preventDefault();

    const file = fileInput.files?.[0];
    if (!file || uploadInProgress) {
        return;
    }

    uploadInProgress = true;
    uploadButton.disabled = true;
    uploadButton.textContent = "Uploading...";
    setStatus("");

    const form = new FormData();
    form.append("file", file);

    try {
        const response = await fetch("/api/files/upload?path=" + encodeURIComponent(currentPath()), {
            method: "POST", body: form
        });

        if (!response.ok) {
            const body = (await response.json().catch(() => null)) as Parameters<typeof uploadErrorMessage>[0];
            throw new Error(uploadErrorMessage(body, response.status));
        }

        fileInput.value = "";
        clearFileButton.hidden = true;
        await loadView();
    } catch (cause) {
        setStatus(cause instanceof Error ? cause.message : "Upload failed.", "error");
    } finally {
        uploadInProgress = false;
        uploadButton.textContent = "Upload";

        // Keep Upload unavailable if the refreshed folder could not be loaded.
        uploadButton.disabled = currentView === null ||
                                currentView.path !== currentPath() ||
                                Boolean(uploadForm.hidden);
    }
});

window.addEventListener("popstate", () => void loadView());

// A page restored from the back-forward cache may have an unfinished request.
window.addEventListener("pageshow", event => {
    if (event.persisted) {
        void loadView();
    }
});

void loadView();
